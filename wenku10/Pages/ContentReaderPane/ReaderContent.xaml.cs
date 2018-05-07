using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using GR.Config;
using GR.CompositeElement;
using GR.Database.Models;
using GR.Effects;
using GR.GSystem;
using GR.Model.Loaders;
using GR.Model.Section;
using GR.Model.Text;
using GR.Resources;

using BookItem = GR.Model.Book.BookItem;

namespace wenku10.Pages.ContentReaderPane
{
	sealed partial class ReaderContent : Page, IDisposable
	{
		public static readonly string ID = typeof( ReaderContent ).Name;

		public ReaderView Reader { get; private set; }
		public bool UserStartReading = false;

		private ContentReaderBase Container;
		private BookItem CurrentBook { get { return Container.CurrentBook; } }
		private Chapter CurrentChapter { get { return Container.CurrentChapter; } }
		private Paragraph SelectedParagraph;

		public AccelerScroll ACScroll { get; private set; }
		DispatcherTimer ACSTimer;
		ScrollViewer AccelerSV;

		private volatile bool HoldOneMore = false;
		private volatile int UndoingJump = 0;

		private bool IsHorz = false;

		private AHQueue AnchorHistory;

		ScrollBar VScrollBar;
		ScrollBar HScrollBar;

		CompatMenuFlyoutItem ToggleAcceler;
		MenuFlyoutItem CallibrateAcceler;

		public ReaderContent( ContentReaderBase Container, int Anchor )
		{
			this.InitializeComponent();
			this.Container = Container;

			IsHorz = ( Container is ContentReaderHorz );
			SetTemplate( Anchor );
		}

		public void Dispose()
		{
			try
			{
				ACScroll.StopReading();
				ACSTimer?.Stop();

				Reader.PropertyChanged -= ScrollToParagraph;
				Reader.Dispose();
				Reader = null;

				Worker.UIInvoke( () =>
				{
					MasterGrid.DataContext = null;
				} );
			}
			catch ( Exception ) { }
		}

		private void SetTemplate( int Anchor )
		{
			if ( Reader != null )
				Reader.PropertyChanged -= ScrollToParagraph;

			Paragraph.Translator = new libtranslate.Translator();
			InitPhaseConverter();

			Reader = new ReaderView( CurrentBook, CurrentChapter );
			Reader.ApplyCustomAnchor( Anchor );

			AnchorHistory = new AHQueue( 20 );
			HCount.DataContext = AnchorHistory;

			ContentGrid.ItemsPanel = ( ItemsPanelTemplate ) Resources[ IsHorz ? "HPanel" : "VPanel" ];

			MasterGrid.DataContext = Reader;
			Reader.PropertyChanged += ScrollToParagraph;
			GRConfig.ConfigChanged.AddHandler( this, CRConfigChanged );

			SetAccelerScroll();
		}

		private async void InitPhaseConverter()
		{
			List<CustomConv> Phases = await Shared.BooksDb.LoadCollectionAsync( CurrentBook.Entry, x => x.ConvPhases, x => x.Phase );
			Phases.ForEach( x => Paragraph.Translator.AddTable( x.Table ) );

			if ( IsHorz )
			{
				TRTable Table = new TRTable();
				Paragraph.Translator.AddTable( await Table.Get( "vertical" ) );
			}
		}

		private void CRConfigChanged( Message Mesg )
		{
			if ( Mesg.TargetType == typeof( GR.Config.Scopes.Conf_ContentReader ) && Mesg.Content == "ScrollBarColor" )
			{
				UpdateScrollBar();
			}
		}

		internal void Load( bool Reload = false )
		{
			Reader.Load( !Reload || CurrentBook.Type == BookType.L );
		}

		internal void ContentGrid_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if ( Reader == null || UserStartReading ) return;
			UserStartReading = true;

			if ( 0 < e.AddedItems.Count )
			{
				ContentGrid.ScrollIntoView( e.AddedItems[ 0 ] );
			}

			Reader.AutoVolumeAnchor();
		}

		internal void Grid_RightTapped( object sender, RightTappedRoutedEventArgs e )
		{
			Grid ParaGrid = sender as Grid;
			if ( ParaGrid == null || ( ACScroll.ForceBrake && Container.OverlayActive ) )
				return;

			FlyoutBase.ShowAttachedFlyout( MainStage.Instance.IsPhone ? MasterGrid : ParaGrid );

			SelectedParagraph = ParaGrid.DataContext as Paragraph;
		}

