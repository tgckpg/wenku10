﻿using System;
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

namespace wenku10.Pages.ShHub
{
    using Dialogs.Sharers;

    using AESManager = wenku8.System.AESManager;
    using TokenManager = wenku8.System.TokenManager;
    using CryptAES = wenku8.System.CryptAES;
    using CryptRSA = wenku8.System.CryptRSA;
    using SHTarget = SharersRequest.SHTarget;
    using Net.Astropenguin.Helpers;

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
        private volatile SHTarget ReqTarget;

        private HubScriptItem BindItem;
        private RuntimeCache RCache = new RuntimeCache();

        private SHTarget CCTarget = SHTarget.SCRIPT;
        private string AccessToken;
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
                ReqTarget = SHTarget.KEY;
                Crypt = new AESManager().GetAuthById( BindItem.Id );
            }

            SetTemplate();
        }

        private void SetTemplate()
        {
            BottomControls = new ObservableCollection<PaneNavButton>();
            AccessToken = new TokenManager().GetAuthById( BindItem.Id ).Value;

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
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptDownload( BindItem.Id, AccessToken )
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
        private void PlaceKeyRequest( object sender, RoutedEventArgs e ) { PlaceRequest( SHTarget.KEY, BindItem ); }
        private void PlaceTokenRequest( object sender, RoutedEventArgs e ) { PlaceRequest( SHTarget.TOKEN, BindItem ); }

        public async void PlaceRequest( SHTarget Target, HubScriptItem HSI )
        {
            StringResources stx = new StringResources();

            PlaceRequest RequestBox = new PlaceRequest(
                Target, HSI
                , stx.Text( ( Target ^ SHTarget.KEY ) == 0 ? "KeyRequest" : "TokenRequest" )
            );

            await Popups.ShowDialog( RequestBox );

            if ( !RequestBox.Canceled )
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
        }

        private void ToggleRequests()
        {
            if ( RequestStory.GetCurrentState() != ClockState.Stopped ) return;
            RequestStory.Children.Clear();

            // Slide In / Slide Out
            if ( RequestsOpened )
            {
                DisplayControls( HomeControls );

                SetDoubleAnimation(
                    RequestStory
                    , RequestSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0, 0.25 * LayoutSettings.ScreenHeight
                );

                SetDoubleAnimation( RequestStory, RequestSection, "Opacity", 1, 0 );
            }
            else
            {
                DisplayControls(
                    BindItem.Encrypted
                    ? new string[] { "KeyRequest", "TokenRequest", "CloseRequest" }
                    : new string[] { "TokenRequest", "CloseRequest" }
                );

                SetDoubleAnimation(
                    RequestStory
                    , RequestSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0.25 * LayoutSettings.ScreenHeight, 0
                );

                SetDoubleAnimation( RequestStory, RequestSection, "Opacity", 0, 1 );

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
            if ( ( Target ^ SHTarget.KEY ) == 0 )
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
                        , Shared.ShRequest.GrantRequest( BindItem.Id, GrantData )
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
            System.Diagnostics.Debugger.Break();
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
        private void ToggleComments()
        {
            if ( CommentStory.GetCurrentState() != ClockState.Stopped ) return;
            CommentStory.Children.Clear();

            // Slide In / Slide Out
            if ( CommentsOpened )
            {
                DisplayControls( HomeControls );

                SetDoubleAnimation(
                    CommentStory, CommentSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0, 0.25 * LayoutSettings.ScreenHeight
                );

                SetDoubleAnimation( CommentStory, CommentSection, "Opacity", 1, 0 );
            }
            else
            {
                DisplayControls( CommentControls );

                SetDoubleAnimation(
                    CommentStory, CommentSection
                    , "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    , 0.25 * LayoutSettings.ScreenHeight, 0
                );

                SetDoubleAnimation( CommentStory, CommentSection, "Opacity", 0, 1 );

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
    }
}