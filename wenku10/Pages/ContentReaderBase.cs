using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.UI;
using Net.Astropenguin.UI.Icons;

using GR.CompositeElement;
using GR.Config;
using GR.Database.Models;
using GR.Effects;
using GR.Model.Interfaces;
using GR.Model.Loaders;
using GR.Model.Book;
using GR.Model.Pages;
using GR.Model.Pages.ContentReader;
using GR.Model.ListItem;
using GR.Model.Section;
using GR.Resources;

namespace wenku10.Pages
{
	using ContentReaderPane;

	abstract class ContentReaderBase : Page, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get { return true; } }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; protected set; }
		public IList<ICommandBarElement> Major2ndControls { get; protected set; }
		public IList<ICommandBarElement> MinorControls { get; protected set; }

		public BookItem CurrentBook { get; protected set; }
		public Chapter CurrentChapter { get; protected set; }
		public ReaderContent ContentView { get; protected set; }
		public TimeSpan TimpSpan { get; protected set; }

		public bool UseInertia
		{
			get => GRConfig.ContentReader.UseInertia;
			set => GRConfig.ContentReader.UseInertia = value;
		}

		public bool OverlayActive => _Overlay.State == ControlState.Active;

		protected bool OrientationRedraw = false;

		protected Action ReloadReader;
		protected volatile bool OpenLock = false;
		protected bool NeedRedraw = false;
		protected bool Disposed = false;

		protected EpisodeStepper ES;

		protected NavPaneSection ContentPane;
		protected GR.GSystem.KeyboardController KbControls;

		protected TextBlock _BookTitle;
		protected TextBlock _VolTitle;
		protected TitleStepper _EpTitleStepper;
		protected ListView _HistoryThubms;

		protected FrameworkElement _LayoutRoot;
		protected Frame _OverlayFrame;
		protected Frame _ContentFrame;
		protected Grid _ContentBg;
		protected Grid _PaneGrid;
		protected Grid _FocusHelper;
		protected Grid _VESwipe;

		protected PassiveSplitView _MainSplitView;
		protected StateControl _Overlay;
		protected TipMask _RenderMask;

		private ApplicationViewOrientation? Orientation { get; set; }
		private ApplicationViewOrientation? LastAwareOri;

		Rectangle OriIndicator;

		void Dispose()
		{
			if ( Disposed ) return;
			Disposed = true;

			KbControls.Dispose();

			ClockStop();

			NavigationHandler.OnNavigatedBack -= OnBackRequested;
			Window.Current.SizeChanged -= Current_SizeChanged;
			App.ViewControl.PropertyChanged -= VC_PropertyChanged;

			CurrentBook = null;
			CurrentChapter = null;

			ContentView?.Dispose();
			ContentView = null;

			ES = null;
			ContentPane = null;
		}

		public void SoftOpen( bool NavForward )
		{
			if ( MainStage.Instance.IsPhone && !App.ViewControl.IsFullScreen )
			{
				App.ViewControl.ToggleFullScreen();
			}

			KbControls.ShowHelp();
		}

		public void SoftClose( bool NavForward )
		{
			if ( MainStage.Instance.IsPhone && App.ViewControl.IsFullScreen )
			{
				App.ViewControl.ToggleFullScreen();
			}

			Dispose();
		}

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, _LayoutRoot, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, _LayoutRoot.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			_LayoutRoot.RenderTransform = new TranslateTransform();

			SimpleStory.DoubleAnimation( AnimaStory, _LayoutRoot, "Opacity", 1, 0, 350, 0, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, _LayoutRoot.RenderTransform, "Y", 0, 30, 350, 0, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		protected void SetTemplate()
		{
			ContentIllusLoader.Initialize();

			_LayoutRoot.RenderTransform = new TranslateTransform();

			NavigationHandler.InsertHandlerOnNavigatedBack( OnBackRequested );

			// First Trigger don't redraw
			TriggerOrientation();

			_FocusHelper.DataContext = new AssistContext();
			App.ViewControl.PropertyChanged += VC_PropertyChanged;

			InitAppBar();

			KbControls = new GR.GSystem.KeyboardController( "ContentReader" );
			// KeyBoard Navigations
			KbControls.AddCombo( "NextPara", e => ContentView.NextPara(), VirtualKey.J );
			KbControls.AddCombo( "PrevPara", e => ContentView.PrevPara(), VirtualKey.K );

			KbControls.AddCombo( "ScrollLess", e => ContentView.ScrollLess(), VirtualKey.Shift, VirtualKey.Up );
			KbControls.AddCombo( "ScrollMore", e => ContentView.ScrollMore(), VirtualKey.Shift, VirtualKey.Down );
			KbControls.AddCombo( "ScrollMore", e => ContentView.ScrollMore(), VirtualKey.Shift, VirtualKey.J );
			KbControls.AddCombo( "ScrollLess", e => ContentView.ScrollLess(), VirtualKey.Shift, VirtualKey.K );
			KbControls.AddCombo( "ScrollCurrent", e => ContentView.GoCurrent(), VirtualKey.X );
			KbControls.AddCombo( "ScrollTop", e => ContentView.GoBottom(), VirtualKey.Shift, VirtualKey.G );
			KbControls.AddSeq( "ScrollBottom", e => ContentView.GoTop(), VirtualKey.G, VirtualKey.G );

			KbControls.AddCombo( "EPStepper", KeyboardSlideEp, VirtualKey.B );
			KbControls.AddCombo( "EPStepper", KeyboardSlideEp, VirtualKey.Space );

			KbControls.AddCombo( "PrevChapter", e => ChangeChapter( e, false ), VirtualKey.Left );
			KbControls.AddCombo( "NextChapter", e => ChangeChapter( e, true ), VirtualKey.Right );
			KbControls.AddCombo( "PrevChapter", e => ChangeChapter( e, false ), VirtualKey.H );
			KbControls.AddCombo( "NextChapter", e => ChangeChapter( e, true ), VirtualKey.L );
			KbControls.AddCombo( "UndoJump", e => ContentView.UndoJump(), VirtualKey.U );
			KbControls.AddCombo( "UndoJump", e => ContentView.UndoJump(), VirtualKey.Control, VirtualKey.Z );

			// `:
			KbControls.AddCombo( "ShowMenu", e => RollOutLeftPane(), ( VirtualKey ) 192 );
			KbControls.AddCombo( "ShowMenu", e => RollOutLeftPane(), VirtualKey.Shift, ( VirtualKey ) 186 );

			KbControls.AddCombo( "SearchWords", e => ContentView.SearchWords( ContentView.Reader.SelectedData ), VirtualKey.Shift, VirtualKey.S );

			SetSlideGesture();

			Window.Current.SizeChanged += Current_SizeChanged;

			if ( MainStage.Instance.IsPhone ) SizeChanged += ContentReader_SizeChanged;

		}

		private void InitAppBar()
		{
			MajorControls = new ICommandBarElement[ 0 ];
		}

		private void ChangeChapter( KeyCombinationEventArgs e, bool Next )
		{
			if ( AnyStoryActive ) return;
			if ( CurrManiState == ManiState.UP )
			{
				ContentBeginAway( Next );
			}
			else
			{
				KeyboardSlideEp( e );
			}
		}

		private void KeyboardSlideEp( KeyCombinationEventArgs e )
		{
			if ( CurrManiState == ManiState.NORMAL )
			{
				ReaderSlideUp();
			}
			else
			{
				ReaderSlideBack();
			}
		}

		private void VC_PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "IsFullScreen" ) NeedRedraw = true;
		}

		private void Current_SizeChanged( object sender, WindowSizeChangedEventArgs e )
		{
			TriggerOrientation();
		}

		private void ContentReader_SizeChanged( object sender, SizeChangedEventArgs e )
		{
			if ( e.PreviousSize != new Size( 0, 0 ) )
			{
				TriggerHorzLayoutAware( e.PreviousSize, e.NewSize );
			}

			LastAwareOri = Orientation;
		}

		private void TriggerHorzLayoutAware( Size Old, Size New )
		{
			// if ContentView is not present
			// then there is no need to redraw as ContentView is not drawn yet
			if ( ContentView == null ) return;

			bool UpdatePane = false;
			bool LocalRedraw = false;

			if ( LastAwareOri == Orientation )
			{
				UpdatePane = ( Old.Height != New.Height );
				LocalRedraw = ( OrientationRedraw && UpdatePane );
			}

			ContentView.ACScroll.UpdateOrientation( DisplayInformation.GetForCurrentView().CurrentOrientation );

			if ( UpdatePane && _MainSplitView.State != PaneStates.Closed )
			{
				// This handles CommandBar goes hidden
				_MainSplitView.OpenPane();
				_MainSplitView.ClosePane();
			}

			if ( LocalRedraw )
			{
				ReaderSlideBack();
			}
		}

		private void TriggerOrientation()
		{
			// This block will run once only
			if ( Orientation == null )
			{
				Orientation = App.ViewControl.Orientation;
				UpdateManiMode();
			}

			if ( NeedRedraw || Orientation != App.ViewControl.Orientation )
			{
				Orientation = App.ViewControl.Orientation;
				UpdateManiMode();
			}

			UpdateOriIndicator();
		}

		private void UpdateOriIndicator()
		{
			if ( OriIndicator == null ) return;

			if ( Orientation == ApplicationViewOrientation.Portrait )
			{
				OriIndicator.Width = 35;
				OriIndicator.Height = 45;
			}
			else
			{
				OriIndicator.Width = 45;
				OriIndicator.Height = 35;
			}
		}

		private void UpdateManiMode()
		{
			_MainSplitView.ManiMode =
				( MainStage.Instance.IsPhone && Orientation == ApplicationViewOrientation.Landscape )
				? ManipulationModes.TranslateY
				: ManipulationModes.TranslateX;
		}

		internal void OpenBookmark( BookmarkListItem item )
		{
			Chapter C = item.GetChapter();
			if ( C == null ) return;

			OpenBook( C, false, item.AnchorIndex );
		}

		public void OpenBook( Chapter C, bool Reload = false, int Anchor = -1, BookItem ToBook = null )
		{
			if ( OpenLock ) return;
			if ( C == null )
			{
				Logger.Log( "ContentReaderBase", "Oops, Chapter is null. Can't open nothing.", LogType.WARNING );
				return;
			}

			bool BookChanged = ( CurrentBook.Id != C.BookId );
			if ( BookChanged )
			{
				if ( CurrentBook.Entry.TextLayout != C.Book.TextLayout )
				{
					PageProcessor.NavigateToReader( ItemProcessor.GetBookItem( C.Book ), C );
					return;
				}
				CurrentBook = ToBook ?? throw new ArgumentException( "ToBook cannot be null while changing chapter accross book" );
			}

			if ( !Reload && C.Equals( CurrentChapter ) )
			{
				if ( Anchor != -1 )
				{
					ContentView.UserStartReading = false;
					ContentView.GotoIndex( Anchor );
				}

				return;
			}

			ClosePane();
			OpenMask();

			CurrentChapter = C;
			OpenLock = true;

			// Throw this into background as it is resources intensive
			Task.Run( () =>
			{
				BookLoader BL = new BookLoader( BookLoaded );
				BL.Load( CurrentBook, true );

				// Refresh the TOC if Book has changed
				if ( BookChanged )
				{
					Worker.UIInvoke( () => ContentPane.SelectSection( ContentPane.Nav.First() ) );
				}

				// Fire up Episode stepper, used for stepping next episode
				if ( ES == null || ES.Chapter.BookId != C.BookId )
				{
					Shared.LoadMessage( "EpisodeStepper" );
					ES = new EpisodeStepper( new VolumesInfo( CurrentBook ) );
				}

				Worker.UIInvoke( () => SetInfoTemplate() );

				ReloadReader = () =>
				{
					_ContentFrame.Content = null;
					Shared.LoadMessage( "RedrawingContent" );
					ContentView?.Dispose();

					// Set Predefined BlockHeight if available
					double BlockHeight = GRConfig.ContentReader.BlockHeight;
					if ( 0 < BlockHeight )
					{
						VerticalLogaTable LogaTable = VerticalLogaManager.GetLoga( GRConfig.ContentReader.FontSize );
						LogaTable.Override( BlockHeight );
					}

					ContentView = new ReaderContent( this, Anchor );

					SetLayoutAware();

					_ContentFrame.Content = ContentView;
					// Load Content at the very end
					ContentView.Load( false );
				};

				// Override reload here
				// Since the selected index just won't update
				if ( Reload )
				{
					ChapterLoader CL = new ChapterLoader( CurrentBook, x =>
					{
						OpenLock = false;
						Redraw();
					} );

					// if book is local, use the cache
					CL.Load( CurrentChapter, CurrentBook.IsLocal() );
				}
				else
				{
					Worker.UIInvoke( () =>
					{
						// Lock should be released before redrawing start
						OpenLock = false;
						Redraw();
					} );
				}

			} );
		}

		private void SetLayoutAware()
		{
			if ( _ContentBg.DataContext == null )
			{
				_ContentBg.DataContext = new BgContext( GRConfig.ContentReader.BgContext );
			}

			( ( BgContext ) _ContentBg.DataContext ).Reload();

			_BookTitle.Text = CurrentBook.Title;
			SetClock();
			SetManiState();

			SetLayoutAwareInfo();
		}

		private void SetInfoTemplate()
		{
			Shared.LoadMessage( "SettingEpisodeStepper" );
			ES.SetCurrentPosition( CurrentChapter );
			if ( ContentPane != null && ContentPane.Context is TableOfContents )
			{
				( ContentPane.Context as TableOfContents ).UpdateDisplay();
			}

			SetLayoutAwareInfo();
		}

		private void SetLayoutAwareInfo()
		{
			if ( ContentView == null || ES == null ) return;

			_VolTitle.Text = ES.VolTitle;

			_EpTitleStepper.UpdateDisplay();

			if ( ES != _EpTitleStepper.Source )
			{
				_EpTitleStepper.Source = ES;

				ES.PropertyChanged += ES_PropertyChanged;
			}

		}

		private void ES_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			OpenBook( ES.Chapter );
		}

		protected async void HistoryThumbs_ItemClick( object sender, ItemClickEventArgs e )
		{
			ActiveItem Item = ( ActiveItem ) e.ClickedItem;
			Book Bk = ( Book ) Item.RawPayload;

			ReaderSlideBack();
			if ( Bk.Id != CurrentBook.Id )
			{
				OpenMask();

				BookItem b = ItemProcessor.GetBookItem( Bk );
				AsyncTryOut<Chapter> bAnchor = await PageProcessor.TryGetAutoAnchor( b );
				// AutoAnchor will be the first chapter if anchor is not available
				OpenBook( bAnchor.Out, false, -1, b );
			}
		}

		private void BookLoaded( BookItem b )
		{
			if ( ContentPane == null ) InitPane();
			b.Entry.LastAccess = DateTime.UtcNow;
			b.SaveInfo();
		}

		public async void RenderComplete( IdleDispatchedHandlerArgs e )
		{
			CloseMask();

			// Place a thumbnail to Reader history
			if ( CurrentBook != null )
			{
				await GR.History.CreateThumbnail( ContentView, CurrentBook.PathId );
			}
		}

		protected void MainGrid_DoubleTapped( object sender, DoubleTappedRoutedEventArgs e )
		{
			if ( ContentView.Reader.UsePageClick || !ContentView.Reader.UseDoubleTap ) return;
			RollOutLeftPane();
		}

		public void RollOutLeftPane()
		{
			// Overlay frame is active, do not roll out the pane
			if ( _Overlay.State == ControlState.Active )
				return;

			ContentView.UserStartReading = false;
			_MainSplitView.OpenPane();
		}

		private void InitPane()
		{
			List<PaneNavButton> Sections = new List<PaneNavButton>();
			Sections.Add( new PaneNavButton( new IconTOC() { AutoScale = true }, typeof( TableOfContents ) ) );
			Sections.Add( new PaneNavButton( new IconBookmark() { AutoScale = true }, typeof( BookmarkList ) ) );
			Sections.Add( new PaneNavButton( new IconImage() { AutoScale = true }, typeof( ImageList ) ) );
			Sections.Add( new PaneNavButton( new IconReload() { AutoScale = true }, Reload ) );
			Sections.Add( InertiaButton() );

			if ( MainStage.Instance.IsPhone )
			{
				Sections.Add( RotationButton() );
			}
			else
			{
				Sections.Add( FullScreenButton() );
			}

			Sections.Add( FlowDirButton() );
			Sections.Add( new PaneNavButton( new IconSettings() { AutoScale = true }, GotoSettings ) );

			ContentPane = new NavPaneSection( this, Sections );
			ContentPane.SelectSection( Sections[ 0 ] );

			_PaneGrid.DataContext = ContentPane;
			_MainSplitView.PanelBackground = ContentPane.BackgroundBrush;

			_Overlay.OnStateChanged += Overlay_OnStateChanged;
		}

		private PaneNavButton InertiaButton()
		{
			PaneNavButton InertiaButton = null;

			void ToggleFIcon()
			{
				if ( UseInertia = !UseInertia )
				{
					InertiaButton.UpdateIcon( new IconUseInertia() { AutoScale = true } );
				}
				else
				{
					InertiaButton.UpdateIcon( new IconNoInertia() { AutoScale = true } );
				}
				ContentView.ToggleInertia();
			}

			InertiaButton = UseInertia
				? new PaneNavButton( new IconUseInertia() { AutoScale = true }, ToggleFIcon )
				: new PaneNavButton( new IconNoInertia() { AutoScale = true }, ToggleFIcon )
				;
			return InertiaButton;
		}

		private PaneNavButton FullScreenButton()
		{
			PaneNavButton FullScreenButton = null;

			void ToggleFIcon()
			{
				ToggleFullScreen();
				if ( App.ViewControl.IsFullScreen )
				{
					FullScreenButton.UpdateIcon( new IconRetract() { AutoScale = true } );
				}
				else
				{
					FullScreenButton.UpdateIcon( new IconExpand() { AutoScale = true } );
				}
			}

			FullScreenButton = App.ViewControl.IsFullScreen
				? new PaneNavButton( new IconRetract() { AutoScale = true }, ToggleFIcon )
				: new PaneNavButton( new IconExpand() { AutoScale = true }, ToggleFIcon )
				;
			return FullScreenButton;
		}

		private PaneNavButton RotationButton()
		{
			PaneNavButton RotationButton = null;

			Grid RGrid = new Grid();
			OriIndicator = new Rectangle
			{
				Width = 35,
				Height = 35,
				Stroke = new SolidColorBrush( GRConfig.Theme.RelColorShades ),
				StrokeThickness = 2
			};

			UpdateOriIndicator();

			RGrid.Children.Add( OriIndicator );

			bool Locked = (
				DisplayInformation.AutoRotationPreferences == ( DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped )
				|| DisplayInformation.AutoRotationPreferences == ( DisplayOrientations.PortraitFlipped | DisplayOrientations.Portrait )
			);

			FontIcon LockIcon = new FontIcon() { Glyph = Locked ? SegoeMDL2.Lock : SegoeMDL2.Unlock };
			LockIcon.Foreground = OriIndicator.Stroke;

			LockIcon.VerticalAlignment
				= OriIndicator.VerticalAlignment
				= VerticalAlignment.Center;

			LockIcon.HorizontalAlignment
				= OriIndicator.HorizontalAlignment
				= HorizontalAlignment.Center;

			RGrid.Children.Add( LockIcon );

			Action ToggleFIcon = () =>
			{
				if ( Locked )
				{
					Locked = false;
					DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
					LockIcon.Glyph = SegoeMDL2.Unlock;
				}
				else
				{
					Locked = true;
					DisplayInformation.AutoRotationPreferences = ( Orientation == ApplicationViewOrientation.Portrait )
						? ( DisplayOrientations.Portrait | DisplayOrientations.PortraitFlipped )
						: ( DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped )
					;

					LockIcon.Glyph = SegoeMDL2.Lock;
				}
			};

			RotationButton = new PaneNavButton( RGrid, ToggleFIcon );

			return RotationButton;
		}

		private PaneNavButton FlowDirButton()
		{
			PaneNavButton FlowDirButton = null;

			Rectangle RectInd = new Rectangle()
			{
				Width = 20,
				Height = 40,
				Fill = new SolidColorBrush( GRConfig.Theme.RelColorShades )
			};

			_MainSplitView.FlowDirection = GRConfig.ContentReader.LeftContext
				? FlowDirection.LeftToRight
				: FlowDirection.RightToLeft;

			RectInd.Margin = new Thickness( 10 );
			RectInd.VerticalAlignment = VerticalAlignment.Center;
			RectInd.HorizontalAlignment = ( FlowDirection == FlowDirection.LeftToRight )
				? HorizontalAlignment.Left
				: HorizontalAlignment.Right;

			Action ToggleFIcon = () =>
			{
				if ( _MainSplitView.FlowDirection == FlowDirection.LeftToRight )
				{
					GRConfig.ContentReader.LeftContext = false;
					RectInd.HorizontalAlignment = HorizontalAlignment.Right;
					_MainSplitView.FlowDirection = FlowDirection.RightToLeft;
				}
				else
				{
					GRConfig.ContentReader.LeftContext = true;
					RectInd.HorizontalAlignment = HorizontalAlignment.Left;
					_MainSplitView.FlowDirection = FlowDirection.LeftToRight;
				}
			};

			FlowDirButton = new PaneNavButton( RectInd, ToggleFIcon );

			return FlowDirButton;
		}

		protected void SectionClicked( object sender, ItemClickEventArgs e )
		{
			PaneNavButton Section = e.ClickedItem as PaneNavButton;
			ContentPane.SelectSection( Section );
		}

		internal void ClosePane()
		{
			// Detecting state could skip the Visual State Checking 
			if ( _MainSplitView.State == PaneStates.Opened )
			{
				_MainSplitView.State = PaneStates.Closed;
			}
		}

		private void Reload()
		{
			OpenBook( CurrentChapter, true );
		}

		private void GotoSettings()
		{
			_MainSplitView.ClosePane();
			_Overlay.State = ControlState.Active;
			_OverlayFrame.Content = new Settings.Themes.ContentReader();
		}

		public void OverNavigate( Type Page, object Param )
		{
			_Overlay.State = ControlState.Active;
			_OverlayFrame.Navigate( Page, Param );
		}

		private void ToggleFullScreen()
		{
			App.ViewControl.ToggleFullScreen();
			NeedRedraw = true;
		}

		private void OnBackRequested( object sender, XBackRequestedEventArgs e )
		{
			// Close the settings first
			if ( _Overlay.State == ControlState.Active )
			{
				_Overlay.State = ControlState.Closed;
				_MainSplitView.PanelBackground = ContentPane.BackgroundBrush;
				_FocusHelper.DataContext = new AssistContext();

				// If the overlay frame content is settings, and the settings is changed
				if ( _OverlayFrame.Content is Settings.Themes.ContentReader Settings && Settings.NeedRedraw )
				{
					Redraw();
				}
				else if( _OverlayFrame.Content is Settings.Advanced.LocalTableEditor TableEditor && TableEditor.NeedRedraw )
				{
					Redraw();
				}
				else if( _OverlayFrame.Content is Settings.CallibrateAcceler Callibrator )
				{
					Callibrator.EndCallibration();
				}

				e.Handled = true;
				return;
			}

			if ( CurrManiState != ManiState.NORMAL || _ContentSlideBack.GetCurrentState() == ClockState.Active )
			{
				e.Handled = true;
				ReaderSlideBack();
				return;
			}

			if ( _MainSplitView.State == PaneStates.Opened )
			{
				e.Handled = true;
				_MainSplitView.ClosePane();
				return;
			}
		}

		private void Overlay_OnStateChanged( object sender, ControlState args )
		{
			if ( args == ControlState.Closed )
			{
				_OverlayFrame.Content = null;
				( ( BgContext ) _ContentBg.DataContext )?.Reload();
			}
		}

		private void Redraw()
		{
			// When Open operation is processing you should not do any redraw before opening

			if ( OpenLock ) return;
			OpenMask();
			ReloadReader();

			// No need to RenderComplete since this is handled by
			// property changed Data event in ReaderView
			// await Task.Delay( 2000 );
			// var NOP = _ContentFrame.Dispatcher.RunIdleAsync( new IdleDispatchedHandler( RenderComplete ) );
		}

		private void OpenMask()
		{
			StringResources stx = StringResources.Load( "LoadingMessage" );
			_RenderMask.Text = stx.Str( "ProgressIndicator_Message" );
			_RenderMask.State = ControlState.Active;
		}

		private void CloseMask()
		{
			_RenderMask.State = ControlState.Closed;
		}

		#region Clock
		protected QClock _Clock;
		protected TextBlock _Month;
		protected TextBlock _DayofWeek;
		protected TextBlock _DayofMonth;
		DispatcherTimer ClockTicker;

		private void SetClock()
		{
			if ( ClockTicker != null ) return;

			ClockTicker = new DispatcherTimer();
			ClockTicker.Interval = TimeSpan.FromSeconds( 5 );

			_UpperBack.DataContext
				= _LowerBack.DataContext
				= new ESContext();

			ClockTick();
			ClockTicker.Tick += ClockTicker_Tick;
			ClockTicker.Start();

			AggregateBattery_ReportUpdated( Battery.AggregateBattery, null );
			Battery.AggregateBattery.ReportUpdated += AggregateBattery_ReportUpdated;
		}

		private void ClockStop()
		{
			if ( ClockTicker == null ) return;
			ClockTicker.Stop();

			_UpperBack.DataContext
				= _LowerBack.DataContext
				= null;

			ClockTicker.Tick -= ClockTicker_Tick;
			ClockTicker = null;
			Battery.AggregateBattery.ReportUpdated -= AggregateBattery_ReportUpdated;
		}

		private void ClockTicker_Tick( object sender, object e ) { ClockTick(); }

		private void ClockTick()
		{
			_Clock.Time = DateTime.Now;
			_DayofWeek.Text = _Clock.Time.ToString( "dddd" );
			_DayofMonth.Text = _Clock.Time.Day.ToString();
			_Month.Text = _Clock.Time.ToString( "MMMM" );
		}

		private void AggregateBattery_ReportUpdated( Battery sender, object args )
		{
			BatteryReport Report = sender.GetReport();

			if ( Report.RemainingCapacityInMilliwattHours == null ) return;
			Worker.UIInvoke( () =>
			{
				_Clock.Progress = ( float ) Report.RemainingCapacityInMilliwattHours / ( float ) Report.FullChargeCapacityInMilliwattHours;
			} );
		}
		#endregion

		#region Swipe Controller
		public enum ManiState { UP, NORMAL, DOWN }
		public ManiState CurrManiState = ManiState.NORMAL;

		protected double ZoomTrigger = 0;

		protected Storyboard ContentAway;
		protected Storyboard _ContentRestore;
		protected Storyboard _ContentSlideUp;
		protected Storyboard _ContentSlideDown;
		protected Storyboard _ContentSlideBack;
		protected CompositeTransform _CGTransform;

		protected double MaxVT = double.PositiveInfinity;
		protected double MinVT = double.NegativeInfinity;

		protected double VT = 130;

		protected Grid _UpperBack;
		protected Grid _LowerBack;

		protected bool AnyStoryActive
		{
			get
			{
				return new Storyboard[] {
					_ContentSlideBack, _ContentSlideUp, ContentAway
				}.Any( x => x?.GetCurrentState() == ClockState.Active );
			}
		}

		protected void SetSlideGesture()
		{
			_ContentSlideBack.Completed += ( s, e ) => CurrManiState = ManiState.NORMAL;
			_ContentSlideUp.Completed += ( s, e ) => CurrManiState = ManiState.UP;
			_ContentSlideDown.Completed += ( s, e ) => CurrManiState = ManiState.DOWN;

			_VESwipe.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY;
			_VESwipe.ManipulationStarted += VEManiStart;
		}

		protected void SetManiState()
		{
			_CGTransform.TranslateX = _CGTransform.TranslateY = 0;
			_CGTransform.ScaleX = _CGTransform.ScaleY = 1;
			_ContentSlideBack.Stop();
			_ContentSlideDown.Stop();
			_ContentSlideUp.Stop();
		}

		abstract protected void ContentBeginAway( bool Next );

		protected void StepPrevTitle()
		{
			if ( CurrManiState == ManiState.UP ) _EpTitleStepper.Prev();
		}

		protected void StepNextTitle()
		{
			if ( CurrManiState == ManiState.UP ) _EpTitleStepper.Next();
		}

		protected void VEManiStart( object sender, ManipulationStartedRoutedEventArgs e )
		{
			_CGTransform.SetValue( CompositeTransform.TranslateXProperty, _CGTransform.GetValue( CompositeTransform.TranslateXProperty ) );
			_CGTransform.SetValue( CompositeTransform.TranslateYProperty, _CGTransform.GetValue( CompositeTransform.TranslateYProperty ) );
			_ContentRestore.Stop();
		}

		protected void VEZoomBackUp( double dv )
		{
			ZoomTrigger += dv;
			if ( 100 < ZoomTrigger ) ReaderSlideBack();
			else if ( ZoomTrigger < 0 ) ZoomTrigger = 0;
		}

		protected void VEZoomBackDown( double dv )
		{
			ZoomTrigger += dv;
			if ( ZoomTrigger < -100 ) ReaderSlideBack();
			else if ( 0 < ZoomTrigger ) ZoomTrigger = 0;
		}

		protected void StopZoom()
		{
			ZoomTrigger = 0;
			_VESwipe.IsHitTestVisible = false;
			_VESwipe.ManipulationDelta -= ManiZoomBackUp;
			_VESwipe.ManipulationDelta -= ManiZoomBackDown;
			_VESwipe.ManipulationCompleted -= ManiZoomEnd;
		}

		abstract protected void ManiZoomBackUp( object sender, ManipulationDeltaRoutedEventArgs e );
		abstract protected void ManiZoomBackDown( object sender, ManipulationDeltaRoutedEventArgs e );
		abstract protected void ManiZoomEnd( object sender, ManipulationCompletedRoutedEventArgs e );

		protected void StartZoom( bool Up )
		{
			ZoomTrigger = 0;
			_VESwipe.IsHitTestVisible = true;

			_CGTransform.TranslateX = _CGTransform.TranslateY = 0;
			_CGTransform.ScaleX = _CGTransform.ScaleY = 1;
			_ContentRestore.Stop();

			if ( Up )
			{
				MaxVT = VT - 1;
				MinVT = 1 - VT;
			}
			else
			{
				MaxVT = ES.PrevStepAvailable() ? double.PositiveInfinity : ( VT - 1 );
				MinVT = ES.NextStepAvailable() ? double.NegativeInfinity : ( 1 - VT );
			}

			_VESwipe.ManipulationCompleted += ManiZoomEnd;
			if ( Up ) _VESwipe.ManipulationDelta += ManiZoomBackDown;
			else _VESwipe.ManipulationDelta += ManiZoomBackUp;
		}

		public void ReaderSlideBack()
		{
			if ( _ContentSlideBack.GetCurrentState() != ClockState.Active )
			{
				StopZoom();
				_ContentRestore.Begin();
				_ContentSlideBack.Begin();
				TransitionDisplay.SetState( _VolTitle, TransitionState.Inactive );
				TransitionDisplay.SetState( _BookTitle, TransitionState.Inactive );
				TransitionDisplay.SetState( _LowerBack, TransitionState.Inactive );
				TransitionDisplay.SetState( _UpperBack, TransitionState.Inactive );

				// Compensate for Storyboard.Completed event not firing
				CurrManiState = ManiState.NORMAL;
			}
		}

		public void ReaderSlideUp()
		{
			if ( _ContentSlideUp.GetCurrentState() != ClockState.Active )
			{
				StartZoom( false );
				_ContentSlideUp.Begin();
				TransitionDisplay.SetState( _VolTitle, TransitionState.Active );
				TransitionDisplay.SetState( _BookTitle, TransitionState.Inactive );
				TransitionDisplay.SetState( _LowerBack, TransitionState.Active );
				TransitionDisplay.SetState( _UpperBack, TransitionState.Inactive );
			}
		}

		public void ReaderSlideDown()
		{
			if ( _ContentSlideDown.GetCurrentState() != ClockState.Active )
			{
				StartZoom( true );
				_HistoryThubms.ItemsSource = new GR.History().GetListItems();

				_ContentSlideDown.Begin();
				TransitionDisplay.SetState( _VolTitle, TransitionState.Inactive );
				TransitionDisplay.SetState( _BookTitle, TransitionState.Active );
				TransitionDisplay.SetState( _LowerBack, TransitionState.Inactive );
				TransitionDisplay.SetState( _UpperBack, TransitionState.Active );
			}
		}
		#endregion
	}
}