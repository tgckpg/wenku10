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
using Net.Astropenguin.UI.Icons;

using libtaotu.Pages;

using wenku8.Config;
using wenku8.CompositeElement;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.Section;
using wenku8.Section;
using wenku8.Settings;
using wenku8.Storage;
using StatusType = wenku8.Model.REST.SharersRequest.StatusType;
using SHTarget = wenku8.Model.REST.SharersRequest.SHTarget;

namespace wenku10.Pages
{
    public sealed partial class LocalModeTxtList : Page
    {
        private static readonly string ID = typeof( LocalModeTxtList ).Name;

        private ZoneList ZoneListContext;
        private ZoneSpider SelectedZone;

        private LocalFileList FileListContext;
        private LocalBook SelectedBook;

        private Button ActivityButton;

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

            ZoneListContext = new ZoneList();
            ZoneListView.DataContext = ZoneListContext;

            SHHub = new SharersHub();
            SharersHub.DataContext = SHHub;
            MessageBus.OnDelivery += MessageBus_OnDelivery;

            SHHub.Member.OnStatusChanged += SHMem_OnStatusChanged;

            SHHub.Search( "" );

            if ( Properties.ENABLE_ONEDRIVE && OneDriveSync.Instance == null )
            {
                OneDriveSync.Instance = new OneDriveSync();
                await OneDriveSync.Instance.Authenticate();
            }
        }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            if ( !Frame.CanGoBack )
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

                    if ( ( HSI.Scope & SpiderScope.ZONE ) != 0 )
                    {
                        MainHub.ScrollToSection( ZoneListView );
                        PopupFrame.Content = null;
                        await ZoneListContext.OpenFile( HSI.ScriptFile );
                        break;
                    }

                    if ( await FileListContext.OpenSpider( HSI.ScriptFile ) )
                    {
                        ConfirmScriptParse( HSI );
                    }
                    else
                    {
                        ConfirmErrorReport( HSI.Id, StatusType.HS_INVALID );
                    }
                    break;

                case AppKeys.HS_DECRYPT_FAIL:
                    StringResources stx = new StringResources( "Message", "ContextMenu" );
                    MessageDialog MsgBox = new MessageDialog( stx.Str( "Desc_DecryptionFailed" ), stx.Str( "DecryptionFailed" ) );

                    HSI = ( HubScriptItem ) Mesg.Payload;
                    bool Place = false;

                    MsgBox.Commands.Add( new UICommand( stx.Text( "PlaceRequest", "ContextMenu" ), ( x ) => { Place = true; } ) );
                    MsgBox.Commands.Add( new UICommand( stx.Str( "OK" ) ) );

                    await Popups.ShowDialog( MsgBox );

                    if ( Place ) TransferRequest( SHTarget.KEY, HSI );
                    break;

                case AppKeys.HS_DETAIL_VIEW:
                    PopupFrame.Content = new Sharers.ScriptDetails( ( HubScriptItem ) Mesg.Payload );
                    break;

                case AppKeys.HS_OPEN_COMMENT:
                    InboxMessage BoxMessage = ( InboxMessage ) Mesg.Payload;
                    Sharers.ScriptDetails SSDetails = new Sharers.ScriptDetails( BoxMessage.HubScript );
                    PopupFrame.Content = SSDetails;
                    SSDetails.OpenCommentStack( BoxMessage.CommId );
                    break;

                case AppKeys.SH_SHOW_GRANTS:
                    Sharers.ManageAuth ManageAuth = new Sharers.ManageAuth( SHHub, PopupFrame );
                    PopupFrame.Content = ManageAuth;

                    ManageAuth.GotoRequests();
                    break;

                case AppKeys.SH_SCRIPT_REMOVE:
                    Tuple<string, HubScriptItem> RemoveInst = ( Tuple<string, HubScriptItem> ) Mesg.Payload;
                    if ( await SHHub.Remove( RemoveInst.Item2, RemoveInst.Item1 ) )
                    {
                        PopupFrame.Content = null;
                    }
                    break;

                case AppKeys.HS_NO_VOLDATA:
                    ConfirmErrorReport( ( ( BookInstruction ) Mesg.Payload ).Id, StatusType.HS_NO_VOLDATA );
                    break;

