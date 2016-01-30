using System;
using System.Collections.Generic;
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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.UI;
using Net.Astropenguin.UI.Icons;

using wenku8.CompositeElement;
using wenku8.Ext;
using wenku8.Model.Loaders;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.ListItem;
using wenku8.Model.Section;
using wenku8.Resources;

namespace wenku10.Pages
{
    using ContentReaderPane;
    sealed partial class ContentReader : Page
    {
        public static readonly string ID = typeof( ContentReader ).Name;

        public BookItem CurrentBook { get; private set; }
        public Chapter CurrentChapter { get; private set; }
        public ReaderContent ContentView { get; private set; }
        public TimeSpan TimpSpan { get; private set; }

        public bool UseInertia = false;

        private Action ReloadReader;
        private bool OpenLock = false;
        private bool NeedRedraw = false;
        private bool Disposed = true;

        private ApplicationViewOrientation? Orientation;

        private EpisodeStepper ES;

        private NavPaneSection ContentPane;
        private List<Action> RegKey;

        public ContentReader()
        {
            this.InitializeComponent();
        }

        ~ContentReader() { Dispose(); }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            if( Disposed )
            {
                Disposed = false;
                NavigationHandler.InsertHandlerOnNavigatedBack( OnBackRequested );

                // First Trigger won't need redraw
                TriggerOrientation();
                SetTemplate();
                Window.Current.SizeChanged += Current_SizeChanged;
            }

            OpenBook( e.Parameter as Chapter );
        }

        void Dispose()
        {
            if ( Disposed ) return;
            Disposed = true;

            foreach ( Action p in RegKey ) p();

            NavigationHandler.OnNavigatedBack -= OnBackRequested;
            Window.Current.SizeChanged -= Current_SizeChanged;
            App.ViewControl.PropertyChanged -= VC_PropertyChanged;
            CurrentBook = null;
            CurrentChapter = null;
            ContentView = null;
            ES = null;
            ContentPane = null;

            try
            {
                VolStepper.ItemsSource = null;
                EPStepper.ItemsSource = null;
            }
            catch( Exception ) { }

        }

