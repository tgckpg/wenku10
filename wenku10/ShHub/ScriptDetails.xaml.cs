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

using Net.Astropenguin.UI;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.DataModel;

using wenku8.AdvDM;
using wenku8.Model.Comments;
using wenku8.Model.ListItem;
using wenku8.Resources;
using AuthManager = wenku8.System.AuthManager;

namespace wenku10.ShHub
{
    sealed partial class ScriptDetails : Page
    {
        public static readonly string ID = typeof( ScriptDetails ).Name;

        private Storyboard CommentStory;
        private Observables<HSComment, HSComment> CommentsSource;
        private bool CommentsOpened = false;
        private volatile bool CommInit = false;

        private HubScriptItem BindItem;

        public ScriptDetails( HubScriptItem Item )
        {
            this.InitializeComponent();

            BindItem = Item as HubScriptItem;
            DataContext = BindItem;

            SetTemplate();
        }

        private void SetTemplate()
        {
            CommentStory = new Storyboard();
            CommentStory.Completed += CommentStory_Completed;
        }

        private void Download( object sender, RoutedEventArgs e )
        {
            RuntimeCache RCache = new RuntimeCache();

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

        private async void ShowComments( object sender, RoutedEventArgs e )
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

                    MarkLoading();
                    HSCommentLoader CLoader = new HSCommentLoader( BindItem.Id, wenku8.Model.REST.SharersRequest.CommentTarget.SCRIPT );
                    IList<HSComment> FirstPage = await CLoader.NextPage();
                    MarkNotLoading();

                    CommentsSource = new Observables<HSComment, HSComment>( FirstPage );
                    CommentsSource.LoadStart += ( x, y ) => MarkLoading();
                    CommentsSource.LoadEnd += ( x, y ) => MarkNotLoading();

                    CommentsSource.ConnectLoader( CLoader );
                    CommentList.ItemsSource = CommentsSource;
                }
            }

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

        private void CommentList_ItemClick( object sender, ItemClickEventArgs e )
        {
            ( ( HSComment ) e.ClickedItem ).MarkSelect();
        }

        private void MarkLoading()
        {
            LoadingRing.IsActive = true;
            LoadingState.State = ControlState.Reovia;
        }

        private void MarkNotLoading()
        {
            LoadingRing.IsActive = false;
            LoadingState.State = ControlState.Foreatii;
        }

        private void PlaceKeyRequest( object sender, RoutedEventArgs e )
        {

        }
    }
}