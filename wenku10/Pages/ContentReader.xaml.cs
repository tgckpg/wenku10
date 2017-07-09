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

using wenku8.CompositeElement;
using wenku8.Config;
using wenku8.Effects;
using wenku8.Model.Interfaces;
using wenku8.Model.Loaders;
using wenku8.Model.Book;
using wenku8.Model.Pages;
using wenku8.Model.Pages.ContentReader;
using wenku8.Model.ListItem;
using wenku8.Model.Section;
using wenku8.Resources;

using BgContext = wenku8.Settings.Layout.BookInfoView.BgContext;

namespace wenku10.Pages
{
	using ContentReaderPane;

	sealed partial class ContentReader : Page, ICmdControls, IAnimaPage, INavPage
	{
		public static readonly string ID = typeof( ContentReader ).Name;

#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get { return true; } }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		public BookItem CurrentBook { get; private set; }
		public Chapter CurrentChapter { get; private set; }
		public ReaderContent ContentView { get; private set; }
		public TimeSpan TimpSpan { get; private set; }

		private bool IsHorz { get { return ContentView.Reader.Settings.IsHorizontal; } }
		public bool UseInertia
		{
			get { return Properties.CONTENTREADER_USEINERTIA; }
			set { Properties.CONTENTREADER_USEINERTIA = value; }
		}

		private Action ReloadReader;
		private volatile bool OpenLock = false;
		private bool NeedRedraw = false;
		private bool Disposed = false;

		private ApplicationViewOrientation? Orientation;

		private EpisodeStepper ES;

		private NavPaneSection ContentPane;
		private wenku8.System.KeyboardController KbControls;

		private TextBlock BookTitle { get { return IsHorz ? YBookTitle : XBookTitle; } }
		private TextBlock VolTitle { get { return IsHorz ? YVolTitle : XVolTitle; } }
		private TitleStepper EpTitleStepper { get { return IsHorz ? YEpTitleStepper : XEpTitleStepper; } }
		private ListView HistoryThumbs { get { return IsHorz ? YHistoryThumbs : XHistoryThumbs; } }

		Rectangle OriIndicator;

		private ContentReader()
		{
			this.InitializeComponent();
		}

		public ContentReader( BookItem Book, Chapter C )
			: this()
		{
			SetTemplate();

			CurrentBook = Book;
			OpenBook( C );
		}

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

		public void SoftOpen() { KbControls.ShowHelp(); }
		public void SoftClose() { Dispose(); }

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			LayoutRoot.RenderTransform = new TranslateTransform();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		private void SetTemplate()
		{
			ContentIllusLoader.Initialize();

			LayoutRoot.RenderTransform = new TranslateTransform();

			NavigationHandler.InsertHandlerOnNavigatedBack( OnBackRequested );

			// First Trigger don't redraw
			TriggerOrientation();

			FocusHelper.DataContext = new AssistContext();
			App.ViewControl.PropertyChanged += VC_PropertyChanged;

			InitAppBar();

			KbControls = new wenku8.System.KeyboardController( "ContentReader" );
			// KeyBoard Navigations
			KbControls.AddCombo( "NextPara", e => ContentView.NextPara(), VirtualKey.J );
			KbControls.AddCombo( "PrevPara", e => ContentView.PrevPara(), VirtualKey.K );

			KbControls.AddCombo( "ScrollLess", e => ContentView.ScrollLess(), VirtualKey.Shift, VirtualKey.Up );
			KbControls.AddCombo( "ScrollMore", e => ContentView.ScrollMore(), VirtualKey.Shift, VirtualKey.Down );
			KbControls.AddCombo( "ScrollMore", e => ContentView.ScrollMore(), VirtualKey.Shift, VirtualKey.J );
			KbControls.AddCombo( "ScrollLess", e => ContentView.ScrollLess(), VirtualKey.Shift, VirtualKey.K );
			KbControls.AddCombo( "ScrollBottom", e => ContentView.GoTop(), VirtualKey.Shift, VirtualKey.G );
			KbControls.AddCombo( "ScrollCurrent", e => ContentView.GoCurrent(), VirtualKey.X );
			KbControls.AddSeq( "ScrollTop", e => ContentView.GoBottom(), VirtualKey.G, VirtualKey.G );

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

		private ApplicationViewOrientation? LastAwareOri;
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
				LocalRedraw = ( IsHorz && UpdatePane );
			}

			if ( UpdatePane && MainSplitView.State != PaneStates.Closed )
			{
				// This handles CommandBar goes hidden
				MainSplitView.OpenPane();
				MainSplitView.ClosePane();
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
			MainSplitView.ManiMode =
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
				Logger.Log( ID, "Oops, Chapter is null. Can't open nothing.", LogType.WARNING );
				return;
			}