		internal void ScrollMore( bool IsPage = false )
		{
			ScrollViewer SV = ContentGrid.Child_0<ScrollViewer>( 1 );
			double d = 50;
			if ( IsHorz )
			{
				if ( IsPage ) d = LayoutSettings.ScreenWidth;
				SV.ChangeView( SV.HorizontalOffset + d, null, null );
			}
			else
			{
				if ( IsPage ) d = LayoutSettings.ScreenHeight;
				SV.ChangeView( null, SV.VerticalOffset + d, null );
			}
		}

		private async void SetAccelerScroll()
		{
			var ACSConf = GRConfig.ContentReader.AccelerScroll;
			ACScroll = new AccelerScroll
			{
				ProgramBrake = true,
				TrackAutoAnchor = ACSConf.TrackAutoAnchor,
				Brake = ACSConf.Brake,
				BrakeOffset = ACSConf.BrakeOffset,
				BrakingForce = ACSConf.BrakingForce,
				AccelerMultiplier = ACSConf.AccelerMultiplier,
				TerminalVelocity = ACSConf.TerminalVelocity
			};

			ACScroll.UpdateOrientation( App.ViewControl.DispOrientation );

			StringResources stx = StringResources.Load( "Settings", "Message" );
			ToggleAcceler = UIAliases.CreateMenuFlyoutItem( stx.Text( "Enabled" ), new SymbolIcon( Symbol.Accept ) );
			ToggleAcceler.Click += ( s, e ) => ToggleAccelerScroll();

			CallibrateAcceler = new MenuFlyoutItem() { Text = stx.Text( "Callibrate" ) };
			CallibrateAcceler.Click += CallibrateAcceler_Click;

			AccelerMenu.Items.Add( ToggleAcceler );
			AccelerMenu.Items.Add( CallibrateAcceler );

			if ( ACScroll.Available && !ACSConf.Asked )
			{
				bool EnableAccel = false;

				await Popups.ShowDialog( UIAliases.CreateDialog(
					stx.Str( "EnableAccelerScroll", "Message" )
					, () => EnableAccel = true
					, stx.Str( "Yes", "Message" ), stx.Str( "No", "Message" )
				) );

				ACSConf.Asked = true;
				ACSConf.Enable = EnableAccel;
			}

			ToggleAccelerScroll( ACSConf.Enable );
			UpdateAccelerDelta();
		}

		internal void UpdateAccelerDelta()
		{
			float a = 0, v = 0;

			if ( ACSTimer == null )
			{
				ACSTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds( 20 ) };
				ACSTimer.Tick += ( s, e ) =>
				{
					if ( a == 0 )
					{
						// Apply the brake when stopped
						Easings.ParamTween( ref v, 0, 1 - ACScroll.BrakingForce, ACScroll.BrakingForce );
					}
					else
					{
						v += ( ACScroll.AccelerMultiplier * a );
						v = v.Clamp( -ACScroll.TerminalVelocity, ACScroll.TerminalVelocity );
					}

					if ( 0.0001 < Math.Abs( v ) )
					{
						float d = ( float ) AccelerSV.HorizontalOffset;
						AccelerSV.ChangeView( d - v, null, null, true );
					}
					else
					{
						ACSTimer.Stop();
					}
				};
			}

			void UpdateAcc( float _a )
			{
				if ( ACScroll.ForceBrake || ACScroll.ProgramBrake )
				{
					a = 0;
				}
				else
				{
					a = _a;
					var j = Dispatcher.RunAsync( CoreDispatcherPriority.High, () =>
					{
						if ( !ACSTimer.IsEnabled )
							ACSTimer.Start();

						if( ACScroll.TrackAutoAnchor )
							AutoSelectParagraph();
					} );
				}
			}