                case AppKeys.HS_MOVED:
                    Tuple<string, SpiderBook> Payload = ( Tuple<string, SpiderBook> ) Mesg.Payload;

                    LocalBook OBook = FileListContext.GetById( Payload.Item1 );
                    OBook?.RemoveSource();

                    FileListContext.Add( Payload.Item2 );
                    FileListContext.CleanUp();
                    break;
            }
        }

        private void TransferRequest( SHTarget Target, HubScriptItem HSI )
        {
            Sharers.ScriptDetails Details = PopupFrame.Content as Sharers.ScriptDetails;

            if ( Details == null )
            {
                Details = new Sharers.ScriptDetails( HSI );
                PopupFrame.Content = Details;
            }

            Details.PlaceRequest( Target, HSI );
        }

        private async void ConfirmScriptParse( HubScriptItem HSI )
        {
            StringResources stx = new StringResources( "Message" );
            MessageDialog MsgBox = new MessageDialog( stx.Str( "ConfirmScriptParse" ) );

            bool Parse = false;

            MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { Parse = true; } ) );
            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

            await Popups.ShowDialog( MsgBox );

            if ( Parse )
            {
                ProcessItem( FileListContext.GetById( HSI.Id ) );
                MainHub.RefSV.ChangeView( 0, 0, null, false );
                PopupFrame.Content = null;
            }
        }

        private async void ConfirmErrorReport( string Id, StatusType ErrorType )
        {
            StringResources stx = new StringResources( "Message", "Error" );
            MessageDialog MsgBox = new MessageDialog(
                string.Format( stx.Str( "ReportError" ), stx.Str( ErrorType.ToString(), "Error" ) )
            );

            bool Report = false;

            MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { Report = true; } ) );
            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

            await Popups.ShowDialog( MsgBox );

            if ( Report ) SHHub.ReportStatus( Id, ErrorType );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        #region Zone Spider
        private void OpenZone( object sender, RoutedEventArgs e ) { ZoneListContext.OpenFile(); }
        private void ExitZone( object sender, RoutedEventArgs e ) { ZoneListContext.ExitZone(); }
        private void EditZone( object sender, RoutedEventArgs e ) { EditItem( SelectedZone ); }
        private void ResetZoneState( object sender, RoutedEventArgs e ) { SelectedZone.Reset(); }
        private void ReloadZone( object sender, RoutedEventArgs e ) { SelectedZone.Reload(); }

        private void RemoveZone( object sender, RoutedEventArgs e )
        {
            ZoneListContext.RemoveZone( SelectedZone );
            SelectedZone = null;
        }

        private void ZoneList_ItemClick( object sender, ItemClickEventArgs e )
        {
            ZoneListContext.EnterZone( ( ZoneSpider ) e.ClickedItem );
        }

        private void ZoneSpider_ItemClick( object sender, ItemClickEventArgs e )
        {
            BackMask.HandleForward(
                Frame, async () =>
                {
                    BookInstruction BInst = ( BookInstruction ) e.ClickedItem;

                    // "Z" to let LocalFileList know this is a Zone directory
                    BInst.SetId( LocalFileList.ZONE_PFX + ZoneListContext.CurrentZone.ZoneId );
                    SpiderBook Book = await SpiderBook.CreateFromZoneInst( BInst );
                    if ( Book.CanProcess )
                    {
                        await Book.Process();
                        Frame.Navigate( typeof( BookInfoView ), Book.GetBook() );
                    }
                }
            );
        }

        private void ShowZoneAction( object sender, RightTappedRoutedEventArgs e )
        {
            Grid G = ( Grid ) sender;
            FlyoutBase.ShowAttachedFlyout( G );

            SelectedZone = ( ZoneSpider ) G.DataContext;
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
        #endregion

        #region Local Section

        #region Right Control Buttons
        private void ProcessAll( object sender, RoutedEventArgs e )
        {
            FileListContext.Terminate = !FileListContext.Terminate;
            FileListContext.ProcessAll();
        }

        private async void FavMode( object sender, RoutedEventArgs e )
        {
            await FileListContext.ToggleFavs();

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

        private void LoadFiles( object sender, RoutedEventArgs e )
        {
            FileListContext.OpenDirectory();
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
        #endregion

        private void FileList_ItemClick( object sender, ItemClickEventArgs e )
        {
            OpenBookInfoView( ( LocalBook ) e.ClickedItem );
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            FileListContext.SearchTerm = sender.Text.Trim();
        }

        #region Local item Right Click Context Menu
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
            try
            {
                SelectedBook.RemoveSource();
            }
            catch ( Exception ) { }

            FileListContext.CleanUp();
        }

        private async void CopySource( object sender, RoutedEventArgs e )
        {
            SpiderBook Book = SelectedBook as SpiderBook;
            if ( Book == null ) return;

            FileListContext.Add( await Book.Clone() );
        }

        private void ViewRaw( object sender, RoutedEventArgs e )
        {
            if ( SelectedBook.File != null )
            {
                Frame.Navigate( typeof( DirectTextViewer ), SelectedBook.File );
            }
        }

        private void EditSource( object sender, RoutedEventArgs e )
        {
            if ( SelectedBook is SpiderBook )
            {
                EditItem( ( SpiderBook ) SelectedBook );
            }
        }

        private async void Reanalyze( object sender, RoutedEventArgs e )
        {
            await SelectedBook.Reload();
            ProcessItem( SelectedBook );
        }
        #endregion

        private void EditItem( IMetaSpider LB )
        {
            Frame.Navigate( typeof( ProceduresPanel ), LB.MetaLocation );
        }
        #endregion

        #region Sharers Hub
        private void ToggleActivities( object sender, RoutedEventArgs e )
        {
            if ( !SHHub.LoggedIn )
            {
                LoginOrLogout();
                return;
            }

            ActivityButton = ( Button ) sender;
            if ( SHHub.Activities.Count() == 0 )
            {
                SHHub.GetMyRequests();
                SHHub.GetMyInbox();
            }
            else
            {
                if ( ActivityButton.Tag == null ) ActivityButton.Tag = true;
                else ActivityButton.Tag = null;
            }
        }

        private void Activities_ItemClick( object sender, ItemClickEventArgs e )
        {
            SHHub.CheckActivity( ( Activity ) e.ClickedItem );
            ActivityButton.Tag = null;
        }

        private void SearchBox_QuerySubmitted( AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args )
        {
            SharersHub.Focus( FocusState.Pointer );
            SHHub.Search( args.QueryText );
        }

        private void ShHub_ItemCLick( object sender, ItemClickEventArgs e )
        {
            HubScriptItem HSI = ( HubScriptItem ) e.ClickedItem;

            if ( HSI.Faultered )
            {
                // Report to admin
            }
            else
            {
                PopupFrame.Content = new Sharers.ScriptDetails( HSI );
            }
        }

        private void ScriptUpload( object sender, RoutedEventArgs e )
        {
            PopupFrame.Content = new Sharers.ScriptUpload( UploadExit );
        }

        private void UploadExit( string Id, string AccessToken )
        {
            PopupFrame.Content = null;
            SHHub.Search( "uuid: " + Id, new string[] { AccessToken } );
        }

        private void LoginOrLogout( object sender, RoutedEventArgs e ) { LoginOrLogout(); }

        private void EditInfo( object sender, RoutedEventArgs e )
        {
            PopupFrame.Content = new Sharers.UserInfo();
        }

        private void ManageAuths( object sender, RoutedEventArgs e )
        {
            PopupFrame.Content = new Sharers.ManageAuth( SHHub, PopupFrame );
        }

        private async void Register( object sender, RoutedEventArgs e )
        {
            await SHHub.Member.Register();
            LoginOrLogout();
        }

        private async void LoginOrLogout()
        {
            if ( SHHub.Member.WillLogin ) return;
            if ( SHHub.LoggedIn )
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
            if ( Status == MemberStatus.RE_LOGIN_NEEDED )
            {
                LoginOrLogout();
            }
        }
        #endregion

        private void OpenBookInfoView( LocalBook Item )
        {
            // Prevent double processing on the already processed item
            if ( !Item.ProcessSuccess && Item.CanProcess ) ProcessItem( Item );

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

        private async void ProcessItem( LocalBook LB )
        {
            await LB.Process();
            if ( LB is SpiderBook )
            {
                SpiderBook SB = ( SpiderBook ) LB;
                BookInstruction BS = SB.GetBook();
                if ( BS.Packable )
                {
                    BS.PackVolumes( SB.GetPPConvoy() );
                }
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