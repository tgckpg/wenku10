using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using wenku8.AdvDM;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.CompositeElement;
using wenku8.Model.Comments;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.Pages;
using wenku8.Model.REST;
using wenku8.Resources;
using wenku8.Settings;

using AESManager = wenku8.System.AESManager;
using TokenManager = wenku8.System.TokenManager;
using CryptAES = wenku8.System.CryptAES;

namespace wenku10.Pages.Sharers
{
    using Dialogs.Sharers;
    using SHHub;
    using SHTarget = SharersRequest.SHTarget;

    sealed partial class ScriptDetails : Page, ICmdControls, IAnimaPage
    {
        public static readonly string ID = typeof( ScriptDetails ).Name;

        private HubScriptItem BindItem;
        private RuntimeCache RCache = new RuntimeCache();

        private string AccessToken;
        private CryptAES Crypt;

        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get ; private set; }

        AppBarButton DownloadBtn;
        AppBarButton KeyReqBtn;

        SHMember Member;

        public ScriptDetails()
        {
            this.InitializeComponent();
        }

        public ScriptDetails( HubScriptItem Item )
            :this()
        {
            BindItem = Item;
            SetTemplate();
        }

        private void SetTemplate()
        {
            HeaderPanel.RenderTransform = new TranslateTransform();
            HistoryHeader.RenderTransform = new TranslateTransform();
            HistoryGrid.RenderTransform = new TranslateTransform();
            ScriptDesc.RenderTransform = new TranslateTransform();
            InfoPanel.RenderTransform = new TranslateTransform();
            StatPanel.RenderTransform = new TranslateTransform();
            AccessControls.RenderTransform = new TranslateTransform();

            Member = X.Singleton<SHMember>( XProto.SHMember );

            if ( BindItem.Encrypted )
            {
                Crypt = ( CryptAES ) new AESManager().GetAuthById( BindItem.Id );
            }

            AccessToken = ( string ) new TokenManager().GetAuthById( BindItem.Id )?.Value;

            if ( !string.IsNullOrEmpty( AccessToken ) || ( Member.IsLoggedIn && Member.Id == BindItem.AuthorId ) )
            {
                TransitionDisplay.SetState( AccessControls, TransitionState.Active );
            }

            Unloaded += ScriptDetails_Unloaded;

            UpdateTemplate( BindItem );
        }

        public void UpdateTemplate( HubScriptItem Item )
        {
            BindItem = Item;
            LayoutRoot.DataContext = BindItem;

            InitAppBar();
        }

        private void InitAppBar()
        {
            if ( DownloadBtn == null )
            {
                StringResources stx = new StringResources( "AppResources", "AppBar" );

                DownloadBtn = UIAliases.CreateAppBarBtn( Symbol.Download, stx.Text( "Download", "AppBar" ) );
                DownloadBtn.Click += ( s, e ) => Download();

                AppBarButton CommentBtn = UIAliases.CreateAppBarBtn( Symbol.Comment, stx.Text( "Comments" ) );
                CommentBtn.Click += ( s, e ) => ControlFrame.Instance.SubNavigateTo( this, () => new HSCommentView( BindItem, Crypt ) );

                AppBarButton TokReqBtn = UIAliases.CreateAppBarBtn( Symbol.Permissions, stx.Text( "TokenRequest" ) );
                TokReqBtn.Click += ( s, e ) => ControlFrame.Instance.SubNavigateTo( this, () => new HSRequestView( BindItem, Crypt, SHTarget.TOKEN, AccessToken ) );

                KeyReqBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.Lock, stx.Text( "KeyRequest" ) );
                KeyReqBtn.Click += ( s, e ) => ControlFrame.Instance.SubNavigateTo( this, () => new HSRequestView( BindItem, Crypt, SHTarget.KEY, AccessToken ) );

                MajorControls = new AppBarButton[] { CommentBtn, DownloadBtn };
                MinorControls = new AppBarButton[] { TokReqBtn, KeyReqBtn };
            }

