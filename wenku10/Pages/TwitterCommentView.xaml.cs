using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Microsoft.Toolkit.Uwp.Services.Twitter;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Text;
using wenku8.Model.Twitter;
using wenku8.Resources;
using Net.Astropenguin.Controls;

namespace wenku10.Pages
{
    sealed partial class TwitterCommentView : Page, INavPage, ICmdControls
    {
#pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

        public const int TweetLimit = 140;

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get; private set; }

        AppBarButton AddBtn;
        AppBarButton SubmitBtn;
        AppBarButton DiscardBtn;

        ICommandBarElement[] CommentControls;
        ICommandBarElement[] InputControls;

        List<NameValue<bool>> TagsAvailable = new List<NameValue<bool>>();

        BookItem ThisBook;

        TwitterUser CurrentUser;
        Tweet InReplyTo;
        Action RmCtrlEnterListener;

        private TwitterCommentView()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        public TwitterCommentView( BookItem Book )
            : this()
        {
            ThisBook = Book;
            SetContext();
        }

        public void SoftOpen()
        {
            NavigationHandler.InsertHandlerOnNavigatedBack( ShouldCloseInputBox );
            RmCtrlEnterListener = App.KeyboardControl.RegisterCombination( e => CtrlSubmit(), Windows.System.VirtualKey.Control, Windows.System.VirtualKey.Enter );
        }

        public void SoftClose()
        {
            NavigationHandler.OnNavigatedBack -= ShouldCloseInputBox;
            RmCtrlEnterListener?.Invoke();
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "AppBar", "AppResources", "Settings" );

            AddBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Str( "AddComment" ) );
            AddBtn.Click += OpenInputBox;

            SubmitBtn = UIAliases.CreateAppBarBtn( Symbol.Send, stx.Text( "Button_Post", "AppResources" ) );
            SubmitBtn.Click += SubmitBtn_Click;

            DiscardBtn = UIAliases.CreateAppBarBtn( Symbol.Delete, "Discard" );
            DiscardBtn.Click += CloseInputBox;

            CommentControls = new ICommandBarElement[] { AddBtn };
            InputControls = new ICommandBarElement[] { SubmitBtn, DiscardBtn };

            MajorControls = CommentControls;

            SecondaryIconButton LogoutBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.ChevronLeft, stx.Text( "Account_Logout", "Settings" ) + " ( twitter )" );
            LogoutBtn.Click += LogoutBtn_Click;

            Major2ndControls = new ICommandBarElement[] { LogoutBtn };
        }

        private void ReplyBtn_Click( object sender, RoutedEventArgs e )
        {
            InReplyTo = ( Tweet ) ( ( Button ) sender ).DataContext;
            OpenInputBox( sender, e );
        }

        private void OpenTweetBtn_Click( object sender, RoutedEventArgs e )
        {
            Tweet Tw = ( Tweet ) ( ( Button ) sender ).DataContext;
            var j = Windows.System.Launcher.LaunchUriAsync( new Uri( $"https://twitter.com/{Tw.User.ScreenName}/status/{Tw.Id}" ) );
        }

        private void OpenInputBox( object sender, RoutedEventArgs e )
        {
            TransitionDisplay.SetState( TweetBox, TransitionState.Active );

            if ( InReplyTo != null )
                ReplyToName.Text = InReplyTo.User.ScreenName;

            TweetInput.Text = " " + Keywords.Text + " #wenku10";
            TweetInput.SelectionStart = 0;
            TweetInput.Focus( FocusState.Keyboard );

            UpdateCharLeft();

            MajorControls = InputControls;
            ControlChanged?.Invoke( this );
        }

        private void ShouldCloseInputBox( object sender, XBackRequestedEventArgs e )
        {
            if( TransitionDisplay.GetState( TweetBox ) == TransitionState.Active )
            {
                e.Handled = true;
                CloseInputBox();
            }
        }

        private void CloseInputBox( object sender, RoutedEventArgs e ) { CloseInputBox(); }

        private async void CloseInputBox()
        {
            if ( !( string.IsNullOrEmpty( TweetInput.Text ) || TweetInput.Text == ( " " + Keywords.Text + " #wenku10" ) ) )
            {
                bool Discard = false;

                StringResources stx = new StringResources( "Message" );
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    stx.Str( "ConfirmDiscard" )
                    , () => Discard = true
                    , stx.Str( "Yes" ), stx.Str( "No" )
                ) );

                if ( !Discard ) return;
            }

            InReplyTo = null;
            ReplyToName.Text = "";

            TransitionDisplay.SetState( TweetBox, TransitionState.Inactive );

            MajorControls = CommentControls;
            ControlChanged?.Invoke( this );
        }

        private void LogoutBtn_Click( object sender, RoutedEventArgs e )
        {
            TwitterService.Instance.Logout();
            var j = ControlFrame.Instance.CloseSubView();
        }

        private void SetTemplate()
        {
            InitAppBar();
        }

        private async void SetContext()
        {
            TwitterLoader Loader = new TwitterLoader();
            await Loader.Authenticate();

            CurrentUser = await TwitterService.Instance.GetUserAsync();

            TagsAvailable.Add( new NameValue<bool>( "wenku10", true ) );

            if ( !string.IsNullOrEmpty( ThisBook.Title ) )
                TagsAvailable.AddRange( ThisBook.Title.ToHashTags().Remap( x => new NameValue<bool>( x, false ) ) );

            if ( !string.IsNullOrEmpty( ThisBook.PressRaw ) )
                TagsAvailable.AddRange( ThisBook.PressRaw.ToHashTags().Remap( x => new NameValue<bool>( x, false ) ) );

            if ( !string.IsNullOrEmpty( ThisBook.AuthorRaw ) )
                TagsAvailable.AddRange( ThisBook.AuthorRaw.ToHashTags().Remap( x => new NameValue<bool>( x, false ) ) );

            Keywords.Text = ThisBook.Title.TrimForSearch();
            HashTags.ItemsSource = TagsAvailable;

            ReloadTweets();
        }

        private async void CtrlSubmit()
        {
            if ( TweetInput.FocusState == FocusState.Keyboard )
            {
                bool Continue = false;
                StringResources stx = new StringResources( "Message" );
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    stx.Str( "ConfirmSubmit" )
                    , () => Continue = true
                    , stx.Str( "Yes" ), stx.Str( "No" )
                ) );

                if ( Continue ) Submit();
            }
        }

        private void SubmitBtn_Click( object sender, RoutedEventArgs e ) { Submit(); } 
        private async void Submit()
        {
            string TweetContent = TweetInput.Text.Replace( "\r\n", "\n" ).Trim();

            if( TweetLimit < TweetContent.Length ) return;

            if ( !SubmitBtn.IsEnabled ) return;
            SubmitBtn.IsEnabled = false;

            if ( !( TweetContent.Contains( "#wenku10" ) && TweetContent.Contains( Keywords.Text ) ) )
            {
                bool Continue = false;
                StringResources stx = new StringResources( "Message" );
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    string.Format( stx.Str( "Desc_OSTweet" ), Keywords.Text, "#wenku10" )
                    , stx.Str( "OSTweet" )
                    , () => Continue = true
                    , stx.Str( "Yes" ), stx.Str( "No" )
                ) );

                if ( !Continue )
                {
                    string ScopeText = "";

                    if ( !TweetContent.Contains( Keywords.Text ) ) ScopeText += Keywords.Text + " ";
                    if ( !TweetContent.Contains( "#wenku10" ) ) ScopeText += "#wenku10 ";

                    TweetInput.Text = TweetContent + " " + ScopeText;
                    TweetInput.SelectionStart = TweetContent.Length;
                    TweetInput.SelectionLength = ScopeText.Length;

                    TweetInput.Focus( FocusState.Keyboard );

                    UpdateCharLeft();

                    SubmitBtn.IsEnabled = true;
                    return;
                }
            }

            try
            {
                if ( InReplyTo == null )
                {
                    await TwitterService.Instance.TweetStatusAsync( TweetContent );
                }
                else
                {
                    await TSExtended.Instance.ReplyStatusAsync( TweetContent, InReplyTo.Id );
                }

                InsertFakeTweet( TweetContent );

                TweetInput.Text = "";
                SubmitBtn.IsEnabled = true;

                CloseInputBox();
            }
            catch( TwitterException ex )
            {
                var j = Popups.ShowDialog( UIAliases.CreateDialog( ex.Message ) );
            }
        }

        private void ToggleTag( object sender, TappedRoutedEventArgs e )
        {
            NameValue<bool> NV = ( NameValue<bool> ) ( ( Grid ) sender ).DataContext;
            NV.Value = !NV.Value;

            ReloadTweets();
        }

        private async void ReloadTweets()
        {
            TwitterLoader Loader = new TwitterLoader();
            await Loader.Authenticate();

            Loader.Keyword = ThisBook.Title.TrimForSearch();
            Loader.Tags = TagsAvailable;

            LoadingRing.IsActive = true;

            Observables<Tweet, Tweet> Tweets = new Observables<Tweet, Tweet>( await Loader.NextPage( 20 ) );
            Tweets.ConnectLoader( Loader );

            Tweets.LoadStart += ( s, e ) => { LoadingRing.IsActive = true; };
            Tweets.LoadEnd += ( s, e ) => { LoadingRing.IsActive = false; };

            LoadingRing.IsActive = false;

            TweetsView.ItemsSource = Tweets;
        }

        private void InsertFakeTweet( string FTweet )
        {
            Observables<Tweet, Tweet> Tweets = ( Observables<Tweet, Tweet> ) TweetsView.ItemsSource;
            Tweets.Insert( 0, new Tweet()
            {
                Text = FTweet
                , User = CurrentUser
                , CreatedAt = DateTime.Now.ToString( "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.InvariantCulture )
            } );
        }

        private void CalcLimit( object sender, TextChangedEventArgs e ) { UpdateCharLeft(); }

        private void UpdateCharLeft()
        {
            int Overflow = TweetLimit - TweetInput.Text.Replace( "\r\n", "\n" ).Length;
            CharLimit.Text = Overflow.ToString();

            if( Overflow < 0 )
            {
                InputBorder.BorderBrush = new SolidColorBrush( Colors.Red );
            }
            else
            {
                InputBorder.BorderBrush = null;
            }
        }
    }
}