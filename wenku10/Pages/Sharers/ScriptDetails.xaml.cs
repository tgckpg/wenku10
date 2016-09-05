using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.UI.Icons;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.UI;

using wenku8.AdvDM;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Comments;
using wenku8.Model.ListItem;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.Loaders;
using wenku8.Model.REST;
using wenku8.Resources;
using wenku8.Settings;
using wenku8.ThemeIcons;

namespace wenku10.Pages.Sharers
{
    using Dialogs.Sharers;

    using AESManager = wenku8.System.AESManager;
    using TokenManager = wenku8.System.TokenManager;
    using CryptAES = wenku8.System.CryptAES;
    using CryptRSA = wenku8.System.CryptRSA;
    using SHTarget = SharersRequest.SHTarget;
    using System.Threading.Tasks;

    sealed partial class ScriptDetails : Page
    {
        public static readonly string ID = typeof( ScriptDetails ).Name;

        private Storyboard CommentStory;
        private Storyboard RequestStory;
        private ObservableCollection<PaneNavButton> BottomControls;
        private Observables<HSComment, HSComment> CommentsSource;
        private Observables<SHRequest, SHRequest> RequestsSource;

        private XRegistry XGrant = new XRegistry( "<xg />", wenku8.Settings.FileLinks.ROOT_SETTING + "XGrant.tmp" );
        private Dictionary<string, PaneNavButton> AvailControls;

        private IMember Member;

        private bool CommentsOpened = false;
        private volatile bool CommInit = false;

        private bool RequestsOpened = false;
        private volatile SHTarget ReqTarget;

        private HubScriptItem BindItem;
        private RuntimeCache RCache = new RuntimeCache();

        private SHTarget CCTarget = SHTarget.SCRIPT;
        private string AccessToken;
        private CryptAES Crypt;
        private string CCId;

        private string[] HomeControls = new string[] { "OpenRequest", "Comment", "Download" };
        private string[] CommentControls = new string[] { "NewComment", "HideComment" };

        private Frame ParentFrame;

        public ScriptDetails( HubScriptItem Item, Frame ParentFrame = null )
            :this()
        {
            BindItem = Item;
            this.ParentFrame = ParentFrame;
            SetTemplate();
        }

        public ScriptDetails( BookItem b, Frame ParentFrame )
            :this()
        {
            this.ParentFrame = ParentFrame;
            AccessToken = ( string ) new TokenManager().GetAuthById( b.Id )?.Value;
            SuggestUpload( b );
        }

        public async void SuggestUpload( BookItem b )
        {
            BindItem = await LoadFromSHHub( b.Id, AccessToken );
            if ( BindItem != null )
            {
                await Dispatcher.RunIdleAsync( ( x ) => SetTemplate() );
            }
            else
            {
                await Dispatcher.RunIdleAsync( x => ParentFrame.Content = new ScriptUpload( b, UploadReturn ) );
            }
        }

        private async Task<HubScriptItem> LoadFromSHHub( string Id, string Token )
        {
            SHSearchLoader SHLoader = new SHSearchLoader( "uuid: " + Id, new string[] { Token } );
            IEnumerable<HubScriptItem> HSIs = await SHLoader.NextPage( 1 );

            return HSIs.FirstOrDefault();
        }

        public ScriptDetails()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            BindItem = ( HubScriptItem ) e.Parameter;
            SetTemplate();
        }

