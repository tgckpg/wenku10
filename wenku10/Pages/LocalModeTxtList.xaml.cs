using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.UI;
using Net.Astropenguin.UI.Icons;

using libtaotu.Pages;

using wenku8.Config;
using wenku8.CompositeElement;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.ListItem;
using wenku8.Model.Section;
using wenku8.Section;
using wenku8.Settings;
using wenku8.Storage;
using StatusType = wenku8.Model.REST.SharersRequest.StatusType;

namespace wenku10.Pages
{
    public sealed partial class LocalModeTxtList : Page
    {
        private static readonly string ID = typeof( LocalModeTxtList ).Name;

        private LocalFileList FileListContext;
        private LocalBook SelectedBook;

        private SharersHub SHHub;

        public LocalModeTxtList()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private async void SetTemplate()
        {
            NavigationHandler.OnNavigatedBack += OnBackRequested;

            FileListContext = new LocalFileList();
            FileListView.DataContext = FileListContext;

            SHHub = new SharersHub();
            SharersHub.DataContext = SHHub;
            MessageBus.OnDelivery += MessageBus_OnDelivery;

            SHHub.Member.OnStatusChanged += SHMem_OnStatusChanged;

            SHHub.Search( "" );

            if( Properties.ENABLE_ONEDRIVE && OneDriveSync.Instance == null )
            {
                OneDriveSync.Instance = new OneDriveSync();
                await OneDriveSync.Instance.Authenticate();
            }
        }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            if( !Frame.CanGoBack )
            {
                e.Handled = true;
                PopupFrame.Content = null;
            }
        }

        private void ClosePopup( object sender, RoutedEventArgs e )
        {
            PopupFrame.Content = null;
        }

        private async void MessageBus_OnDelivery( Message Mesg )
        {
            switch ( Mesg.Content )
            {
                case AppKeys.SH_SCRIPT_DATA:
                    HubScriptItem HSI = ( HubScriptItem ) Mesg.Payload;

                    if ( await FileListContext.OpenSpider( HSI.ScriptFile ) )
                    {
                        ConfirmScriptParse( HSI );
                    }
                    else
                    {
                        ConfirmErrorReport( HSI );
                    }
                    break;
                case AppKeys.HS_DECRYPT_FAIL:
                    StringResources stx = new StringResources( "Message" );
                    MessageDialog MsgBox = new MessageDialog( stx.Str( "HubScriptDecryptFail" ) );

                    HSI = ( HubScriptItem ) Mesg.Payload;
                    bool PlaceRequest = false;

                    MsgBox.Commands.Add( new UICommand(
                        stx.Str( "PlaceKeyRequest" )
                        , ( x ) => { PlaceRequest = true; } ) );

                    MsgBox.Commands.Add( new UICommand( stx.Str( "OK" ) ) );

                    await Popups.ShowDialog( MsgBox );

                    if ( PlaceRequest ) SHHub.PlaceKeyRequest( HSI.Id );

                    break;
            }
        }

        private async void ConfirmScriptParse( HubScriptItem HSI )
        {
            StringResources stx = new StringResources( "Message" );
            MessageDialog MsgBox = new MessageDialog( stx.Str( "ConfirmScriptParse" ) );
            bool Parse = false;

            MsgBox.Commands.Add( new UICommand(
                stx.Str( "Yes" )
                , ( x ) => { Parse = true; } ) );

            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

            await Popups.ShowDialog( MsgBox );

            if ( Parse )
            {
                ProcessItem( FileListContext.GetById( HSI.Id ) );
                MainHub.RefSV.ChangeView( 0, 0, null, false );
                PopupFrame.Content = null;
            }
        }

