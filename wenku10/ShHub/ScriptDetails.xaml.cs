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
using Net.Astropenguin.Logging;
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
    using AuthManager = wenku8.System.AuthManager;
    using CryptAES = wenku8.System.CryptAES;
    using CommentTarget = SharersRequest.CommentTarget;

    sealed partial class ScriptDetails : Page
    {
        public static readonly string ID = typeof( ScriptDetails ).Name;

        private Storyboard CommentStory;
        private ObservableCollection<PaneNavButton> BottomControls;
        private Observables<HSComment, HSComment> CommentsSource;

        private Dictionary<string, PaneNavButton> AvailControls;

        private bool CommentsOpened = false;
        private volatile bool CommInit = false;

        private HubScriptItem BindItem;
        private RuntimeCache RCache = new RuntimeCache();

        private CommentTarget CCTarget = CommentTarget.SCRIPT;
        private CryptAES Crypt;
        private string CCId;

        public ScriptDetails( HubScriptItem Item )
        {
            this.InitializeComponent();

            BindItem = Item;
            DataContext = BindItem;

            if( BindItem.Encrypted )
            {
                Crypt = new AuthManager().GetKeyById( BindItem.Id );
            }

            SetTemplate();
        }

        private void SetTemplate()
        {
            BottomControls = new ObservableCollection<PaneNavButton>();

            AvailControls = new Dictionary<string, PaneNavButton>()
            {
                { "Download", new PaneNavButton( new IconLogin() { AutoScale = true, Direction = Direction.Rotate270 }, Download ) }
                , { "Comment", new PaneNavButton( new IconComment() { AutoScale = true }, ShowComments ) }
                , { "HideComment", new PaneNavButton( new IconNavigateArrow() { AutoScale = true, Direction = Direction.MirrorHorizontal }, ShowComments ) }
                , { "NewComment", new PaneNavButton( new IconPlusSign() { AutoScale = true }, () => {
                    StringResources stx = new StringResources( "AppBar" );
                    CCTarget = CommentTarget.SCRIPT;
                    CCId = BindItem.Id;
                    NewComment( stx.Str( "AddComment" ) );
                } ) }
                , { "KeyRequest", new PaneNavButton( new IconManTalksAboutKey() { AutoScale = true }, OpenKeyRequests ) }
                , { "Submit", new PaneNavButton( new IconTick() { AutoScale = true }, SubmitComment ) }
                , { "Discard", new PaneNavButton( new IconCross() { AutoScale = true }, DiscardComment ) }
            };

            DisplayControls( "KeyRequest", "Comment", "Download" );

            ControlsList.ItemsSource = BottomControls;

            CommentStory = new Storyboard();
            CommentStory.Completed += CommentStory_Completed;
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

        private void Download()
        {
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptDownload( BindItem.Id, new AuthManager().GetATokenById( BindItem.Id ) )
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

        private void ShowComments()
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
            HSCommentLoader CLoader = new HSCommentLoader( BindItem.Id, CommentTarget.SCRIPT );
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
                    HSC.Title = "[Failed to decrypt]\n" + CryptAES.RawBytes( HSC.Title );
                }
            }

            return Comments;
        }

        private IList<HSComment> CrippledComments( IList<HSComment> Comments )
        {
            foreach( HSComment HSC in Comments )
            {
                HSC.Title = "[Encrypted Comment]\n" + CryptAES.RawBytes( HSC.Title );
            }

            return Comments;
        }

        private void SlideInComments()
        {
            if ( CommentStory.GetCurrentState() != ClockState.Stopped ) return;

            DisplayControls( "NewComment", "HideComment" );

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

            DisplayControls( "KeyRequest", "Comment", "Download" );

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

            CCTarget = CommentTarget.COMMENT;
            CCId = HSC.Id;
            NewComment( stx.Text( "Reply" ) );
        }

        private void NewComment( string Label )
        {
            if( Crypt == null )
            {
                StringResources stx = new StringResources();
                CommentError.Text = stx.Text( "CommentsEncrypted" );
                return;
            }

            CommentEditor.State = ControlState.Reovia;
            DisplayControls( "Submit", "Discard" );
            CommentError.Text = "";
            CommentModeLabel.Text = Label;
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
                , Shared.ShRequest.Comment( CCTarget, CCId, Data )
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
            DisplayControls( "NewComment", "Comment", "Download" );
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

        private void OpenKeyRequests()
        {

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