        private void SetTemplate()
        {
            LayoutRoot.DataContext = BindItem;

            if( BindItem.Encrypted )
            {
                ReqTarget = SHTarget.KEY;
                Crypt = ( CryptAES ) new AESManager().GetAuthById( BindItem.Id );
            }

            BottomControls = new ObservableCollection<PaneNavButton>();
            AccessToken = ( string ) new TokenManager().GetAuthById( BindItem.Id )?.Value;
            XGrant.SetParameter( BindItem.Id, wenku8.Storage.BookStorage.TimeKey );

            if ( !string.IsNullOrEmpty( AccessToken ) )
            {
                TransitionDisplay.SetState( AccessControls, TransitionState.Active );
            }

            AvailControls = new Dictionary<string, PaneNavButton>()
            {
                { "Download", new PaneNavButton( new IconLogin() { AutoScale = true, Direction = Direction.Rotate270 }, Download ) }
                , { "Comment", new PaneNavButton( new IconComment() { AutoScale = true }, ToggleComments ) }
                , { "HideComment", new PaneNavButton( new IconNavigateArrow() { AutoScale = true, Direction = Direction.MirrorHorizontal }, ToggleComments ) }
                , { "NewComment", new PaneNavButton( new IconPlusSign() { AutoScale = true }, () => {
                    StringResources stx = new StringResources( "AppBar" );
                    CCTarget = SHTarget.SCRIPT;
                    CCId = BindItem.Id;
                    NewComment( stx.Str( "AddComment" ) );
                } ) }
                , { "OpenRequest", new PaneNavButton( new IconKeyRequest() { AutoScale = true }, ToggleRequests ) }
                , { "KeyRequest", new PaneNavButton( new IconRawDocument() { AutoScale = true }, () => { ShowRequest( SHTarget.KEY ); } ) }
                , { "TokenRequest", new PaneNavButton( new IconMasterKey() { AutoScale = true }, () => { ShowRequest( SHTarget.TOKEN ); } ) }
                , { "CloseRequest", new PaneNavButton( new IconNavigateArrow() { AutoScale = true, Direction = Direction.MirrorHorizontal }, ToggleRequests ) }
                , { "Submit", new PaneNavButton( new IconTick() { AutoScale = true }, SubmitComment ) }
                , { "Discard", new PaneNavButton( new IconCross() { AutoScale = true }, DiscardComment ) }
            };

            DisplayControls( HomeControls );

            ControlsList.ItemsSource = BottomControls;

            CommentStory = new Storyboard();
            CommentStory.Completed += CommentStory_Completed;

            RequestStory = new Storyboard();
            RequestStory.Completed += RequestStory_Completed;

            Member = X.Singleton<IMember>( XProto.SHMember );
            Unloaded += ScriptDetails_Unloaded;
        }

        private void ScriptDetails_Unloaded( object sender, RoutedEventArgs e )
        {
            DataContext = null;
            CommentsSource = null;
            HSComment.ActiveInstance = null;
        }

        private async void UploadReturn( string Id, string Token )
        {
            AccessToken = Token;

            HubScriptItem HSI = await LoadFromSHHub( Id, Token );

            // We are in BookInfoView
            if ( !( ParentFrame == null || HSI == null ) )
            {
                ParentFrame.Content = new ScriptDetails( HSI, ParentFrame );
            }
            // We are in LocalModeTextList
            else if ( HSI != null )
            {
                // Since we cannot set the Content property here anymore
                // We call for help
                MessageBus.SendUI( GetType(), AppKeys.HS_DETAIL_VIEW, HSI );
            }
        }

        private void ControlClick( object sender, ItemClickEventArgs e )
        {
            ( ( PaneNavButton ) e.ClickedItem ).Action();
        }

        private void DisplayControls( params string[] Controls )
        {
            BottomControls.Clear();
            foreach ( string Cont in Controls )
            {
                BottomControls.Add( AvailControls[ Cont ] );
            }
        }

        #region Priviledged Controls
        private void Update( object sender, RoutedEventArgs e )
        {
            Content = new ScriptUpload( BindItem, UploadReturn );
        }