        private async void ConfirmErrorReport( HubScriptItem HSI )
        {
            StringResources stx = new StringResources( "Message", "Error" );
            MessageDialog MsgBox = new MessageDialog(
                string.Format( stx.Str( "ReportError" ), stx.Str( "InvalidScript", "Error" ) )
            );

            bool Report = false;

            MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { Report = true; } ) );
            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

            await Popups.ShowDialog( MsgBox );

            if ( Report ) SHHub.ReportStatus( HSI.Id, StatusType.INVALID_SCRIPT );
        }

        private void LoadFiles( object sender, RoutedEventArgs e )
        {
            FileListContext.Load();
        }

        private async void LoadUrl( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "AdvDM" );

            DownloadBookContext UrlC = new DownloadBookContext();
            Dialogs.Rename UrlBox = new Dialogs.Rename( UrlC, stx.Text( "Download_Location" ) );
            UrlBox.Placeholder = "http://example.com/NN. XXXX.txt";

            await Popups.ShowDialog( UrlBox );

            if ( UrlBox.Canceled ) return;

            FileListContext.LoadUrl( UrlC );
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
        }

        private void FileList_ItemClick( object sender, ItemClickEventArgs e )
        {
            LocalBook Item = ( LocalBook ) e.ClickedItem;

            // Prevent double processing on the already processed item
            if ( !Item.ProcessSuccess ) ProcessItem( Item );

            if ( Item.ProcessSuccess )
            {
                if ( Item is SpiderBook )
                {
                    BackMask.HandleForward(
                        Frame, () => Frame.Navigate( typeof( BookInfoView ), ( Item as SpiderBook ).GetBook() )
                    );
                }
                else
                {
                    BackMask.HandleForward(
                        Frame, () => Frame.Navigate( typeof( BookInfoView ), new LocalTextDocument( Item.aid ) )
                    );
                }
            }
            else if ( !Item.Processing && Item.File != null )
            {
                Frame.Navigate( typeof( DirectTextViewer ), Item.File );
            }
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            FileListContext.SearchTerm = sender.Text.Trim();
        }

        private void ProcessAll( object sender, RoutedEventArgs e )
        {
            FileListContext.Terminate = !FileListContext.Terminate;
            FileListContext.ProcessAll();
        }

        private void FavMode( object sender, RoutedEventArgs e )
        {
            FileListContext.ToggleFavs();

            IconBase b = ( sender as Button ).ChildAt<IconBase>( 0, 0, 0 );
            if ( FileListContext.FavOnly )
            {
                b.Foreground = new SolidColorBrush( Properties.APPEARENCE_THEME_MAJOR_COLOR );
            }
            else
            {
                b.Foreground = new SolidColorBrush( Properties.APPEARENCE_THEME_RELATIVE_SHADES_COLOR );
            }
        }

        private async void GotoSettings( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Message", "Settings", "AppBar" );

            bool Go = false;
            MessageDialog Msg = new MessageDialog( stx.Text( "Preface", "Settings" ), stx.Text( "Settings", "AppBar" ) );

            Msg.Commands.Add( new UICommand( stx.Str( "Yes" ), x => Go = true ) );
            Msg.Commands.Add( new UICommand( stx.Str( "No" ) ) );

            await Popups.ShowDialog( Msg );

            if ( Go ) Frame.Navigate( typeof( Settings.MainSettings ) );
        }

        private void ShowBookAction( object sender, RightTappedRoutedEventArgs e )
        {
            LSBookItem G = ( LSBookItem ) sender;
            FlyoutBase.ShowAttachedFlyout( G );

            SelectedBook = ( LocalBook ) G.DataContext;
        }

        private void ToggleFav( object sender, RoutedEventArgs e )
        {
            SelectedBook.ToggleFav();
        }

        private void RemoveSource( object sender, RoutedEventArgs e )
        {
            SelectedBook.RemoveSource();
            FileListContext.CleanUp();
        }

        private void ViewRaw( object sender, RoutedEventArgs e )
        {
            if ( SelectedBook.File != null )
            {
                Frame.Navigate( typeof( DirectTextViewer ), SelectedBook.File );
            }
        }

        private void Reanalyze( object sender, RoutedEventArgs e )
        {
            ProcessItem( SelectedBook );
        }

        private async void ProcessItem( LocalBook LB )
        {
            await LB.Process();
            if( LB is SpiderBook )
            {
                BookInstruction BS = ( LB as SpiderBook ).GetBook();
                if( BS.Packable )
                {
                    BS.PackVolumes();
                }
            }
        }

        private void GotoGrabberPage( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( ProceduresPanel ) );
        }

        private void ImportSpider( object sender, RoutedEventArgs e )
        {
            FileListContext.OpenSpider();
        }

        private void ProcessButtonLoaded( object sender, RoutedEventArgs e )
        {
            ( ( Button ) sender ).DataContext = FileListContext;
        }

        private void LSSync( object sender, RoutedEventArgs e )
        {
            BookStorage BS = new BookStorage();
            ( ( OneDriveButton ) sender ).SetSync( BS.SyncSettings );
        }

        private void SearchBox_QuerySubmitted( AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args )
        {
            SharersHub.Focus( FocusState.Pointer );
            SHHub.Search( args.QueryText );
        }

        private void ShHub_ItemCLick( object sender, ItemClickEventArgs e )
        {
            PopupFrame.Content = new ShHub.ScriptDetails( e.ClickedItem as HubScriptItem );
        }

        private void ScriptUpload( object sender, RoutedEventArgs e )
        {
            PopupFrame.Content = new ShHub.ScriptUpload( UploadExit );
        }

        private void UploadExit( string Id, string AccessToken )
        {
            PopupFrame.Content = null;
            SHHub.Search( "uuid: " + Id, new string[] { AccessToken } );
        }

        private void LoginOrLogout( object sender, RoutedEventArgs e ) { LoginOrLogout(); }
        private void ManageAuths( object sender, RoutedEventArgs e ) { }

        private async void Register( object sender, RoutedEventArgs e )
        {
            Dialogs.Sharers.Register RegisterDialog = new Dialogs.Sharers.Register();
            await Popups.ShowDialog( RegisterDialog );
            if ( RegisterDialog.Canceled ) return;
            LoginOrLogout();
        }

        private async void LoginOrLogout()
        {
            if ( SHHub.Member.WillLogin ) return;
            if( SHHub.LoggedIn )
            {
                SHHub.Member.Logout();
            }
            else
            {
                Dialogs.Login LoginDialog = new Dialogs.Login( SHHub.Member );
                await Popups.ShowDialog( LoginDialog );
            }
        }

        private void SHMem_OnStatusChanged( object sender, MemberStatus Status )
        {
            if( Status == MemberStatus.RE_LOGIN_NEEDED )
            {
                LoginOrLogout();
            }
        }

        public class DownloadBookContext : INamable
        {
            private Uri _url;

            public Regex Re = new Regex( @"https?://.+/(\d+)\.([\w\W]+\.)?txt$" );

            public Uri Url { get { return _url; } }

            public string Id { get; private set; }
            public string Title { get; private set; }

            public string Name
            {
                get { return _url == null ? "" : _url.ToString(); }
                set
                {
                    Match m = Re.Match( value );
                    if ( !m.Success )
                    {
                        throw new Exception( "Invalid Url" );
                    }
                    else
                    {
                        Id = m.Groups[ 1 ].Value;
                        Title = m.Groups[ 2 ].Value;
                    }

                    _url = new Uri( value );
                }
            }
        }
    }
}