using System;
using System.Collections.Generic;
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

using Net.Astropenguin.Logging;
using wenku8.Model.ListItem;
using wenku8.Resources;
using wenku8.AdvDM;
using Net.Astropenguin.Loaders;

namespace wenku10.ShHub
{
    public sealed partial class ScriptDetails : Page
    {
        public static readonly string ID = typeof( ScriptDetails ).Name;

        private Storyboard CommentStory;
        private bool CommentsOpened = false;

        private HubScriptItem BindItem;

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            BindItem = e.Parameter as HubScriptItem;
            DataContext = BindItem;
        }

        public ScriptDetails()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            CommentStory = new Storyboard();
            CommentStory.Completed += CommentStory_Completed;
        }

        private void ShowComments( object sender, RoutedEventArgs e )
        {
            if ( CommentsOpened )
            {
                SlideOutComments();
            }
            else
            {
                SlideInComments();
            }
        }

        private void Download( object sender, RoutedEventArgs e )
        {
            RuntimeCache RCache = new RuntimeCache();
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptDownload( BindItem.Id )
                , DownloadComplete
                , DownloadFailed
                , false
            );
        }

        private void DownloadFailed( string CacheName, string Id, Exception ex )
        {
            throw new NotImplementedException();
        }

        private void DownloadComplete( DRequestCompletedEventArgs e, string Id )
        {
            BindItem.SetScriptData( e.ResponseString );
        }

        private void SlideInComments()
        {
            if ( CommentStory.GetCurrentState() != ClockState.Stopped ) return;

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
    }
}