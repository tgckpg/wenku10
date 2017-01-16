using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Microsoft.Toolkit.Uwp.Services.Twitter;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Twitter;

namespace wenku10.Pages
{
    using Dialogs;
    using Scenes;

    public sealed partial class About : Page, ICmdControls
    {
        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get { return true; } }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get ; private set; }

        private CanvasStage CStage;

        public About()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            TestTwitter();

            CStage = new CanvasStage( Stage );
            CStage.Add( new Fireworks() );
            Stage.Paused = true;

            Unloaded += About_Unloaded;
        }

        private async void AddTweet_Click( object sender, RoutedEventArgs e )
        {
            Button Btn = ( Button ) sender;
            Btn.IsEnabled = false;

            if ( !await AuthData.Authenticate() ) goto TweetEnd;

            string TweetText = "";
            StringResources stx = new StringResources( "Error", "AppResources" );

            TweetStart:

            ValueHelpInput TweetInput = new ValueHelpInput( "", "wenku10 ♥", stx.Text( "Tweetwenku10", "AppResources" ) );
            TweetInput.Value = TweetText;

            await Popups.ShowDialog( TweetInput );

            if ( TweetInput.Canceled ) goto TweetEnd;

            TweetText = TweetInput.Value.Trim();
            if ( string.IsNullOrEmpty( TweetText ) ) goto TweetEnd;

            if ( 131 < TweetText.Length )
            {
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    stx.Str( "TweetTooLong" ) + string.Format( " ( {0} < {1} )", 131, TweetText.Length )
                ) );
                goto TweetStart;
            }

            if ( await TwitterService.Instance.TweetStatusAsync( TweetText + " #wenku10" ) )
            {
                Observables<Tweet, Tweet> Tweets = ( Observables<Tweet, Tweet> ) TweetsView.ItemsSource;
                Tweets.Insert( 0, new Tweet()
                {
                    Text = TweetText
                    , User = await TwitterService.Instance.GetUserAsync()
                    , CreatedAt = DateTime.Now.ToString( "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.InvariantCulture )
                } );
            }
            else
            {
                await Popups.ShowDialog( UIAliases.CreateDialog( stx.Str( "SubmitError" ) ) );
                goto TweetStart;
            }

            TweetEnd:
            Btn.IsEnabled = true;
        }

        private void OpenTwitter_Click( object sender, RoutedEventArgs e )
        {
            if ( TransitionDisplay.GetState( TwitterBtn ) == TransitionState.Active )
            {
                TransitionDisplay.SetState( TwitterBtn, TransitionState.Inactive );
                SetTwitter();
            }
        }

        private void OpenEffects_Click( object sender, RoutedEventArgs e )
        {
            if ( TransitionDisplay.GetState( EffectsBtn ) == TransitionState.Active )
            {
                TransitionDisplay.SetState( EffectsBtn, TransitionState.Inactive );

                if ( Stage != null )
                    Stage.Paused = false;
            }
        }

        private async void TestTwitter()
        {
            wenku8.Settings.Layout.BookInfoView InfoView = new wenku8.Settings.Layout.BookInfoView();

            if( InfoView.TwitterConfirmed )
            {
                TwitterBtn.IsEnabled = false;

                await Task.Delay( 1000 );
                TransitionDisplay.SetState( TwitterBtn, TransitionState.Inactive );
                SetTwitter();
            }
        }

        private async void SetTwitter()
        {
            TwitterService.Instance.Initialize( AuthData.Token );
            TwitterLoader Loader = new TwitterLoader();

            Loader.Tags = new List<NameValue<bool>>();
            Loader.Tags.Add( new NameValue<bool>( "wenku10", true ) );

            LoadingRing.IsActive = true;
            Observables<Tweet, Tweet> Tweets = new Observables<Tweet, Tweet>( await Loader.NextPage( 20 ) );
            LoadingRing.IsActive = false;

            Tweets.LoadStart += ( s, e ) => LoadingRing.IsActive = true;
            Tweets.LoadEnd += ( s, e ) => LoadingRing.IsActive = false;

            Tweets.ConnectLoader( Loader );

            TweetsView.ItemsSource = Tweets;
        }

        private void About_Unloaded( object sender, RoutedEventArgs e )
        {
            if ( Stage != null )
            {
                Stage.RemoveFromVisualTree();
                Stage = null;

                CStage.Dispose();
                CStage = null;
            }
        }

    }
}