			// Kickstarting machanism
			ACScroll.Delta = _a =>
			{
				if ( AccelerSV != null )
				{
					ACScroll.Delta = UpdateAcc;
					UpdateAcc( _a );
				}
			};
		}

		FrameworkElement VisibleParagraph;
		FrameworkElement VisibleContext;
		ItemsStackPanel ParaVisualizer;

		private void AutoSelectParagraph()
		{
			if ( ParaVisualizer == null )
			{
				ParaVisualizer = ContentGrid.ChildAt<ItemsStackPanel>( 0, 0, 0, 0, 0, 0, 1 );
				if ( ParaVisualizer == null )
					return;
			}

			Rect ScreenBounds = new Rect( 0, 0, ActualWidth * 0.8, ActualHeight );

			if ( VisibleParagraph != null && VisibleContext.DataContext.Equals( SelectedParagraph ) == true )
			{
				if ( VisualTreeHelper.FindElementsInHostCoordinates( ScreenBounds, ParaVisualizer ).Contains( VisibleParagraph ) )
				{
					return;
				}
			}

			int l = ParaVisualizer.Children.Count();
			for ( int i = 0; i < l; i++ )
			{
				FrameworkElement Item = ( FrameworkElement ) ParaVisualizer.Children[ i ];
				if ( VisualTreeHelper.FindElementsInHostCoordinates( ScreenBounds, ParaVisualizer ).Contains( Item ) )
				{
					FrameworkElement _ContentPresenter = Item.ChildAt<FrameworkElement>( 0, 0, 1 );
					if ( _ContentPresenter?.DataContext is Paragraph P )
					{
						VisibleParagraph = Item;
						VisibleContext = _ContentPresenter;
						SelectedParagraph = P;
						ContentGrid.SelectedItem = P;
						Reader.SelectAndAnchor( P );
						break;
					}
				}
			}
		}

		private void CallibrateAcceler_Click( object sender, RoutedEventArgs e )
		{
			Container.OverNavigate( typeof( Settings.CallibrateAcceler ), ACScroll );
		}

		private void ToggleAccelerScroll( bool? State = null )
		{
			bool RState = false;
			if ( State == null )
			{
				// Start toggling
				RState = ToggleAcceler.Icon2.Opacity == 0;
				GRConfig.ContentReader.AccelerScroll.Enable = RState;
			}
			else
			{
				RState = ( bool ) State;
			}

			if ( RState )
			{
				ToggleAcceler.Icon2.Opacity = 1;
				CallibrateAcceler.IsEnabled = true;
				ACScroll.StartReading();
			}
			else
			{
				ToggleAcceler.Icon2.Opacity = 0;
				CallibrateAcceler.IsEnabled = false;
				ACScroll.StopReading();
			}
		}

		internal void ScrollLess( bool IsPage = false )
		{
			ScrollViewer SV = ContentGrid.Child_0<ScrollViewer>( 1 );
			double d = 50;
			if ( IsHorz )
			{
				if ( IsPage ) d = LayoutSettings.ScreenWidth;
				SV.ChangeView( SV.HorizontalOffset - d, null, null );
			}
			else
			{
				if ( IsPage ) d = LayoutSettings.ScreenHeight;
				SV.ChangeView( null, SV.VerticalOffset - d, null );
			}
		}

		internal void PrevPara()
		{
			Reader.SelectIndex( Reader.SelectedIndex - 1 );
		}

		internal void NextPara()
		{
			Reader.SelectIndex( Reader.SelectedIndex + 1 );
		}

		private void GoTop( object sender, RoutedEventArgs e ) { GoTop(); }
		private void GoCurrent( object sender, RoutedEventArgs e ) { GoCurrent(); }
		private void GoBottom( object sender, RoutedEventArgs e ) { GoBottom(); }

		internal void GoTop() { GotoIndex( 0 ); }
		internal void GoCurrent() { GotoIndex( Reader.SelectedIndex ); }
		internal void GoBottom() { GotoIndex( ContentGrid.Items.Count - 1 ); }

		internal void GotoIndex( int i )
		{
			if ( ContentGrid.ItemsSource == null ) return;
			int l = ContentGrid.Items.Count;
			if ( !( -1 < i && i < l ) ) return;

			ContentGrid.SelectedIndex = i;
			ContentGrid.ScrollIntoView( ContentGrid.SelectedItem, ScrollIntoViewAlignment.Leading );
			Reader.SelectIndex( i );
			ShowUndoButton();
		}

		// This calls onLoaded
		private void SetBookAnchor( object sender, RoutedEventArgs e )
		{
			SetScrollBar();
			ToggleInertia();

			ContentGrid.IsSynchronizedWithCurrentItem = false;

			AccelerSV = ContentGrid.Child_0<ScrollViewer>( 1 );

			// Reader may not be available as ContentGrid.OnLoad is faster then SetTemplate
			if ( !( Reader == null || Reader.SelectedData == null ) )
				ContentGrid.ScrollIntoView( Reader.SelectedData, ScrollIntoViewAlignment.Leading );
		}

		private void SetScrollBar()
		{
			VScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 2 );
			HScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 3 );

			UpdateScrollBar();
		}

		private void UpdateScrollBar()
		{
			VScrollBar.Foreground
			   = HScrollBar.Foreground
			   = new SolidColorBrush( GRConfig.ContentReader.ScrollBarColor );
		}

		internal void ToggleInertia()
		{
			ScrollViewer SV = ContentGrid.Child_0<ScrollViewer>( 1 );
			if ( SV != null )
			{
				SV.HorizontalSnapPointsType = SnapPointsType.None;
				SV.VerticalSnapPointsType = SnapPointsType.None;
				SV.IsScrollInertiaEnabled = Container.UseInertia;
			}
		}

		internal async void ScrollToParagraph( object sender, PropertyChangedEventArgs e )
		{
			switch ( e.PropertyName )
			{
				case "SelectedIndex":
					if ( !UserStartReading )
						ContentGrid.SelectedItem = Reader.SelectedData;
					RecordUndo( Reader.SelectedIndex );
					break;
				case "Data":
					Shared.LoadMessage( "PleaseWaitSecondsForUI", "2" );
					await Task.Delay( 2000 );

					Shared.LoadMessage( "WaitingForUI" );

					ShowUndoButton();

					ACScroll.ProgramBrake = false;
					var NOP = ContentGrid.Dispatcher.RunIdleAsync( new IdleDispatchedHandler( Container.RenderComplete ) );
					break;
			}
		}

		internal void ViewHorizontal( object sender, RoutedEventArgs e )
		{
			if ( SelectedParagraph == null ) return;
			FlyoutBase.ShowAttachedFlyout( ContentGrid );

			ContentFlyout.Content = new TextBlock()
			{
				TextWrapping = TextWrapping.Wrap,
				Text = SelectedParagraph.Text
			};
		}

		internal void ContextCopyClicked( object sender, RoutedEventArgs e )
		{
			if ( SelectedParagraph == null ) return;
			DataPackage Data = new DataPackage();

			Data.SetText( SelectedParagraph.Text );
			Clipboard.SetContent( Data );
		}

		internal void MarkParagraph( object sender, RoutedEventArgs e )
		{
			if ( SelectedParagraph == null ) return;
			SetCustomAnchor( SelectedParagraph );
		}

		private void SearchWords( object sender, RoutedEventArgs e ) { SearchWords( SelectedParagraph ); }

		internal async void SearchWords( Paragraph P )
		{
			if ( P == null ) return;
			Dialogs.EBDictSearch DictDialog = new Dialogs.EBDictSearch( P );

			ACScroll.ProgramBrake = true;
			await Popups.ShowDialog( DictDialog );
			ACScroll.ProgramBrake = false;
		}

		public async void SetCustomAnchor( Paragraph P, string BookmarkName = null )
		{
			Dialogs.NewBookmarkInput BookmarkIn = new Dialogs.NewBookmarkInput( P );
			if ( BookmarkName != null ) BookmarkIn.SetName( BookmarkName );

			ACScroll.ProgramBrake = true;
			await Popups.ShowDialog( BookmarkIn );
			ACScroll.ProgramBrake = false;

			if ( BookmarkIn.Canceled ) return;

			Reader.SetCustomAnchor( BookmarkIn.AnchorName, P );
		}

		private void ShowConvPhases( object sender, RoutedEventArgs e )
		{
			Container.OverNavigate( typeof( Settings.Advanced.LocalTableEditor ), CurrentBook );
		}

		private void MasterGrid_Tapped( object sender, TappedRoutedEventArgs e )
		{
			Container.ClosePane();
			if ( Reader == null ) return;
			if ( Reader.UsePageClick )
			{
				Point P = e.GetPosition( MasterGrid );
				if ( IsHorz )
				{
					double HW = 0.5 * LayoutSettings.ScreenWidth;
					if ( GRConfig.ContentReader.IsRightToLeft )
						if ( P.X < HW ) ScrollMore( true ); else ScrollLess( true );
					else
						if ( HW < P.X ) ScrollMore( true ); else ScrollLess( true );
				}
				else
				{
					double HS = 0.5 * LayoutSettings.ScreenHeight;
					if ( P.Y < HS ) ScrollLess( true ); else ScrollMore( true );
				}
			}
		}

		private void ContentGrid_ItemClick( object sender, ItemClickEventArgs e )
		{
			Paragraph P = e.ClickedItem as Paragraph;
			if ( P == SelectedParagraph ) return;

			RecordUndo( ContentGrid.SelectedIndex );
			Reader.SelectAndAnchor( SelectedParagraph = P );

			if ( P is IllusPara S && !S.EmbedIllus )
			{
				Container.OverNavigate( typeof( ImageView ), S );
			}
		}

		private void UndoAnchorJump( object sender, RoutedEventArgs e )
		{
			if ( TransitionDisplay.GetState( UndoButton ) == TransitionState.Active )
			{
				UndoJump();
			}
			else
			{
				ShowUndoButton();
			}
		}

		internal void UndoJump()
		{
			while ( 0 < AnchorHistory.Count && AnchorHistory.Peek() == Reader.SelectedIndex )
				AnchorHistory.Pop();

			if ( AnchorHistory.Count == 0 ) return;

			UndoingJump++;
			GotoIndex( AnchorHistory.Pop() );
		}

		private void ShowUndoButton( object sender, PointerRoutedEventArgs e )
		{
			if ( !MainStage.Instance.IsPhone )
			{
				ShowUndoButton();
			}
		}

		private async void ShowUndoButton()
		{
			HoldOneMore = true;

			if ( TransitionDisplay.GetState( UndoButton ) == TransitionState.Active )
				return;

			TransitionDisplay.SetState( UndoButton, TransitionState.Active );
			while ( HoldOneMore )
			{
				HoldOneMore = false;
				await Task.Delay( 3000 );
			}

			TransitionDisplay.SetState( UndoButton, TransitionState.Inactive );
		}

		private void RecordUndo( int Index )
		{
			if ( 0 < UndoingJump )
			{
				UndoingJump--;
				return;
			}

			AnchorHistory.Push( Index );
			AnchorHistory.TrimExcess();
		}

		private class AHQueue : Stack<int>, INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public AHQueue( int Capacity ) : base( Capacity ) { }

			new public void Push( int i )
			{
				if ( 0 < Count && Peek() == i ) return;

				base.Push( i );
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( "Count" ) );
			}

			new public int Pop()
			{
				int i = base.Pop();
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( "Count" ) );
				return i;
			}
		}

		private double ZoomTrigger = 0;

		private void ManipulationDeltaX( object sender, ManipulationDeltaRoutedEventArgs e ) { TriggerZoom( e.Delta.Translation.X ); }
		private void ManipulationDeltaY( object sender, ManipulationDeltaRoutedEventArgs e ) { TriggerZoom( e.Delta.Translation.Y ); }

		private void TriggerZoom( double dv )
		{
			ZoomTrigger += dv;

			if ( 100 < ZoomTrigger )
			{
				ZoomTrigger = 0;
				CRSlide( ContentReaderBase.ManiState.DOWN );
			}
			else if ( ZoomTrigger < -100 )
			{
				ZoomTrigger = 0;
				CRSlide( ContentReaderBase.ManiState.UP );
			}
			else if ( ZoomTrigger == 0 )
			{
				CRSlide( ContentReaderBase.ManiState.NORMAL );
			}
		}

		private void CRSlide( ContentReaderBase.ManiState State )
		{
			if ( State == Container.CurrManiState ) return;

			switch ( State )
			{
				case ContentReaderBase.ManiState.NORMAL:
					Container.ReaderSlideBack();
					break;
				case ContentReaderBase.ManiState.UP:
					if ( Container.CurrManiState == ContentReaderBase.ManiState.DOWN )
						goto case ContentReaderBase.ManiState.NORMAL;

					Container.ReaderSlideUp();
					break;
				case ContentReaderBase.ManiState.DOWN:
					if ( Container.CurrManiState == ContentReaderBase.ManiState.UP )
						goto case ContentReaderBase.ManiState.NORMAL;

					Container.ReaderSlideDown();
					break;
			}

			Container.CurrManiState = State;
		}

		private void ContentGrid_Holding( object sender, HoldingRoutedEventArgs e )
		{
			if ( AccelerScroll.StateActive )
			{
				ACScroll.ForceBrake = true;
				Container.OverNavigate( typeof( Settings.CallibrateAcceler ), ACScroll );
			}
		}

	}
}