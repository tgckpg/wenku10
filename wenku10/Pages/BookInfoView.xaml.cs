using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
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

using wenku8.Ext;
using wenku8.CompositeElement;
using wenku8.Model.Book;
using wenku8.Model.Comments;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Section;
using wenku8.Storage;

namespace wenku10.Pages
{
    public sealed partial class BookInfoView : Page
    {
        public static readonly string ID = typeof( BookInfoView ).Name;

        public static BookInfoView Instance;

        internal BookItem ThisBook;

        private TOCSection TOCData;
        private ListView VolList;
        private ReviewsSection ReviewsSection;
        private global::wenku8.Settings.Layout.BookInfoView LayoutSettings;

        private bool SkipThisPage = false;
        private bool useCache = true;
        private bool _inSync = false;

        private bool SyncStarted
        {
            get { return _inSync; }
            set
            {
                _inSync = value;
                if( InSync != null ) InSync.IsActive = _inSync;
            }
        }

        private List<string> ViewOrder;

        private ProgressRing InSync;
        private Grid InfoBgGrid;
        private Grid PushGrid;

        public BookInfoView()
        {
            InitializeComponent();
            ReorderModules();
        }

        ~BookInfoView() { Dispose(); }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            BackMask.HandleBack( Frame, e );
            Dispose();
        }

        private void Dispose()
        {
            NavigationHandler.OnNavigatedBack -= OnBackRequested;
            ThisBook = null;
            TOCData = null;
            VolList = null;
            ReviewsSection = null;

            try
            {
                // Try Dispose
                Worker.UIInvoke( () =>
                {
                    TOCSection.DataContext = null;
                    CommentSection.DataContext = null;
                    BookInfoSection.DataContext = null;
                } );
            }
            catch( Exception ) { }
        }

        private void ReorderModules()
        {
            LayoutSettings = new global::wenku8.Settings.Layout.BookInfoView();

            TOCBg.DataContext = LayoutSettings.GetBgContext( "TOC" );
            ViewOrder = LayoutSettings.GetViewOrders();

            LayoutRoot.FlowDirection = LayoutSettings.IsRightToLeft
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight
                ;

            IEnumerable<UIElement> Modules = MasterContainer.Children.OrderBy( ( x ) => ViewOrder.IndexOf( ( x as Border ).Name ) );

            foreach( UIElement e in Modules )
            {
                Border Mod = e as Border;
                Logger.Log( ID, "Placing in Order: " + Mod.Name, LogType.DEBUG );

                MasterContainer.Children.Remove( e );
                if( ViewOrder.IndexOf( Mod.Name ) != -1 )
                {
                    MasterContainer.Children.Add( e );
                }
            }

            if( ViewOrder.Count() == 0 )
            {
                StringResources stx = new StringResources();
                TextBlock ButThereIsNothing = new TextBlock();
                ButThereIsNothing.Text = stx.Text( "But_There_Is_Nothing" );
                ButThereIsNothing.TextWrapping = TextWrapping.Wrap;
                ButThereIsNothing.Foreground = new SolidColorBrush(
                    global::wenku8.Config.Properties.APPEARENCE_THEME_MAJOR_COLOR
                );
                ButThereIsNothing.TextAlignment = TextAlignment.Center;

                ProgressRing Pring = new ProgressRing();
                Pring.Height = Pring.Width = 40;
                Pring.IsActive = true;

                MasterContainer.Orientation = Orientation.Vertical;
                MasterContainer.HorizontalAlignment = HorizontalAlignment.Center;
                MasterContainer.VerticalAlignment = VerticalAlignment.Center;
                MasterContainer.Children.Add( Pring );
                MasterContainer.Children.Add( ButThereIsNothing );

                Logger.Log( ID, "Everything is disabled, this section will be skipped", LogType.INFO );
                SkipThisPage = true;
            }
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );

