using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.UI.Icons;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.UI;

using wenku8.AdvDM;
using wenku8.Model.Comments;
using wenku8.Model.ListItem;
using wenku8.Model.REST;
using wenku8.Resources;
using wenku8.ThemeIcons;

namespace wenku10.ShHub
{
    using AESManager = wenku8.System.AESManager;
    using TokenManager = wenku8.System.TokenManager;
    using CryptAES = wenku8.System.CryptAES;
    using SHTarget = SharersRequest.SHTarget;
    using RequestTarget = SharersRequest.SHTarget;

    sealed partial class ScriptDetails : Page
    {
        public static readonly string ID = typeof( ScriptDetails ).Name;

        private Storyboard CommentStory;
        private Storyboard RequestStory;
        private ObservableCollection<PaneNavButton> BottomControls;
        private Observables<HSComment, HSComment> CommentsSource;
        private Observables<SHRequest, SHRequest> RequestsSource;

        private Dictionary<string, PaneNavButton> AvailControls;

        private bool CommentsOpened = false;
        private volatile bool CommInit = false;

        private bool RequestsOpened = false;
        private volatile bool ReqInit = false;

        private HubScriptItem BindItem;
        private RuntimeCache RCache = new RuntimeCache();

        private SHTarget CCTarget = SHTarget.SCRIPT;
        private CryptAES Crypt;
        private string CCId;

        private string[] HomeControls = new string[] { "OpenRequest", "Comment", "Download" };
        private string[] CommentControls = new string[] { "NewComment", "HideComment" };

        public ScriptDetails( HubScriptItem Item )
        {
            this.InitializeComponent();

            BindItem = Item;
            DataContext = BindItem;

            if( BindItem.Encrypted )
            {
                Crypt = new AESManager().GetAuthById( BindItem.Id );
            }

            SetTemplate();
        }