            KeyReqBtn.IsEnabled = BindItem.Encrypted;
        }

        public void OpenComment()
        {
            var j = Dispatcher.RunIdleAsync( x =>
            {
                ControlFrame.Instance.SubNavigateTo( this, () => new HSCommentView( BindItem, Crypt ) );
            } );
        }

        public void OpenCommentStack( string Id )
        {
            ControlFrame.Instance.SubNavigateTo( this, () => {
                HSCommentView HSCV = new HSCommentView( BindItem, Crypt );
                HSCV.OpenCommentStack( Id );
                return HSCV;
            } );
        }

        public void OpenRequest( SHTarget ReqTarget )
        {
            var j = Dispatcher.RunIdleAsync( x =>
            {
                ControlFrame.Instance.SubNavigateTo( this, () => new HSRequestView( BindItem, Crypt, ReqTarget, AccessToken ) );
            } );
        }

        public void PlaceRequest( SHTarget Target )
        {
            ControlFrame.Instance.SubNavigateTo( this, () => {
                HSRequestView HSRV = new HSRequestView( BindItem, Crypt, Target, AccessToken );
                HSRV.PlaceRequest();
                return HSRV;
            } );
        }

        private void ScriptDetails_Unloaded( object sender, RoutedEventArgs e )
        {
            DataContext = null;
            HSComment.ActiveInstance = null;
        }

        private void ControlClick( object sender, ItemClickEventArgs e )
        {
            ( ( PaneNavButton ) e.ClickedItem ).Action();
        }

        private async void AssignKey( object sender, RoutedEventArgs e )
        {
            AssignAuth RequestBox = new AssignAuth( new AESManager(), "Assign Key" );
            await Popups.ShowDialog( RequestBox );

            if ( RequestBox.Canceled || RequestBox.SelectedAuth == null ) return;

            Crypt = ( CryptAES ) RequestBox.SelectedAuth;
        }

        private async void AssignToken( object sender, RoutedEventArgs e )
        {
            AssignAuth RequestBox = new AssignAuth( new TokenManager(), "Assign Token" );
            await Popups.ShowDialog( RequestBox );

            if ( RequestBox.Canceled || RequestBox.SelectedAuth == null ) return;

            AccessToken = ( string ) RequestBox.SelectedAuth.Value;
            TransitionDisplay.SetState( AccessControls, TransitionState.Active );
        }

        private void Update( object sender, RoutedEventArgs e )
        {
            ControlFrame.Instance.SubNavigateTo( this, () => new ScriptUpload( BindItem, UploadReturn ) );
        }

        private async void UploadReturn( string Id, string Token )
        {
            await ControlFrame.Instance.CloseSubView();
            AccessToken = Token;
            BindItem.Update( await PageProcessor.GetScriptFromHub( Id, Token ) );
            SetTemplate();
        }

        private async void Delete( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Message" );
            MessageDialog MsgBox = new MessageDialog( stx.Str( "ConfirmScriptRemove" ) );

            bool DoDelete = false;

            MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { DoDelete = true; } ) );
            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );
            await Popups.ShowDialog( MsgBox );

            if ( DoDelete ) RemoveHSI( BindItem );
        }

        private void RemoveHSI( HubScriptItem HSI )
        {
            TokenManager TokMgr = new TokenManager();
            AESManager AESMgr = new AESManager();

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptRemove( AccessToken, HSI.Id )
                , ( e2, QId ) =>
                {
                    try
                    {
                        JsonStatus.Parse( e2.ResponseString );
                        TokMgr.UnassignId( HSI.Id );
                        AESMgr.UnassignId( HSI.Id );

                        MessageBus.SendUI( GetType(), AppKeys.SH_SCRIPT_REMOVE, HSI );
                        Exit();
                    }
                    catch ( Exception ex )
                    {
                        HSI.ErrorMessage = ex.Message;
                    }
                }
                , ( a, b, ex ) =>
                {
                    HSI.ErrorMessage = ex.Message;
                }
                , false
            );
        }

        private void Exit()
        {
            ControlFrame.Instance.GoBack();
            ControlFrame.Instance.BackStack.Remove( PageId.SCRIPT_DETAILS );
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

        private void Download()
        {
            DownloadBtn.IsEnabled = false;

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
            DownloadBtn.IsEnabled = true;
            BindItem.ErrorMessage = ex.Message;
        }

        private void DownloadComplete( DRequestCompletedEventArgs e, string Id )
        {
            BindItem.SetScriptData( e.ResponseString );
        }

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


        #region Anima
        Storyboard AnimaStory = new Storyboard();

        public async Task EnterAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel, "Opacity", 0, 1, 350, 100 );
            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel.RenderTransform, "Y", 30, 0, 350, 100 );

            SimpleStory.DoubleAnimation( AnimaStory, ScriptDesc, "Opacity", 0, 1, 350, 200 );
            SimpleStory.DoubleAnimation( AnimaStory, ScriptDesc.RenderTransform, "Y", 31, 0, 350, 200 );

            SimpleStory.DoubleAnimation( AnimaStory, HistoryHeader, "Opacity", 0, 1, 350, 200 );
            SimpleStory.DoubleAnimation( AnimaStory, HistoryHeader.RenderTransform, "Y", 30, 0, 350, 200 );

            SimpleStory.DoubleAnimation( AnimaStory, HistoryGrid, "Opacity", 0, 1, 350, 200 );
            SimpleStory.DoubleAnimation( AnimaStory, HistoryGrid.RenderTransform, "Y", 30, 0, 350, 200 );

            SimpleStory.DoubleAnimation( AnimaStory, InfoPanel, "Opacity", 0, 1, 350, 300 );
            SimpleStory.DoubleAnimation( AnimaStory, InfoPanel.RenderTransform, "Y", 30, 0, 350, 300 );

            SimpleStory.DoubleAnimation( AnimaStory, StatPanel, "Opacity", 0, 1, 350, 400 );
            SimpleStory.DoubleAnimation( AnimaStory, StatPanel.RenderTransform, "Y", 30, 0, 350, 400 );

            SimpleStory.DoubleAnimation( AnimaStory, AccessControls, "Opacity", 0, 1, 350, 400 );
            SimpleStory.DoubleAnimation( AnimaStory, AccessControls.RenderTransform, "Y", 30, 0, 350, 400 );

            AnimaStory.Begin();
            await Task.Delay( 1000 );
        }

        public async Task ExitAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel, "Opacity", 1, 0, 350, 400 );
            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel.RenderTransform, "Y", 0, 30, 350, 400 );

            SimpleStory.DoubleAnimation( AnimaStory, ScriptDesc, "Opacity", 1, 0, 350, 300 );
            SimpleStory.DoubleAnimation( AnimaStory, ScriptDesc.RenderTransform, "Y", 0, 30, 350, 300 );

            SimpleStory.DoubleAnimation( AnimaStory, HistoryHeader, "Opacity", 1, 0, 350, 300 );
            SimpleStory.DoubleAnimation( AnimaStory, HistoryHeader.RenderTransform, "Y", 0, 30, 350, 300 );

            SimpleStory.DoubleAnimation( AnimaStory, HistoryGrid, "Opacity", 1, 0, 350, 300 );
            SimpleStory.DoubleAnimation( AnimaStory, HistoryGrid.RenderTransform, "Y", 0, 30, 350, 300 );

            SimpleStory.DoubleAnimation( AnimaStory, InfoPanel, "Opacity", 1, 0, 350, 200 );
            SimpleStory.DoubleAnimation( AnimaStory, InfoPanel.RenderTransform, "Y", 0, 30, 350, 200 );

            SimpleStory.DoubleAnimation( AnimaStory, StatPanel, "Opacity", 1, 0, 350, 100 );
            SimpleStory.DoubleAnimation( AnimaStory, StatPanel.RenderTransform, "Y", 0, 30, 350, 100 );

            SimpleStory.DoubleAnimation( AnimaStory, AccessControls, "Opacity", 1, 0, 350, 100 );
            SimpleStory.DoubleAnimation( AnimaStory, AccessControls.RenderTransform, "Y", 0, 30, 350, 100 );

            AnimaStory.Begin();
            await Task.Delay( 1000 );
        }
        #endregion

    }
}