        private void SetTemplate()
        {
            FocusHelper.DataContext = new global::wenku8.Model.Pages.ContentReader.AssistContext();
            App.ViewControl.PropertyChanged += VC_PropertyChanged;

            RegKey = new List<Action>();
            // KeyBoard Navigations
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => ContentView.NextPara(), Windows.System.VirtualKey.J ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => ContentView.PrevPara(), Windows.System.VirtualKey.K ) );

            RegKey.Add( App.KeyboardControl.RegisterCombination( e => ContentView.ScrollLess(), Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.Up ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => ContentView.ScrollMore(), Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.Down ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => ContentView.ScrollMore(), Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.J ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => ContentView.ScrollLess(), Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.K ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( ScrollBottom, Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.G ) );
            RegKey.Add( App.KeyboardControl.RegisterSequence( ScrollTop, Windows.System.VirtualKey.G, Windows.System.VirtualKey.G  ) );

            RegKey.Add( App.KeyboardControl.RegisterCombination( PrevChapter, Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.Left ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( NextChapter, Windows.System.VirtualKey.Shift, Windows.System.VirtualKey.Right ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( PrevChapter, Windows.System.VirtualKey.H ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( NextChapter, Windows.System.VirtualKey.L ) );

            // `:
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => RollOutLeftPane(), ( Windows.System.VirtualKey ) 192 ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => RollOutLeftPane(), Windows.System.VirtualKey.Shift, ( Windows.System.VirtualKey ) 186 ) );
        }

        private void NextChapter( KeyCombinationEventArgs e )
        {
            ES.stepNext();
            OpenBook( ES.Chapter );
        }

        private void PrevChapter( KeyCombinationEventArgs e )
        {
            ES.stepPrev();
            OpenBook( ES.Chapter );
        }

        private void VC_PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == "IsFullScreen" ) NeedRedraw = true;
        }

        private void Current_SizeChanged( object sender, WindowSizeChangedEventArgs e )
        {
            TriggerOrientation();
        }

        private void ScrollTop( KeyCombinationEventArgs obj )
        {
            ContentView.GoTop();
        }

        private void ScrollBottom( KeyCombinationEventArgs obj )
        {
            ContentView.GoBottom();
        }

        private void TriggerOrientation()
        {
            if( Orientation == null )
            {
                Orientation = App.ViewControl.Orientation;
            }

            if ( NeedRedraw || Orientation != App.ViewControl.Orientation )
            {
                Orientation = App.ViewControl.Orientation;
                NeedRedraw = false;
                Redraw();
            }
        }

        internal void OpenBookmark( BookmarkListItem item )
        {
            Chapter C = item.GetChapter();
            if ( C == null ) return;

            OpenBook( C, false, item.AnchorIndex );
        }

        public void OpenBook( Chapter C, bool Reload = false, int Anchor = -1 )
        {
            if ( OpenLock ) return;
            if ( C == null )
            {
                Logger.Log( ID, "Oops, Chapter is null. Can't open nothing.", LogType.WARNING );
                return;
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
                if ( CurrentBook == null || C.aid != CurrentBook.Id )
                {
                    Shared.LoadMessage( "BookConstruct" );

                    if ( C is SChapter )
                    {
                        CurrentBook = new BookInstruction( C as SChapter );
                    }
                    else
                    {
                        CurrentBook = X.Instance<BookItem>( XProto.BookItemEx, C );
                    }
                }

                BookLoader BL = new BookLoader( BookLoaded );
                BL.Load( CurrentBook, true );

                // Fire up Episode stepper, used for stepping next episode
                if ( ES == null || ES.Chapter.aid != C.aid )
                {
                    Shared.LoadMessage( "EpisodeStepper" );
                    VolumeLoader VL = new VolumeLoader(
                        ( BookItem b ) =>
                        {
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
                    ContentView = new ReaderContent( this, Anchor );
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
                    CL.Load( CurrentChapter, CurrentBook.IsLocal );
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

        private void SetInfoTemplate()
        {
            Shared.LoadMessage( "SettingEpisodeStepper" );
            if ( VolStepper.ItemsSource != null )
            {
                SelectCurrentEp();
                return;
            }

            List<ActiveItem> Vols = new List<ActiveItem>();
            List<ActiveItem> Eps = new List<ActiveItem>();

            string pVid = "";
            for( ES.Rewind(); ES.NextStepAvailable(); ES.stepNext() )
            {
                if( ES.currentVid != pVid )
                {
                    pVid = ES.currentVid;
                    Vols.Add( new ActiveItem( ES.CurrentVolTitle, "", ES.currentVid ) );
                }

                Eps.Add( new ActiveItem( ES.currentEpTitle, "", ES.currentCid ) );
            }

            VolStepper.ItemsSource = Vols;
            EPStepper.ItemsSource = Eps;
            SelectCurrentEp();

            ES.SetCurrentPosition( CurrentChapter );
        }

        private void SelectCurrentEp()
        {
            List<ActiveItem> Eps = EPStepper.ItemsSource as List<ActiveItem>;
            ChangedManually = false;
            EPStepper.SelectedItem = Eps.First( x => x.Payload == CurrentChapter.cid ); 
        }

        private void BookLoaded( BookItem b )
        {
            if ( ContentPane == null ) InitPane();
            new global::wenku8.History().Push( b );
        }

        public void RenderComplete( IdleDispatchedHandlerArgs e )
        {
            RenderMask.State = ControlState.Foreatii;
            InfoMask.State = ControlState.Foreatii;
        }

        private void MainGrid_DoubleTapped( object sender, DoubleTappedRoutedEventArgs e )
        {
            if ( ContentView.Reader.UsePageClick ) return;
            RollOutLeftPane();
        }

        private void RollOutLeftPane()
        {
            // Config is open, do not roll out the pane
            if ( Config.State == ControlState.Reovia ) return;
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
            Sections.Add( new PaneNavButton( new IconFastForword() { AutoScale = true }, () => ContentView.GoTop() ) );
            Sections.Add( new PaneNavButton(
                new IconFastForword() { AutoScale = true, Direction = Direction.MirrorVertical }
                , () => ContentView.GoBottom()
            ) );

            Sections.Add( InertiaButton() );
            Sections.Add( FullScreenButton() );
            Sections.Add( new PaneNavButton( new IconSettings() { AutoScale = true }, GotoSettings ) );

            ContentPane = new NavPaneSection( this, Sections );
            ContentPane.SelectSection( Sections[ 0 ] );

            PaneGrid.DataContext = ContentPane;
            MainSplitView.PanelBackground = ContentPane.BackgroundBrush;
        }

        private PaneNavButton InertiaButton()
        {
            PaneNavButton InertiaButton = null;

            Action ToggleFIcon = () =>
            {
                if( UseInertia = !UseInertia )
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
                if( App.ViewControl.IsFullScreen )
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
            Config.State = ControlState.Reovia;
            ConfigPopup.Content = new Settings.Themes.ContentReader();
        }

        private void ToggleFullScreen()
        {
            App.ViewControl.ToggleFullScreen();
            NeedRedraw = true;
        }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            // Close the settings first
            if ( Config.State == ControlState.Reovia )
            {
                Config.State = ControlState.Foreatii;
                Settings.Themes.ContentReader Settings = ConfigPopup.Content as Settings.Themes.ContentReader;
                MainSplitView.PanelBackground = ContentPane.BackgroundBrush;
                FocusHelper.DataContext = new global::wenku8.Model.Pages.ContentReader.AssistContext();

                if ( Settings.NeedRedraw ) Redraw();

                ConfigPopup.Content = null;
                e.Handled = true;
                return;
            }

            if ( MainSplitView.State == PaneStates.Opened )
            {
                MainSplitView.ClosePane();
                e.Handled = true;
                return;
            }

            // Popup info mask
            if( InfoMask.State == ControlState.Foreatii )
            {
                InfoMask.State = ControlState.Reovia;
                e.Handled = true;
                return;
            }

            StringResources stx = new StringResources( "LoadingMessage" );
            RenderMask.Text = stx.Text( "ProgressIndicator_PleaseWait" );
            RenderMask.HandleBack( Frame, e );
            Dispose();
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

        private void BeginRead( object sender, TappedRoutedEventArgs e )
        {
            InfoMask.State = ControlState.Foreatii;
        }

        private bool ChangedManually = true;
        private void EPStepper_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count < 1 ) return;

            string EP = ( e.AddedItems[ 0 ] as ActiveItem ).Payload;

            List<ActiveItem> Vols = VolStepper.ItemsSource as List<ActiveItem>;
            if ( Vols == null ) return;

            string Vid = null;
            for ( ES.Rewind(); ES.NextStepAvailable(); ES.stepNext() )
            {
                if ( ES.currentCid == EP )
                {
                    Vid = ES.currentVid;
                    break;
                }
            }

            if ( Vid == null ) return;

            if( ( VolStepper.SelectedItem as ActiveItem ).Payload != Vid )
            {
                ChangedManually = false;
                VolStepper.SelectedItem = Vols.First( x => x.Payload == Vid );
            }

            var j = EPStepper.Dispatcher.RunIdleAsync(
                ( x ) => OpenBook( ES.Chapter )
            );
        }

        private void VolStepper_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count < 1 ) return;

            string Vol = ( e.AddedItems[ 0 ] as ActiveItem ).Payload;

            List<ActiveItem> Eps = EPStepper.ItemsSource as List<ActiveItem>;
            if ( Eps == null ) return;

            if( !ChangedManually )
            {
                ChangedManually = true;
                return;
            }

            string cid = null;
            for( ES.Rewind(); ES.NextStepAvailable(); ES.stepNext() )
            {
                if ( ES.currentVid == Vol )
                {
                    cid = ES.currentCid;
                    break;
                }
            }

            ActiveItem ShouldBeEp = Eps.First( x => x.Payload == cid );

            if( EPStepper.SelectedItem != ShouldBeEp )
            {
                EPStepper.SelectedItem = ShouldBeEp;
            }
        }
    }
}