        private void SetTemplate()
        {
            BottomControls = new ObservableCollection<PaneNavButton>();

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
                , { "KeyRequest", new PaneNavButton( new IconRawDocument() { AutoScale = true }, ShowKeyRequest ) }
                , { "TokenRequest", new PaneNavButton( new IconMasterKey() { AutoScale = true }, ShowTokenRequest ) }
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

        #region Download
        private void Download()
        {
            KeyValuePair<string, string> AccessToken = new TokenManager().GetAuthById( BindItem.Id );
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptDownload( BindItem.Id, AccessToken.Value )
                , DownloadComplete
                , DownloadFailed
                , false
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
        public void ToggleRequests()
        {
            if ( RequestsOpened )
            {
                SlideOutRequests();
            }
            else
            {
                SlideInRequests();
            }
        }

        private void SlideInRequests()
        {
            if ( RequestStory.GetCurrentState() != ClockState.Stopped ) return;

            DisplayControls( "TokenRequest", "KeyRequest", "CloseRequest" );

            RequestStory.Children.Clear();

            SetDoubleAnimation(
                RequestStory
                , RequestSection
                , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                , 0.25 * LayoutSettings.ScreenHeight, 0
            );

            SetDoubleAnimation( RequestStory, RequestSection, "Opacity", 0, 1 );

            RequestSection.Visibility = Visibility.Visible;
            RequestStory.Begin();
        }

        private void SlideOutRequests()
        {
            if ( RequestStory.GetCurrentState() != ClockState.Stopped ) return;

            DisplayControls( HomeControls );

            RequestStory.Children.Clear();

            SetDoubleAnimation(
                RequestStory
                , RequestSection
                , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                , 0, 0.25 * LayoutSettings.ScreenHeight
            );

            SetDoubleAnimation( RequestStory, RequestSection, "Opacity", 1, 0 );

            RequestStory.Begin();
        }

        private void ShowKeyRequest() { ReloadRequests( SHTarget.KEY ); }
        private void ShowTokenRequest() { ReloadRequests( SHTarget.TOKEN ); }

        private async void ReloadRequests( SHTarget Target )
        {
            if ( LoadingRing.IsActive ) return;

            MarkLoading();
            HSLoader<SHRequest> CLoader = new HSLoader<SHRequest>(
                BindItem.Id
                , Target
                , ( _Target, _Skip, _Limit, _Ids ) => Shared.ShRequest.GetRequests( _Target, _Ids[0], _Skip, _Limit )
            );

            IList<SHRequest> FirstPage = await CLoader.NextPage();
            MarkNotLoading();

            RequestsSource = new Observables<SHRequest, SHRequest>( FirstPage );
            RequestsSource.ConnectLoader( CLoader );

            RequestsSource.LoadStart += ( x, y ) => MarkLoading();
            RequestsSource.LoadEnd += ( x, y ) => MarkNotLoading();
            RequestList.ItemsSource = RequestsSource;
        }

        private void RequestList_ItemClick( object sender, ItemClickEventArgs e )
        {
        }

        private void RequestStory_Completed( object sender, object e )
        {
            RequestStory.Stop();
            if ( !RequestsOpened )
            {
                RequestSection.Opacity = 1;
                RequestsOpened = true;
            }
            else if( RequestsOpened )
            {
                RequestSection.Visibility = Visibility.Collapsed;
                RequestSection.Opacity = 0;
                RequestsOpened = false;
            }
        }
        #endregion

        #region Comments
        private void ToggleComments()
        {
            if ( CommentsOpened )
            {
                SlideOutComments();
            }
            else
            {
                SlideInComments();

                if ( !CommInit )
                {
                    CommInit = true;
                    ReloadComments();
                }
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

        private void SlideInComments()
        {
            if ( CommentStory.GetCurrentState() != ClockState.Stopped ) return;

            DisplayControls( CommentControls );

            CommentStory.Children.Clear();

            SetDoubleAnimation(
                CommentStory
                , CommentSection
                , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                , 0.25 * LayoutSettings.ScreenHeight, 0
            );

            SetDoubleAnimation( CommentStory, CommentSection, "Opacity", 0, 1 );

            CommentSection.Visibility = Visibility.Visible;
            CommentStory.Begin();
        }

        private void SlideOutComments()
        {
            if ( CommentStory.GetCurrentState() != ClockState.Stopped ) return;

            DisplayControls( HomeControls );

            CommentStory.Children.Clear();

            SetDoubleAnimation(
                CommentStory
                , CommentSection
                , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                , 0, 0.25 * LayoutSettings.ScreenHeight
            );

            SetDoubleAnimation( CommentStory, CommentSection, "Opacity", 1, 0 );

            CommentStory.Begin();
        }

        private void CommentList_ItemClick( object sender, ItemClickEventArgs e )
        {
            ( ( HSComment ) e.ClickedItem ).MarkSelect();
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
            CommentEditor.State = ControlState.Reovia;
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

        private void SubmitComment()
        {
            string Data;
            CommentInput.Document.GetText( Windows.UI.Text.TextGetOptions.None, out Data );
            Data = Data.Trim();

            if( string.IsNullOrEmpty( Data) )
            {
                CommentInput.Focus( FocusState.Keyboard );
                return;
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
                CommentInput.Document.SetText( Windows.UI.Text.TextSetOptions.None, "" );
                JsonStatus.Parse( e.ResponseString );
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
            CommentEditor.State = ControlState.Foreatii;
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
        #endregion

        private void SetDoubleAnimation( Storyboard Board, UIElement Element, string Property, double From, double To, double Duration = 350 )
        {
            DoubleAnimationUsingKeyFrames d = new DoubleAnimationUsingKeyFrames();

            EasingDoubleKeyFrame still = new EasingDoubleKeyFrame();
            still.Value = From;
            still.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromSeconds( 0 ) );
            still.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };

            EasingDoubleKeyFrame move = new EasingDoubleKeyFrame();
            move.Value = To;
            move.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };
            move.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromMilliseconds( Duration ) );

            d.Duration = new Duration( TimeSpan.FromMilliseconds( Duration ) );

            d.KeyFrames.Add( still );
            d.KeyFrames.Add( move );

            Storyboard.SetTarget( d, Element );
            Storyboard.SetTargetProperty( d, Property );
            Board.Children.Add( d );
        }

        private void MarkLoading()
        {
            LoadingRing.IsActive = true;
        }

        private void MarkNotLoading()
        {
            LoadingRing.IsActive = false;
        }

        private void PlaceKeyRequest( object sender, RoutedEventArgs e )
        {
        }
    }
}