			bool BookChanged = ( CurrentBook.Id != C.aid );
			if ( BookChanged )
			{
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

				// Fire up Episode stepper, used for stepping next episode
				if ( ES == null || ES.Chapter.aid != C.aid )
				{
					Shared.LoadMessage( "EpisodeStepper" );
					VolumeLoader VL = new VolumeLoader(
						( BookItem b ) =>
						{
							// Refresh the TOC if Book is changed
							if ( BookChanged ) ContentPane.SelectSection( ContentPane.Nav.First() );

							ES = new EpisodeStepper( new VolumesInfo( b ) );
							SetInfoTemplate();
						}
					);

					VL.Load( CurrentBook );
				}
				else
				{
					Worker.UIInvoke( () => SetInfoTemplate() );
				}

				ReloadReader = () =>
				{
					ContentFrame.Content = null;
					Shared.LoadMessage( "RedrawingContent" );
					ContentView?.Dispose();

					// Set Predefined BlockHeight if available
					if( 0 < Properties.APPEARANCE_CONTENTREADER_BLOCKHEIGHT )
					{
						VerticalLogaTable LogaTable = VerticalLogaManager.GetLoga( Properties.APPEARANCE_CONTENTREADER_FONTSIZE );
						LogaTable.Override( Properties.APPEARANCE_CONTENTREADER_BLOCKHEIGHT );
					}

					ContentView = new ReaderContent( this, Anchor );

					SetLayoutAware();

					ContentFrame.Content = ContentView;
					// Load Content at the very end
					ContentView.Load( false );
				};

				// Override reload here since
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
			if ( ContentBg.DataContext == null )
			{
				ContentBg.DataContext = ContentView.Reader.Settings.GetBgContext();
			}

			( ( BgContext ) ContentBg.DataContext ).Reload();

			BookTitle.Text = CurrentBook.Title;
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

			VolTitle.Text = ES.VolTitle;

			EpTitleStepper.UpdateDisplay();

			if ( ES != EpTitleStepper.Source )
			{
				EpTitleStepper.Source = ES;

				ES.PropertyChanged += ES_PropertyChanged;
			}

		}

		private void ES_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			OpenBook( ES.Chapter );
		}

		private async void YHistoryThumbs_ItemClick( object sender, ItemClickEventArgs e )
		{
			ActiveItem Item = ( ActiveItem ) e.ClickedItem;
			string Id = Item.Payload;

			ReaderSlideBack();
			if ( Id != CurrentBook.Id )
			{
				OpenMask();

				BookItem b = await ItemProcessor.GetBookFromId( Id );
				AsyncTryOut<Chapter> bAnchor = await PageProcessor.TryGetAutoAnchor( b );
				// AutoAnchor will be the first chapter if anchor is not available
				OpenBook( bAnchor.Out, false, -1, b );
			}
		}

		private void BookLoaded( BookItem b )
		{
			if ( ContentPane == null ) InitPane();
			new global::wenku8.History().Push( b );
		}

		public async void RenderComplete( IdleDispatchedHandlerArgs e )
		{
			RenderMask.State = ControlState.Foreatii;

			// Place a thumbnail to Reader history
			await wenku8.History.CreateThumbnail( ContentView, CurrentBook.Id );
		}

		private void MainGrid_DoubleTapped( object sender, DoubleTappedRoutedEventArgs e )
		{
			if ( ContentView.Reader.UsePageClick || !ContentView.Reader.UseDoubleTap ) return;
			RollOutLeftPane();
		}

		public void RollOutLeftPane()
		{
			// Config is open, do not roll out the pane
			if ( Overlay.State == ControlState.Reovia && OverlayFrame.Content is Settings.Themes.ContentReader ) return;
			ContentView.UserStartReading = false;
			MainSplitView.OpenPane();
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

			PaneGrid.DataContext = ContentPane;
			MainSplitView.PanelBackground = ContentPane.BackgroundBrush;

			Overlay.OnStateChanged += Overlay_OnStateChanged;
		}

		private PaneNavButton InertiaButton()
		{
			PaneNavButton InertiaButton = null;

			Action ToggleFIcon = () =>
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
			};