            Instance = this;

            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );
            NavigationHandler.InsertHandlerOnNavigatedBack( OnBackRequested );

            if( e.NavigationMode == NavigationMode.New )
            {
                TOCSection.DataContext = null;
                TOCFloatSection.DataContext = null;
                CommentSection.DataContext = null;
                BookInfoSection.DataContext = null;
                OpenType( e.Parameter );
            }

            LayoutSettings.GetBgContext( "TOC" ).ApplyBackgrounds();
            LayoutSettings.GetBgContext( "INFO_VIEW" ).ApplyBackgrounds();
            LayoutSettings.GetBgContext( "COMMENTS" ).ApplyBackgrounds();

            if( SkipThisPage && e.NavigationMode == NavigationMode.Back )
            {
                MasterContainer.Children.Clear();
                GauBack();
                return;
            }

        }

        private void OpenType( object parameter )
        {
            useCache = true;
            if ( parameter is string )
            {
                useCache = false;
                LoadBookInfo( parameter.ToString() );
            }
            else if ( parameter is LocalTextDocument )
            {
                LoadBookInfo( parameter as LocalTextDocument );
            }
            else if ( parameter is BookItem )
            {
                ThisBook = ( BookItem ) parameter;
                LoadBookInfo( ThisBook as BookItem );
            }
        }

        private void LoadBookInfo( LocalTextDocument Doc )
        {
            IEnumerable<UIElement> Modules = MasterContainer.Children;

            // Remove Everything and only give toc
            foreach( UIElement e in Modules.ToArray() )
            {
                Border Mod = e as Border;

                MasterContainer.Children.Remove( e );
                if( Mod.Name == "TOCSection" )
                {
                    MasterContainer.Children.Add( e );
                }
            }

            VolumeLoaded(
                ThisBook = X.Instance<BookItem>( XProto.BookItemEx, Doc )
            );
        }

        private void LoadBookInfo( BookItem Book )
        {
            BookLoader BL = new BookLoader( ( NaN ) =>
            {
                if( SkipThisPage )
                {
                    new VolumeLoader( GoToContentReader ).Load( ThisBook );
                    return;
                }

                UpdateBookInfoSection( Book );

                if( ViewOrder.IndexOf( "TOCSection" ) != -1 )
                {
                    new VolumeLoader( VolumeLoaded ).Load( ThisBook );
                }
            } );

            BL.Load( ThisBook, useCache );
        }

        private void LoadBookInfo( string id )
        {
            string[] Argv = id.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );

            if ( Argv.Length == 2 )
            {
                string Mode = Argv[ 0 ];
                id = Argv[ 1 ];

                if ( Mode == "Spider" )
                {
                    // XXX: TODO
                    return;
                }
                else if ( Mode == "Local" )
                {
                    // XXX: TODO
                    return;
                }

                // Commencing the Level2 initializations
                new wenku8.System.Bootstrap().Level2();
            }

            BookItem BookEx = X.Instance<BookItem>( XProto.BookItemEx, id );
            BookEx.XSetProp(
                "Mode"
                , X.Const<string>( XProto.WProtocols, "ACTION_BOOK_META" ) );

            ThisBook = BookEx;

            if( SkipThisPage )
            {
                new VolumeLoader( GoToContentReader ).Load( ThisBook );
                return;
            }

            if( ViewOrder.IndexOf( "BookInfoSection" ) != -1 )
            {
                BookLoader BL = new BookLoader( UpdateBookInfoSection );
                BL.Load( ThisBook, true );
                BL.LoadIntro( ThisBook, true );
            }

            if( ViewOrder.IndexOf( "TOCSection" ) != -1 )
            {
                new VolumeLoader( VolumeLoaded ).Load( ThisBook );
            }

            if( ViewOrder.IndexOf( "CommentSection" ) != -1 )
            {
                InitCommentSection( ThisBook );
            }
        }

        private async Task OneDriveRsync()
        {
            if ( SyncStarted || ThisBook == null ) return;
            SyncStarted = true;
            if( OneDriveSync.Instance == null )
            {
                OneDriveSync.Instance = new OneDriveSync();
            }

            await OneDriveSync.Instance.Authenticate();

            if ( OneDriveSync.Instance.Authenticated )
            {
                AnchorStorage ANC = new AnchorStorage( ThisBook );
                await ANC.SyncSettings();
            }

            SyncStarted = false;
        }

        private async void GoToContentReader( BookItem b )
        {
            TOCData = new TOCSection( b );
            if ( TOCData.AnchorAvailable )
            {
                await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal
                    , () => Frame.Navigate( typeof( ContentReader ), TOCData.AutoAnchor )
                );
                return;
            }

            EpisodeStepper ES = new EpisodeStepper( new VolumesInfo( b ) );
            ES.stepNext();

            await OneDriveRsync();
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal
                , () => Frame.Navigate(
                    typeof( ContentReader )
                    , new Chapter( ES.currentEpTitle, b.Id, ES.currentVid, ES.currentCid )
                )
            ); 
        }

        private async void GauBack()
        {
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal
                , () => { NavigationHandler.MasterNavigationHandler( this, null ); }
            );
        }

        private void UpdateBookInfoSection( BookItem b )
        {
            BookInfoSection.DataContext = b;
        }

        #region TOC Section
        private void VolumeLoaded( BookItem b )
        {
            TOCData = new TOCSection( b );
            TOCData.TemplateSelector.IsHorizontal = LayoutSettings.HorizontalTOC;

            TOCSection.DataContext = TOCData;
            TOCFloatSection.DataContext = TOCData;

            TOCData.SetViewSource( VolumesViewSource );

            if( VolList != null && 0 < VolList.Items.Count() )
            {
                VolList.SelectedIndex = 0;
            }
        }

        private void VolumeChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count() < 1 ) return;

            /** This handles the flashing of the floated list
              * when scrolling is near to the end
              */
            if ( ChFloatList != null )
            {
                ChFloatList.Visibility = Visibility.Collapsed;
            }

            var j = ( sender as ListView ).Dispatcher.RunIdleAsync(
            ( x ) =>
            {
                Refresh = true;
                ChLayoutUpdate = false;
                Volume V = e.AddedItems[ 0 ] as Volume;
                TOCData.SelectVolume( V );
            } );
        }

        private void SyncButtonLoaded( object sender, RoutedEventArgs e )
        {
            OneDriveButton OButton = ( sender as OneDriveButton );

            OButton.SetSync( OneDriveRsync );
        }
        private void SyncIndLoaded( object sender, RoutedEventArgs e )
        {
            InSync = sender as ProgressRing;
            InSync.IsActive = SyncStarted;
        }

        private void VolumeListLoaded( object sender, RoutedEventArgs e )
        {
            VolList = sender as ListView;
            if ( TOCData == null ) return;
            // Auto select the first one
            VolList.SelectedItem = TOCData.Volumes[ 0 ];
        }

        private void ChapterSelected( object sender, ItemClickEventArgs e )
        {
            Chapter C = e.ClickedItem as Chapter;
            BackMask.HandleForward(
                Frame, () => Frame.Navigate( typeof( ContentReader ), C )
            );
        }

        private Volume RightClickedVolume;
        private void TOCShowVolumeAction( object sender, RightTappedRoutedEventArgs e )
        {
            FrameworkElement Elem = sender as FrameworkElement;
            FlyoutBase.ShowAttachedFlyout( Elem );
            RightClickedVolume = Elem.DataContext as Volume;
            if ( RightClickedVolume == null )
            {
                RightClickedVolume = ( Elem.DataContext as TOCSection.ChapterGroup ).Vol;
            }
        }
        private async void DownloadVolume( object sender, TappedRoutedEventArgs e )
        {
            StringResources stx = new StringResources( "ContextMenu" );
            StringResources stm = new StringResources( "Message" );

            MessageDialog Msg = new MessageDialog( RightClickedVolume.VolumeTitle, stx.Text( "ContextMenu_AutoUpdate" ) );

            bool Confirmed = false;
            Msg.Commands.Add(
                new UICommand( stm.Str( "Yes" ), ( x ) => Confirmed = true )
            );
            Msg.Commands.Add( new UICommand( stm.Str( "No" ) ) );

            await Popups.ShowDialog( Msg );

            if ( !Confirmed ) return;

            AutoCache.DownloadVolume( ThisBook, RightClickedVolume );
        }

        private async void JumpToBookmark( object sender, RoutedEventArgs e )
        {
            if( TOCData.AnchorAvailable )
            {
                BackMask.HandleForward(
                    Frame, () => Frame.Navigate( typeof( ContentReader ), TOCData.AutoAnchor )
                );
            }
            else
            {
                await Popups.ShowDialog(
                    new MessageDialog( "BOOKMARK_NOT_SET_YET" )
                );
            }
        }

        // Floating Mechanism
        private ListView ChFixedList;
        private Rectangle ChLeftBoundary;
        private Rectangle ChRightBoundary;
        private bool Floated = false;
        private bool ChLayoutUpdate = false;
        private bool Refresh = true;

        private void ChapterListLoaded( object sender, RoutedEventArgs e ) { ChFixedList = sender as ListView; }
        private void ChLeftBoundaryLoaded( object sender, RoutedEventArgs e ) { ChLeftBoundary = ( Rectangle ) sender; }
        private void ChRightBoundaryLoaded( object sender, RoutedEventArgs e ) { ChRightBoundary = ( Rectangle ) sender; }
        private void ContentScroll_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e ) { FloatList(); }

        private void ChFixedListLayoutUpdate( object sender, object e )
        {
            if( !ChLayoutUpdate )
            {
                ChLayoutUpdate = true;
                FloatList();
            }
        }

        private void FloatList()
        {
            if ( ChLeftBoundary == null ) return;

            double SW = global::wenku8.Resources.LayoutSettings.ScreenWidth;
            double CW = ChFixedList.ActualWidth;
            // Keep the chapter up with the screen
            Point LB = ChLeftBoundary.TransformToVisual( this ).TransformPoint( new Point() );
            Point RB = ChRightBoundary.TransformToVisual( this ).TransformPoint( new Point() );

            if( TOCSection.ActualWidth < SW )
            {
                ChFixedList.Opacity = 1;
                ChFixedList.HorizontalAlignment = HorizontalAlignment.Left;
                ChFloatList.Visibility = Visibility.Collapsed;
                return;
            }

            if ( ContentScroll.FlowDirection == FlowDirection.RightToLeft )
            {
                if ( RB.X < Math.Max( SW - CW, 0 ) && SW < LB.X )
                    FloatChapterList();
                else
                    PlaceChapterList( RB.X < 0 );
            }
            else if ( ContentScroll.FlowDirection == FlowDirection.LeftToRight )
            {
                if ( LB.X < 0 && Math.Min( CW, SW ) < RB.X )
                    FloatChapterList();
                else
                    PlaceChapterList( SW < RB.X );
            }
            else
            {
                PlaceChapterList( true );
            }
        }

        private void FloatChapterList()
        {
            if ( !Refresh && Floated ) return;
            Refresh = false;
            Floated = true;

            Logger.Log( ID, "Floating the ChapterList", LogType.DEBUG );

            ChFixedList.Opacity = 0;
            ChFloatList.Visibility = Visibility.Visible;
        }

        private void PlaceChapterList( bool LeftAlign )
        {
            if ( !Refresh && !Floated ) return;
            Refresh = false;
            Floated = false;

            Logger.Log( ID, "Placing the ChapterList", LogType.DEBUG );

            ChFixedList.HorizontalAlignment = LeftAlign
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            ChFixedList.Opacity = 1;
            ChFloatList.Visibility = Visibility.Collapsed;
        }
        #endregion

        private async void InitCommentSection( wenku8.Model.Book.BookItem b )
        {
            ReviewsSection = new ReviewsSection( b );
            // Let's try the async method this time
            await ReviewsSection.Load();
            CommentSection.DataContext = ReviewsSection;
        }

        private async void OpenComment( object sender, ItemClickEventArgs e )
        {
            await ReviewsSection.OpenReview( e.ClickedItem as Review );
        }

        private void ControlClick( object sender, ItemClickEventArgs e )
        {
            ReviewsSection.ControlAction( e.ClickedItem as PaneNavButton );
        }

        private void AddOrRemoveFav( object sender, TappedRoutedEventArgs e )
        {
            BookItem B = ( ( sender as FrameworkElement ).DataContext ) as BookItem;
            BookStorage BS = new BookStorage();
            if ( B.IsFav )
            {
                BS.RemoveBook( B.Id );
                B.IsFav = false;
            }
            else
            {
                BS.SaveBook( B.Id, B.Title, B.RecentUpdateRaw, B.LatestSection );
                B.IsFav = true;
            }

        }

        private void SearchAuthor( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( Search ), ThisBook.AuthorRaw );
        }

        private void OpenInBrowser( object sender, RoutedEventArgs e )
        {
            var j = Windows.System.Launcher.LaunchUriAsync( new Uri( ThisBook.OriginalUrl ) );
        }

        private void Vote( object sender, RoutedEventArgs e )
        {
            if( ThisBook.XTest( XProto.BookItemEx ) )
            {
                Expression<Action> handler = () => BeginStory();
                ThisBook.XCall<object>( "Vote", handler.Compile() );
            }
        }

        private void BeginStory()
        {
            Storyboard SB = PushGrid.Resources[ "DataUpdate" ] as Storyboard;
            SB.Begin();
        }

        private void PushCountGridLoaded( object sender, RoutedEventArgs e )
        {
            PushGrid = sender as Grid;
        }

        private async void ChangeBackground( object sender, RoutedEventArgs e )
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            string[] Argv = item.Tag.ToString().Split( ',' );

            if ( Argv[ 0 ] == "Preset" )
            {
                bool No = true;

                StringResources stm = new StringResources( "Message" );
                StringResources stc = new StringResources( "ContextMenu" );

                MessageDialog MsgBox = new MessageDialog( stm.Str( "BInfoView_PresetBg_Mesg" ), stc.Text( "PresetBackground" ) );
                MsgBox.Commands.Add( new UICommand( stm.Str( "Yes" ), x => { No = false; } ) );
                MsgBox.Commands.Add( new UICommand( stm.Str( "No" ) ) );

                await Popups.ShowDialog( MsgBox );

                if ( No ) return;

            }

            LayoutSettings.GetBgContext( Argv[ 1 ] ).SetBackground( Argv[ 0 ] );
        }

        private void InfoBgLoaded( object sender, RoutedEventArgs e )
        {
            InfoBgGrid = sender as Grid;
            InfoBgGrid.DataContext = LayoutSettings.GetBgContext( "INFO_VIEW" );
        }

        private void CommentsBgLoaded( object sender, RoutedEventArgs e )
        {
            InfoBgGrid = sender as Grid;
            InfoBgGrid.DataContext = LayoutSettings.GetBgContext( "COMMENTS" );
        }
    }
}

