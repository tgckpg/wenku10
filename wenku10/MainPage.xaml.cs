using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.UI;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Section;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Storage;

namespace wenku10
{
    public sealed partial class MainPage : Page, IDisposable
    {
        public static readonly string ID = typeof( MainPage ).Name;

        private IFavSection FS;
        private Canvas Marquee;
        private bool Init = false;

        public Action ClosePoupFrame { get; private set; }

        ~MainPage()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                NavigationHandler.OnNavigatedBack -= OnBackRequested;
                AdvDMStat.DataContext = null;
                CustomSection.DataContext = null;
                NavigationSection.DataContext = null;
                SettingsButton.DataContext = null;
                FavSectionView.DataContext = null;
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
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
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            if ( e.NavigationMode == NavigationMode.Back ) Start();
            if ( e.NavigationMode != NavigationMode.New ) return;

            string Param = e.Parameter.ToString();
            if ( !string.IsNullOrEmpty( Param ) )
            {
                Logger.Log( ID, string.Format( "Param({0}) is present", Param ), LogType.INFO );
                GotoBookInfoView( e.Parameter.ToString() );
                return;
            }

            Start();
        }

        private void GotoBookInfoView( string id )
        {
            Logger.Log( ID, string.Format( "Navigate to BookInfoView, id: {0}", id ), LogType.INFO );
            StringResources stx = new StringResources( "LoadingMessage" );
            LoadingMask.Text = stx.Str( "ProgressIndicator_Message" );

            Action A = async () =>
            {
                TaskCompletionSource<BookItem> TCS = new TaskCompletionSource<BookItem>();
                new BookLoader( ( x ) => TCS.SetResult( x ) ).Load(
                    X.Instance<BookItem>( XProto.BookItemEx, id )
                    , true
                );

                BookItem b = await TCS.Task;

                if( b == null )
                {
                    LoadingMask.Text = "Failed to download data";
                    return;
                }

                LoadingMask.Text = b.Title;
                await Frame.Dispatcher.RunIdleAsync( x => Frame.Navigate( typeof( Pages.BookInfoView ), id ) );
            };
            LoadingMask.HandleForward( Frame, A );
        }

        public MainPage()
        {
            InitializeComponent();
            new global::wenku8.System.Bootstrap().Level2();

            X.Call<object>( XProto.Verification, "VersionCheck" );
            NavigationHandler.OnNavigatedBack += OnBackRequested;
            // Check for unittest
#if DEBUG
            new global::wenku8.UnitTest();
#endif
        }

        private void Start()
        {
            if ( Init )
            {
                FS?.Reload();
                return;
            }

            Init = true;

            if ( global::wenku8.Config.Properties.LOG_LEVEL == "DEBUG" )
            {
                AdvDMStat.Visibility = Visibility.Visible;
                AdvDMStat.DataContext = App.RuntimeTransfer;
            }

            INavSelections ns = X.Instance<INavSelections>( XProto.NavSelections );
            NavigationSection.DataContext = ns;

            if( ns.MainPage_Settings.IsStaffPicksEnabled )
            {
                RectileSection.Visibility = Visibility.Visible;
                ISectionItem sp = X.Instance<ISectionItem>( XProto.RectileSection );
                RectileSection.DataContext = sp;
                sp.Load( X.Call<XKey[]>( XProto.WRequest, "GetStaffPicks" ) );
            }

            if( ns.MainPage_Settings.IsCustomSectionEnabled )
            {
                CustomSection.Visibility = Visibility.Visible;
                CustomSection.DataContext = ns.CustomSection();
            }

            DebugLog.Visibility = global::wenku8.Config.Properties.ENABLE_SYSTEM_LOG ? Visibility.Visible : Visibility.Collapsed;

            ns.Load();

            SettingsButton.DataContext = new LoginStatus();

            WaitUIs();
        }

        private void WaitUIs()
        {
            // After Login Code
            FS = X.Instance<IFavSection>( XProto.FavSection );

            MemberSections();

            if( global::wenku8.Config.Properties.ENABLE_ONEDRIVE )
            {
                OneDriveResync.Visibility = Visibility.Visible;
            }

            OneDriveResync.SetSync( ReSync );

            FS.PropertyChanged += FS_PropertyChanged;
        }

