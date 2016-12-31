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
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Text;
using wenku8.Resources;
using Net.Astropenguin.Helpers;

namespace wenku10.Pages
{
    sealed partial class TwitterCommentView : Page, ICmdControls
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

            AppBarButton LogoutBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.ChevronLeft, stx.Text( "Account_Logout", "Settings" ) );
            LogoutBtn.Click += LogoutBtn_Click;

            Major2ndControls = new ICommandBarElement[] { LogoutBtn };
        }

        private void OpenInputBox( object sender, RoutedEventArgs e )
        {
            TransitionDisplay.SetState( TweetBox, TransitionState.Active );

            TweetInput.Text = Keywords.Text + " #wenku10";
            TweetInput.SelectionStart = Keywords.Text.Length;
            TweetInput.Focus( FocusState.Keyboard );

            UpdateCharLeft();

            MajorControls = InputControls;
            ControlChanged?.Invoke( this );
        }

        private void CloseInputBox( object sender, RoutedEventArgs e )
        {
            CloseInputBox();
        }

        private async void CloseInputBox()
        {
            if ( !( string.IsNullOrEmpty( TweetInput.Text ) || TweetInput.Text == ( Keywords.Text + " #wenku10" ) ) )
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

            Loader.Keyword = ThisBook.Title.TrimForSearch();
            Loader.Tags = TagsAvailable;

            TagsAvailable.Add( new NameValue<bool>( "wenku10", true ) );

            if ( !string.IsNullOrEmpty( ThisBook.Title ) )
                TagsAvailable.AddRange( ThisBook.Title.ToHashTags().Remap( x => new NameValue<bool>( x, false ) ) );

            if ( !string.IsNullOrEmpty( ThisBook.PressRaw ) )
                TagsAvailable.AddRange( ThisBook.PressRaw.ToHashTags().Remap( x => new NameValue<bool>( x, false ) ) );

            if ( !string.IsNullOrEmpty( ThisBook.AuthorRaw ) )
                TagsAvailable.AddRange( ThisBook.AuthorRaw.ToHashTags().Remap( x => new NameValue<bool>( x, false ) ) );

            Keywords.Text = Loader.Keyword;
            HashTags.ItemsSource = TagsAvailable;

            Observables<Tweet, Tweet> Tweets = new Observables<Tweet, Tweet>( await Loader.NextPage( 20 ) );
            Tweets.ConnectLoader( Loader );

            TweetsView.ItemsSource = Tweets;
        }

        private async void SubmitBtn_Click( object sender, RoutedEventArgs e )
        {
            string TweetContent = TweetInput.Text.Trim();

            if( TweetLimit < TweetContent.Length ) return;

            if ( !( TweetContent.Contains( "#wenku10" ) && TweetContent.Contains( Keywords.Text ) ) )
            {
                bool Continue = false;
                StringResources stx = new StringResources( "Message" );
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    stx.Str( "Desc_OSTweet" ), stx.Str( "OSTweet" )
                    , () => Continue = true
                    , stx.Str( "Yes" ), stx.Str( "No" )
                ) );

                if ( !Continue )
                {
                    string ScopeText = "";

                    if ( !TweetContent.Contains( Keywords.Text ) ) ScopeText += Keywords.Text + " ";
                    if ( !TweetContent.Contains( "#wenku10" ) ) ScopeText += "#wenku10 ";

                    TweetInput.Text = ScopeText + TweetInput.Text;

                    TweetInput.SelectionStart = 0;
                    TweetInput.SelectionLength = ScopeText.Length;

                    TweetInput.Focus( FocusState.Keyboard );

                    UpdateCharLeft();
                    return;
                }
            }

            try
            {
                await TwitterService.Instance.TweetStatusAsync( TweetContent );

                InsertFakeTweet( TweetContent );

                TweetInput.Text = "";
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

            Observables<Tweet, Tweet> Tweets = new Observables<Tweet, Tweet>( await Loader.NextPage( 20 ) );
            Tweets.ConnectLoader( Loader );

            TweetsView.ItemsSource = Tweets;
        }

        private void InsertFakeTweet( string FTweet )
        {
            Observables<Tweet, Tweet> Tweets = ( Observables<Tweet, Tweet> ) TweetsView.ItemsSource;
            Tweets.Insert( 0, new Tweet()
            {
                Text = FTweet
                , Id = "-1"
                , User = CurrentUser
                , CreatedAt = DateTime.Now.ToString( "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.InvariantCulture )
            } );
        }

        private void CalcLimit( object sender, TextChangedEventArgs e ) { UpdateCharLeft(); }

        private void UpdateCharLeft()
        {
            int Overflow = TweetLimit - TweetInput.Text.Length;
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