			InertiaButton = UseInertia
				? new PaneNavButton( new IconUseInertia() { AutoScale = true }, ToggleFIcon )
				: new PaneNavButton( new IconNoInertia() { AutoScale = true }, ToggleFIcon )
				;
			return InertiaButton;
		}

		private PaneNavButton FullScreenButton()
		{
			PaneNavButton FullScreenButton = null;

			Action ToggleFIcon = () =>
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
			};

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
				Width = 35, Height = 35
				, Stroke = new SolidColorBrush( Properties.APPEARENCE_THEME_RELATIVE_SHADES_COLOR )
				, StrokeThickness = 2
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
				Width = 20, Height = 40
				, Fill = new SolidColorBrush( Properties.APPEARENCE_THEME_RELATIVE_SHADES_COLOR )
			};

			MainSplitView.FlowDirection = Properties.APPEARANCE_CONTENTREADER_LEFTCONTEXT
				? FlowDirection.LeftToRight
				: FlowDirection.RightToLeft;

			RectInd.Margin = new Thickness( 10 );
			RectInd.VerticalAlignment = VerticalAlignment.Center;
			RectInd.HorizontalAlignment = ( FlowDirection == FlowDirection.LeftToRight )
				? HorizontalAlignment.Left
				: HorizontalAlignment.Right;

			Action ToggleFIcon = () =>
			{
				if ( MainSplitView.FlowDirection == FlowDirection.LeftToRight )
				{
					Properties.APPEARANCE_CONTENTREADER_LEFTCONTEXT = false;
					RectInd.HorizontalAlignment = HorizontalAlignment.Right;
					MainSplitView.FlowDirection = FlowDirection.RightToLeft;
				}
				else
				{
					Properties.APPEARANCE_CONTENTREADER_LEFTCONTEXT = true;
					RectInd.HorizontalAlignment = HorizontalAlignment.Left;
					MainSplitView.FlowDirection = FlowDirection.LeftToRight;
				}
			};

			FlowDirButton = new PaneNavButton( RectInd, ToggleFIcon );

			return FlowDirButton;
		}

		private void SectionClicked( object sender, ItemClickEventArgs e )
		{
			PaneNavButton Section = e.ClickedItem as PaneNavButton;
			ContentPane.SelectSection( Section );
		}

		internal void ClosePane()
		{
			// Detecting state could skip the Visual State Checking 
			if ( MainSplitView.State == PaneStates.Opened )
			{
				MainSplitView.State = PaneStates.Closed;
			}
		}

		private void Reload()
		{
			OpenBook( CurrentChapter, true );
		}

		private void GotoSettings()
		{
			MainSplitView.ClosePane();
			Overlay.State = ControlState.Reovia;
			OverlayFrame.Content = new Settings.Themes.ContentReader();
		}

		public void OverNavigate( Type Page, object Param )
		{
			Overlay.State = ControlState.Reovia;
			OverlayFrame.Navigate( Page, Param );
		}

		private void ToggleFullScreen()
		{
			App.ViewControl.ToggleFullScreen();
			NeedRedraw = true;
		}

		private void OnBackRequested( object sender, XBackRequestedEventArgs e )
		{
			// Close the settings first
			if ( Overlay.State == ControlState.Reovia )
			{
				Overlay.State = ControlState.Foreatii;
				Settings.Themes.ContentReader Settings = OverlayFrame.Content as Settings.Themes.ContentReader;
				MainSplitView.PanelBackground = ContentPane.BackgroundBrush;
				FocusHelper.DataContext = new AssistContext();

				// If the overlay frame content is settings, and the settings is changed
				if ( Settings != null && Settings.NeedRedraw ) Redraw();

				e.Handled = true;
				return;
			}

			if ( CurrManiState != ManiState.NORMAL || ContentSlideBack.GetCurrentState() == ClockState.Active )
			{
				e.Handled = true;
				ReaderSlideBack();
				return;
			}

			if ( MainSplitView.State == PaneStates.Opened )
			{
				e.Handled = true;
				MainSplitView.ClosePane();
				return;
			}
		}

		private void Overlay_OnStateChanged( object sender, ControlState args )
		{
			if ( args == ControlState.Foreatii )
			{
				OverlayFrame.Content = null;
				( ( BgContext ) ContentBg.DataContext )?.Reload();
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
			// var NOP = ContentFrame.Dispatcher.RunIdleAsync( new IdleDispatchedHandler( RenderComplete ) );
		}

		private void OpenMask()
		{
			StringResources stx = new StringResources( "LoadingMessage" );
			RenderMask.Text = stx.Str( "ProgressIndicator_Message" );
			RenderMask.State = ControlState.Reovia;
		}

		#region Clock

		private QClock RClock { get { return IsHorz ? YClock : XClock; } }
		private TextBlock Month { get { return IsHorz ? YMonth : XMonth; } }
		private TextBlock DayofWeek { get { return IsHorz ? YDayofWeek : XDayofWeek; } }
		private TextBlock DayofMonth { get { return IsHorz ? YDayofMonth : XDayofMonth; } }
		DispatcherTimer ClockTicker;

		private void SetClock()
		{
			if ( ClockTicker != null ) return;

			ClockTicker = new DispatcherTimer();
			ClockTicker.Interval = TimeSpan.FromSeconds( 5 );

			UpperBack.DataContext
				= LowerBack.DataContext
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

			( RClock.DataContext as ESContext ).Dispose();
			UpperBack.DataContext
				= LowerBack.DataContext
				= null;

			ClockTicker.Tick -= ClockTicker_Tick;
			ClockTicker = null;
			Battery.AggregateBattery.ReportUpdated -= AggregateBattery_ReportUpdated;
		}

		private void ClockTicker_Tick( object sender, object e ) { ClockTick(); }

		private void ClockTick()
		{
			RClock.Time = DateTime.Now;
			DayofWeek.Text = RClock.Time.ToString( "dddd" );
			DayofMonth.Text = RClock.Time.Day.ToString();
			Month.Text = RClock.Time.ToString( "MMMM" );
		}

		private void AggregateBattery_ReportUpdated( Battery sender, object args )
		{
			BatteryReport Report = sender.GetReport();

			if ( Report.RemainingCapacityInMilliwattHours == null ) return;
			Worker.UIInvoke( () =>
			{
				RClock.Progress = ( float ) Report.RemainingCapacityInMilliwattHours / ( float ) Report.FullChargeCapacityInMilliwattHours;
			} );
		}
		#endregion

	}
}