        private void FS_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == "IsLoading" )
            {
                BgCover.State = FS.IsLoading ? ControlState.Foreatii : ControlState.Reovia;
            }
        }

        private async Task ReSync()
        {
            FS.IsLoading = true;
            // OneDriveSync
            OneDriveSync.Instance = new OneDriveSync();
            await OneDriveSync.Instance.Authenticate();

            if( OneDriveSync.Instance.Authenticated )
            {
                await new BookStorage().SyncSettings();
                FS.Reload();
            }

            FS.IsLoading = false;
        }

        private async void MemberSections()
        {
            bool LoggedIn = await TryLogin();

            FS.Load();
            FavSectionView.DataContext = FS;
        }

        private void ClosePaneButton( object sender, RoutedEventArgs e )
        {
            MainSplitPane.ClosePane();
        }

        private void BookClicked( object sender, ItemClickEventArgs e )
        {
            BookClicked( e.ClickedItem as BookInfoItem );
        }

        private async void BookClicked( object sender, StarCanvas.ItemClickedEventArgs e )
        {
            await Task.Delay( 500 );
            await Dispatcher.RunIdleAsync( ( x ) =>
            {
                BookClicked( e.ClickedItem as BookInfoItem );
            } );
        }

        private void BookClicked( BookInfoItem BookItem )
        {
            Logger.Log( ID, string.Format( "Clicked items is {0}, mode {1}", BookItem.Payload, BookItem.Mode ) );

            BookItem b = X.Instance<BookItem>( XProto.BookItemEx, BookItem.Payload );

            switch ( BookItem.Mode )
            {
                case SectionMode.InfoPane:
                    // Pane Loading = True
                    b.XSetProp( "Mode", X.Const<string>( XProto.WProtocols, "ACTION_BOOK_META" ) );

                    BookLoader loader = new BookLoader( UpdatePane );
                    loader.Load( b, true );
                    loader.LoadIntro( b, true );
                    RollOutInfoPane();
                    break;
                case SectionMode.DirectNavigation:
                    GotoBookInfoView( b.Id );
                    break;
            }
        }

        private void RollOutInfoPane()
        {
            PaneGridState.State = ControlState.Foreatii;
            PaneBackIcon.Visibility = Visibility.Collapsed;
            PaneLoadBubble.IsActive = true;

            StopMarquee();
            StartMarquee();
            MainSplitPane.OpenPane();
        }

        private async void StartMarquee()
        {
            if ( Marquee == null || MainSplitPane.State == PaneStates.Closed )
            {
                StopMarquee();
                return;
            }

            TextBlock T = Marquee.Children.First() as TextBlock;
            if ( T.ActualWidth < Marquee.ActualWidth && MarqueeStory != null )
            {
                StopMarquee();
                return;
            }


            await Task.Delay( 1000 );
            MakeMarquee();
        }

        private void StopMarquee()
        {
            if ( MarqueeStory == null ) return;
            MarqueeStory.Stop();
            MarqueeStory.Children.Clear();
        }

        private Storyboard MarqueeStory;
        private void MakeMarquee()
        {
            TextBlock T = Marquee.Children.First() as TextBlock;
            if ( MarqueeStory.GetCurrentState() != ClockState.Stopped ) return;

            MarqueeStory.Children.Clear();
            DoubleAnimationUsingKeyFrames d = new DoubleAnimationUsingKeyFrames();

            LinearDoubleKeyFrame still = new LinearDoubleKeyFrame();
            still.Value = 0;
            still.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromSeconds( 0 ) );

            LinearDoubleKeyFrame still_still = new LinearDoubleKeyFrame();
            still_still.Value = 0;
            still_still.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromSeconds( 2 ) );

            LinearDoubleKeyFrame move = new LinearDoubleKeyFrame();
            move.Value = -( T.ActualWidth + Marquee.ActualWidth );
            move.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromSeconds( 8 ) );

            d.Duration = new Duration( TimeSpan.FromSeconds( 8 ) );
            d.RepeatBehavior = new RepeatBehavior( 100 );

            d.KeyFrames.Add( still );
            d.KeyFrames.Add( still_still );
            d.KeyFrames.Add( move );

            Storyboard.SetTarget( d, T );
            Storyboard.SetTargetProperty( d, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)" );
            MarqueeStory.Children.Add( d );

            MarqueeStory.Begin();
        }

        private void Sb_Completed( object sender, object e )
        {
            StartMarquee();
        }

        private void UpdatePane( BookItem obj )
        {
            PaneLoadBubble.IsActive = false;
            PaneBackIcon.Visibility = Visibility.Visible;
            MainSplitPane.PaneGrid.DataContext = obj;
            PaneGridState.State = ControlState.Reovia;
        }

        private void LoginOrInfo( object sender, RoutedEventArgs e )
        {
            LoginStatus LS = ( sender as FrameworkElement ).DataContext as LoginStatus;
            PopupPage.DataContext = SettingsButton.DataContext;
            LS.PopupLoginOrInfo();
        }

        private void Navigate_Tapped( object sender, TappedRoutedEventArgs e )
        {
            object Tag = ( ( FrameworkElement ) sender ).Tag;

            if ( Tag != null )
            {
                GotoBookInfoView( Tag.ToString() );
            }
        }

        private async Task<bool> TryLogin()
        {
            if ( !X.Exists ) return false; 

            TaskCompletionSource<bool> IsLoggedIn = new TaskCompletionSource<bool>();

            IMember Member = X.Singleton<IMember>( XProto.Member );
            if ( Member.WillLogin )
            {
                TypedEventHandler<object, MemberStatus> A = null;
                A = ( s, e ) =>
                {
                    Member.OnStatusChanged -= A;

                    if ( e == MemberStatus.RE_LOGIN_NEEDED ) ReLogin();

                    IsLoggedIn.SetResult( Member.IsLoggedIn );
                };

                Member.OnStatusChanged += A;
            }
            else
            {
                IsLoggedIn.SetResult( Member.IsLoggedIn );
            }

            return await IsLoggedIn.Task;
        }

        private async void ReLogin()
        {
            await Task.Delay( 3000 );
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                ( ( LoginStatus ) SettingsButton.DataContext ).PopupLoginOrInfo();
            } );
        }

        private async void GotoSettings( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Settings" );
            StringResources stapp = new StringResources( "AppBar" );
            StringResources stm = new StringResources( "Message" );

            bool Go = false;
            Windows.UI.Popups.MessageDialog Msg = new Windows.UI.Popups.MessageDialog( stx.Text( "Preface" ), stapp.Text( "Settings" ) );

            Msg.Commands.Add(
                new Windows.UI.Popups.UICommand(
                    stm.Str( "Yes" )
                    , ( c ) => Go = true
                )
            );

            Msg.Commands.Add(
                new Windows.UI.Popups.UICommand( stm.Str( "No" ) )
            );

            await Popups.ShowDialog( Msg );

            if ( Go )
            {
                Dispose();
                Frame.Navigate( typeof( Pages.Settings.MainSettings ) );
            }
        }

        private void GotoSearch( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( Pages.Search ) );
        }

        private void ShowAbout( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( Pages.About ) );
        }

        private void ShowLog( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( Pages.DebugLog ) );
        }

        private void GotoNavigation( object sender, ItemClickEventArgs e )
        {
            SubtleUpdateItem s = e.ClickedItem as SubtleUpdateItem;
            if ( s.Nav == typeof( Pages.CategorizedList ) )
            {
                PopupPage.DataContext = new PopupList( s, Frame );
            }
            else
            {
                Frame.Navigate( s.Nav, s );
            }
        }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            if( !Frame.CanGoBack )
            {
                if( MainSplitPane.State == PaneStates.Opened )
                {
                    MainSplitPane.ClosePane();
                    e.Handled = true;
                    return;
                }

                BackAgainToExit( e );
            }
        }

        private void BackAgainToExit( XBackRequestedEventArgs e )
        {
            if( BackAgainMessage.State == ControlState.Reovia )
            {
                Windows.ApplicationModel.Core.CoreApplication.Exit();
                return;
            }

            e.Handled = true;
            ShowBackMessage();
        }
        private async void ShowBackMessage()
        {
            BackAgainMessage.State = ControlState.Reovia;
            await Task.Delay( 2000 );
            BackAgainMessage.State = ControlState.Foreatii;
        }

        private void ShowFavContext( object sender, RightTappedRoutedEventArgs e )
        {
            FrameworkElement Elem = sender as FrameworkElement;
            FlyoutBase.ShowAttachedFlyout( Elem );
            FS.CurrentItem = Elem.DataContext as FavItem;
        }

        private void FavContext( object sender, RoutedEventArgs e )
        {
            MenuFlyoutItem Item = sender as MenuFlyoutItem;
            switch( Item.Tag.ToString() )
            {
                case "Pin": FS.C_Pin(); break;
                case "RSync": FS.C_RSync(); break;
                case "AutoCache": FS.C_AutoCache(); break;
                case "Delete": FS.C_Delete(); break;
            }
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            FS.SearchTerm = sender.Text.Trim();
        }

        private void OrderFavItems( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count < 1 ) return;
            FS.Reorder( ( sender as ComboBox ).SelectedIndex );
        }

        private void BookTitleLoaded( object sender, RoutedEventArgs e )
        {
            Marquee = sender as Canvas;
            if ( MarqueeStory == null )
            {
                MarqueeStory = new Storyboard();
                MarqueeStory.Completed += Sb_Completed;
            }

        }

        private void ReloadCustomSection( object sender, RoutedEventArgs e )
        {
            Button b = sender as Button;

            INavSelections NS = NavigationSection.DataContext as INavSelections;
            IPaneInfoSection PS = NS.CustomSection();

            if ( PS.Data != null )
            {
                CustomSection.DataContext = PS;
            }
            else
            {
                PropertyChangedEventHandler PropChanged = null;
                PropChanged = ( s, ex ) =>
                {
                    if ( ex.PropertyName == "Data" )
                    {
                        PS.PropertyChanged -= PropChanged;
                        CustomSection.DataContext = PS;
                    }
                };

                PS.PropertyChanged += PropChanged;
            }

        }

        private void FloatyButton_Loaded( object sender, RoutedEventArgs e )
        {
            FloatyButton Floaty = ( ( FloatyButton ) sender );
            Floaty.BindTimer( NTimer.Instance );

            Floaty.TextSpeed = NTimer.RandDouble( -2, 2 );
        }
    }

}