        private async void Delete( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Message" );
            MessageDialog MsgBox = new MessageDialog( stx.Str( "ConfirmScriptRemove" ) );

            bool DoDelete = false;

            MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { DoDelete = true; } ) );
            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );
            await Popups.ShowDialog( MsgBox );

            if ( DoDelete )
            {
                // Since we cannot close the Frame from here
                // We call for help
                MessageBus.SendUI( GetType(), AppKeys.SH_SCRIPT_REMOVE, BindItem );
            }
        }

        private void TogglePublic( object sender, RoutedEventArgs e )
        {
            MarkLoading();
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.Publish( BindItem.Id, !BindItem.Public, AccessToken )
                , ( e2, QId ) =>
                {
                    try
                    {
                        JsonStatus.Parse( e2.ResponseString );
                        BindItem.Public = !BindItem.Public;
                    }
                    catch ( Exception ex )
                    {
                        BindItem.ErrorMessage = ex.Message;
                    }
                    MarkNotLoading();
                }
                , ( a, b, ex ) =>
                {
                    BindItem.ErrorMessage = ex.Message;
                    MarkNotLoading();
                }
                , false
            );
        }
        #endregion

        #region Download
        private void Download()
        {
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptDownload( BindItem.Id, AccessToken )
                , DownloadComplete
                , DownloadFailed
                , true
            );
        }

        private void DownloadFailed( string CacheName, string Id, Exception ex )
        {
            BindItem.ErrorMessage = ex.Message;
        }

        private void DownloadComplete( DRequestCompletedEventArgs e, string Id )
        {
            BindItem.SetScriptData( e.ResponseString );
        }
        #endregion

        #region Requests
        private void PlaceKeyRequest( object sender, RoutedEventArgs e ) { PlaceRequest( SHTarget.KEY, BindItem ); }
        private void PlaceTokenRequest( object sender, RoutedEventArgs e ) { PlaceRequest( SHTarget.TOKEN, BindItem ); }

        private async void AssignToken( object sender, RoutedEventArgs e )
        {
            AssignAuth RequestBox = new AssignAuth( new TokenManager(), "Assign Token" );
            await Popups.ShowDialog( RequestBox );

            if ( RequestBox.Canceled || RequestBox.SelectedAuth == null ) return;

            AccessToken = ( string ) RequestBox.SelectedAuth.Value;
            TransitionDisplay.SetState( AccessControls, TransitionState.Active );
        }

        private async void AssignKey( object sender, RoutedEventArgs e )
        {
            AssignAuth RequestBox = new AssignAuth( new AESManager(), "Assign Key" );
            await Popups.ShowDialog( RequestBox );

            if ( RequestBox.Canceled || RequestBox.SelectedAuth == null ) return;

            Crypt = ( CryptAES ) RequestBox.SelectedAuth;
        }

        public async void PlaceRequest( SHTarget Target, HubScriptItem HSI )
        {
            StringResources stx = new StringResources();

            PlaceRequest RequestBox = new PlaceRequest(
                Target, HSI
                , stx.Text( ( Target & SHTarget.KEY ) != 0 ? "KeyRequest" : "TokenRequest" )
            );

            await Popups.ShowDialog( RequestBox );
            if ( !RequestBox.Canceled ) OpenRequest( Target );
        }

        public void OpenRequest( SHTarget Target )
        {
            if ( RequestsOpened )
            {
                ShowRequest( Target );
            }
            else
            {
                ReqTarget = Target;
                ToggleRequests();
            }
        }

        private void ToggleRequests()
        {
            if ( RequestStory.GetCurrentState() != ClockState.Stopped ) return;
            RequestStory.Children.Clear();

            // Slide In / Slide Out
            if ( RequestsOpened )
            {
                DisplayControls( HomeControls );

                SimpleStory.DoubleAnimation(
                    RequestStory
                    , RequestSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0, 0.25 * LayoutSettings.ScreenHeight
                );

                SimpleStory.DoubleAnimation( RequestStory, RequestSection, "Opacity", 1, 0 );
            }
            else
            {
                DisplayControls(
                    BindItem.Encrypted
                    ? new string[] { "KeyRequest", "TokenRequest", "CloseRequest" }
                    : new string[] { "TokenRequest", "CloseRequest" }
                );

                SimpleStory.DoubleAnimation(
                    RequestStory
                    , RequestSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0.25 * LayoutSettings.ScreenHeight, 0
                );

                SimpleStory.DoubleAnimation( RequestStory, RequestSection, "Opacity", 0, 1 );

                RequestSection.Visibility = Visibility.Visible;
                ShowRequest( ReqTarget );
            }

            RequestStory.Begin();
        }

        private void RequestStory_Completed( object sender, object e )
        {
            RequestStory.Stop();
            if ( RequestsOpened )
            {
                RequestSection.Visibility = Visibility.Collapsed;
                RequestSection.Opacity = 0;
                RequestsOpened = false;
            }
            else
            {
                RequestSection.Opacity = 1;
                RequestsOpened = true;
            }
        }

        private void ShowRequest( SHTarget Target )
        {
            // User have the thing. So he / she can grant requests for this script
            if ( ( Target & SHTarget.KEY ) != 0 )
            {
                ControlsList.SelectedIndex = 0;
                RequestList.Tag = BindItem.Encrypted && Crypt != null;
            }
            else
            {
                ControlsList.SelectedIndex = Crypt != null ? 1 : 0;
                RequestList.Tag = AccessToken;
            }

            ReloadRequests( Target );
        }

        private void GrantRequest( object sender, RoutedEventArgs e )
        {
            SHRequest Req = ( ( Button ) sender ).DataContext as SHRequest;
            if ( Req == null ) return;

            try
            {
                CryptRSA RSA = new CryptRSA( Req.Pubkey );
                string GrantData = null;

                switch ( ReqTarget )
                {
                    case SHTarget.TOKEN:
                        if ( !string.IsNullOrEmpty( AccessToken ) )
                        {
                            GrantData = RSA.Encrypt( AccessToken );
                        }
                        break;
                    case SHTarget.KEY:
                        if ( Crypt != null )
                        {
                            GrantData = RSA.Encrypt( Crypt.KeyBuffer );
                        }
                        break;
                }

                if ( !string.IsNullOrEmpty( GrantData ) )
                {
                    RCache.POST(
                        Shared.ShRequest.Server
                        , Shared.ShRequest.GrantRequest( Req.Id, GrantData )
                        , GrantComplete
                        , GrantFailed
                        , false
                    );
                }
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message );
            }
        }

        private void GrantFailed( string CacheName, string Id, Exception ex )
        {
            System.Diagnostics.Debugger.Break();
        }

        private void GrantComplete( DRequestCompletedEventArgs e, string Id )
        {
            try
            {
                JsonStatus.Parse( e.ResponseString );
                SetGranted( Id );
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message );
            }
        }

        private void SetGranted( string Id )
        {
            RequestsSource.Any( x =>
            {
                if ( x.Id == Id )
                {
                    x.Granted = true;
                    return true;
                }
                return false;
            } );

            XParameter XParam = XGrant.Parameter( BindItem.Id );
            XParam.SetParameter( new XParameter( Id ) );
            XGrant.SetParameter( XParam );
            XGrant.Save();
        }

        private async void ReloadRequests( SHTarget Target )
        {
            if ( LoadingRing.IsActive ) return;

            ReqTarget = Target;
            MarkLoading();
            HSLoader<SHRequest> CLoader = new HSLoader<SHRequest>(
                BindItem.Id
                , Target
                , ( _Target, _Skip, _Limit, _Ids ) => Shared.ShRequest.GetRequests( _Target, _Ids[0], _Skip, _Limit )
            );
            CLoader.ConvertResult = xs =>
            {
                XParameter XParam = XGrant.Parameter( BindItem.Id );
                if ( XParam != null )
                {
                    foreach ( SHRequest x in xs )
                    {
                        x.Granted = XParam.FindParameter( x.Id ) != null;
                    }
                }
                return xs.ToArray();
            };

            IList<SHRequest> FirstPage = await CLoader.NextPage();
            MarkNotLoading();

            RequestsSource = new Observables<SHRequest, SHRequest>( FirstPage );
            RequestsSource.ConnectLoader( CLoader );

            RequestsSource.LoadStart += ( x, y ) => MarkLoading();
            RequestsSource.LoadEnd += ( x, y ) => MarkNotLoading();
            RequestList.ItemsSource = RequestsSource;
        }
        #endregion

        #region Comments
        public async void OpenCommentStack( string CommId )
        {
            CommInit = true;

            DisplayControls( CommentControls );

            SimpleStory.DoubleAnimation(
                CommentStory, CommentSection
                , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                , 0.25 * LayoutSettings.ScreenHeight, 0
            );

            SimpleStory.DoubleAnimation( CommentStory, CommentSection, "Opacity", 0, 1 );

            CommentSection.Visibility = Visibility.Visible;

            bool LoadFirst = true;
            Func<SHTarget, int, uint, string[], PostData> StackRequest = ( a, b, c, Ids ) =>
            {
                if ( LoadFirst )
                {
                    LoadFirst = false;
                    return Shared.ShRequest.GetCommentStack( Ids[ 0 ] );
                }

                return Shared.ShRequest.GetComments( a, b, c, Ids );
            };

            HSLoader<HSComment> CLoader = new HSLoader<HSComment>( CommId, SHTarget.COMMENT, StackRequest )
            {
                ConvertResult = ( x ) => x.Flattern( y => y.Replies )
            };

            IEnumerable<HSComment> FirstPage = await CLoader.NextPage();

            CommentsSource = new Observables<HSComment, HSComment>( FirstPage.Flattern( x => x.Replies ) );

            CommentsSource.LoadStart += ( x, y ) => MarkLoading();
            CommentsSource.LoadEnd += ( x, y ) => MarkNotLoading();

            CommentsSource.ConnectLoader( CLoader );
            CommentList.ItemsSource = CommentsSource;

            CommentStory.Begin();
        }

        private void ToggleComments()
        {
            if ( CommentStory.GetCurrentState() != ClockState.Stopped ) return;
            CommentStory.Children.Clear();

            // Slide In / Slide Out
            if ( CommentsOpened )
            {
                DisplayControls( HomeControls );

                SimpleStory.DoubleAnimation(
                    CommentStory, CommentSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0, 0.25 * LayoutSettings.ScreenHeight
                );

                SimpleStory.DoubleAnimation( CommentStory, CommentSection, "Opacity", 1, 0 );
            }
            else
            {
                DisplayControls( CommentControls );

                SimpleStory.DoubleAnimation(
                    CommentStory, CommentSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0.25 * LayoutSettings.ScreenHeight, 0
                );

                SimpleStory.DoubleAnimation( CommentStory, CommentSection, "Opacity", 0, 1 );

                CommentSection.Visibility = Visibility.Visible;

                if ( !CommInit )
                {
                    CommInit = true;
                    ReloadComments();
                }
            }

            CommentStory.Begin();
        }

        private void CommentStory_Completed( object sender, object e )
        {
            CommentStory.Stop();
            if ( !CommentsOpened )
            {
                CommentSection.Opacity = 1;
                CommentsOpened = true;
            }
            else if( CommentsOpened )
            {
                CommentSection.Visibility = Visibility.Collapsed;
                CommentSection.Opacity = 0;
                CommentsOpened = false;
            }
        }

        private async void ReloadComments()
        {
            if ( LoadingRing.IsActive ) return;

            MarkLoading();
            HSLoader<HSComment> CLoader = new HSLoader<HSComment>( BindItem.Id, SHTarget.SCRIPT, Shared.ShRequest.GetComments )
            {
                ConvertResult = ( x ) => x.Flattern( y => y.Replies )
            };

            IList<HSComment> FirstPage = await CLoader.NextPage();
            MarkNotLoading();

            if ( BindItem.Encrypted )
            {
                if ( Crypt == null )
                {
                    CommentsSource = new Observables<HSComment, HSComment>( CrippledComments( FirstPage ) );
                    CommentsSource.ConnectLoader( CLoader, CrippledComments );
                }
                else
                {
                    CommentsSource = new Observables<HSComment, HSComment>( DecryptComments( FirstPage ) );
                    CommentsSource.ConnectLoader( CLoader, DecryptComments );
                }
            }
            else
            {
                CommentsSource = new Observables<HSComment, HSComment>( FirstPage );
                CommentsSource.ConnectLoader( CLoader );
            }

            CommentsSource.LoadStart += ( x, y ) => MarkLoading();
            CommentsSource.LoadEnd += ( x, y ) => MarkNotLoading();
            CommentList.ItemsSource = CommentsSource;
        }

        private IList<HSComment> DecryptComments( IList<HSComment> Comments )
        {
            foreach( HSComment HSC in Comments )
            {
                try
                {
                    HSC.Title = Crypt.Decrypt( HSC.Title );
                }
                catch ( Exception )
                {
                    HSC.DecFailed = true;
                    HSC.Title = CryptAES.RawBytes( HSC.Title );
                }
            }

            return Comments;
        }

        private IList<HSComment> CrippledComments( IList<HSComment> Comments )
        {
            foreach( HSComment HSC in Comments )
            {
                HSC.DecFailed = true;
                HSC.Title = CryptAES.RawBytes( HSC.Title );
            }

            return Comments;
        }

        private void CommentList_ItemClick( object sender, ItemClickEventArgs e )
        {
            HSComment HSC = ( HSComment ) e.ClickedItem;

            if ( HSC.Folded )
            {
                if ( HSC == HSComment.ActiveInstance ) return;
                int i = CommentsSource.IndexOf( HSC );

                // The Load more always appeared in the next level
                // i.e. previous item is always a parent
                HSComment ParentHSC = CommentsSource[ i - 1 ];

                HSLoader<HSComment> CLoader = new HSLoader<HSComment>( ParentHSC.Id, SHTarget.COMMENT, Shared.ShRequest.GetComments )
                {
                    ConvertResult = ( x ) =>
                        x.Flattern( y =>
                        {
                            y.Level += HSC.Level;
                            return y.Replies;
                        } )
                };

                CommentsSource.LoadStart += ( x, y ) => MarkLoading();
                CommentsSource.LoadEnd += ( x, y ) => MarkNotLoading();


                // Remove the LoadMore thing
                CommentsSource.RemoveAt( i );
                CommentsSource.InsertLoader( i, CLoader );
            }

            HSC.MarkSelect();
        }

        private void NewReply( object sender, RoutedEventArgs e )
        {
            HSComment HSC = ( HSComment ) ( ( FrameworkElement ) sender ).DataContext;
            StringResources stx = new StringResources( "AppBar" );

            CCTarget = SHTarget.COMMENT;
            CCId = HSC.Id;
            NewComment( stx.Text( "Reply" ) );
        }

        private void NewComment( string Label )
        {
            TransitionDisplay.SetState( CommentEditor, TransitionState.Active );
            CommentModeLabel.Text = Label;

            if( BindItem.ForceEncryption && Crypt == null )
            {
                CommentInput.IsEnabled = false;
                StringResources stx = new StringResources();
                CommentError.Text = stx.Text( "CommentsEncrypted" );
                DisplayControls( "Discard" );
            }
            else
            {
                CommentInput.IsEnabled = true;
                DisplayControls( "Submit", "Discard" );
                CommentError.Text = "";
            }
        }

        private async void SubmitComment()
        {
            string Data;
            CommentInput.Document.GetText( Windows.UI.Text.TextGetOptions.None, out Data );
            Data = Data.Trim();

            if ( string.IsNullOrEmpty( Data ) )
            {
                CommentInput.Focus( FocusState.Keyboard );
                return;
            }

            if ( !Member.IsLoggedIn )
            {
                Dialogs.Login LoginBox = new Dialogs.Login( Member );
                await Popups.ShowDialog( LoginBox );
                if ( !Member.IsLoggedIn ) return;
            }

            if ( Crypt != null ) Data = Crypt.Encrypt( Data );

            new RuntimeCache() { EN_UI_Thead = true }.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.Comment( CCTarget, CCId, Data, Crypt != null )
                , CommentSuccess
                , CommentFailed 
                , false
            );
        }

        private void CommentFailed( string CacheName, string Id, Exception ex )
        {
            CommentError.Text = ex.Message;
        }

        private void CommentSuccess( DRequestCompletedEventArgs e, string Id )
        {
            try
            {
                JsonStatus.Parse( e.ResponseString );
                CommentInput.Document.SetText( Windows.UI.Text.TextSetOptions.None, "" );
                DiscardComment();
                ReloadComments();
            }
            catch( Exception ex )
            {
                CommentError.Text = ex.Message;
            }
        }

        private void DiscardComment()
        {
            DisplayControls( CommentControls );
            TransitionDisplay.SetState( CommentEditor, TransitionState.Inactive );
        }
        #endregion

        private void MarkLoading()
        {
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                LoadingRing.IsActive = true;
            } );
        }

        private void MarkNotLoading()
        {
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                LoadingRing.IsActive = false;
            } );
        }

    }
}
