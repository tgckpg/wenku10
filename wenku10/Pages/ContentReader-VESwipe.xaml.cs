using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using wenku8.Effects;

namespace wenku10.Pages
{
    sealed partial class ContentReader : Page
    {
        public enum ManiState { UP, NORMAL, DOWN }
        public ManiState CurrManiState = ManiState.NORMAL;

        private double ZoomTrigger = 0;
        private Storyboard ContentAway;

        private double MaxVT = double.PositiveInfinity;
        private double MinVT = double.NegativeInfinity;

        private double VT = 130;

        private Grid UpperBack { get { return IsHorz ? YUpperBack : XUpperBack; } }
        private Grid LowerBack { get { return IsHorz ? YLowerBack : XLowerBack; } }

        private bool AnyStoryActive
        {
            get
            {
                return new Storyboard[] {
                    ContentSlideBack, ContentSlideUp, ContentAway
                }.Any( x => x?.GetCurrentState() == ClockState.Active );
            }
        }

        private void SetSlideGesture()
        {
            ContentSlideBack.Completed += ( s, e ) => CurrManiState = ManiState.NORMAL;
            ContentSlideUp.Completed += ( s, e ) => CurrManiState = ManiState.UP;
            ContentSlideDown.Completed += ( s, e ) => CurrManiState = ManiState.DOWN;

            VESwipe.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY;
            VESwipe.ManipulationStarted += VEManiStart;
        }

        private void SetManiState()
        {
            CGTransform.TranslateX = CGTransform.TranslateY = 0;
            CGTransform.ScaleX = CGTransform.ScaleY = 1;
            ContentSlideBack.Stop();
            ContentSlideDown.Stop();
            ContentSlideUp.Stop();

            if ( IsHorz )
            {
                Storyboard.SetTargetProperty( SUAnimation, "TranslateY" );
                Storyboard.SetTargetProperty( SDAnimation, "TranslateY" );
                Storyboard.SetTargetProperty( SNAnimation, "TranslateY" );
            }
            else
            {
                Storyboard.SetTargetProperty( SUAnimation, "TranslateX" );
                Storyboard.SetTargetProperty( SDAnimation, "TranslateX" );
                Storyboard.SetTargetProperty( SNAnimation, "TranslateX" );
            }
        }

        private void VEManipulationEndX( object sender, ManipulationCompletedRoutedEventArgs e )
        {
            double dv = e.Cumulative.Translation.X.Clamp( MinVT, MaxVT );
            ContentAway?.Stop();
            if ( VT < dv )
            {
                ContentBeginAwayX( false );
            }
            else if ( dv < -VT )
            {
                ContentBeginAwayX( true );
            }
            else
            {
                ContentRestore.Begin();
            }
        }

        private void VEManipulationEndY( object sender, ManipulationCompletedRoutedEventArgs e )
        {
            double dv = e.Cumulative.Translation.Y.Clamp( MinVT, MaxVT );
            ContentAway?.Stop();
            if ( VT < dv )
            {
                ContentBeginAwayY( false );
            }
            else if ( dv < -VT )
            {
                ContentBeginAwayY( true );
            }
            else
            {
                ContentRestore.Begin();
            }
        }

        private void ContentBeginAwayX( bool Next )
        {
            ContentAway = new Storyboard();

            if ( Next )
            {
                SimpleStory.DoubleAnimation(
                    ContentAway, CGTransform, "TranslateX"
                    , CGTransform.TranslateX
                    , -MainSplitView.ActualWidth );

                StepNextTitle();
            }
            else
            {
                SimpleStory.DoubleAnimation(
                    ContentAway, CGTransform, "TranslateX"
                    , CGTransform.TranslateX
                    , MainSplitView.ActualWidth );

                StepPrevTitle();
            }

            ContentAway.Completed += ( s, e ) =>
            {
                ContentAway.Stop();
                CGTransform.TranslateX = -( double ) CGTransform.GetValue( CompositeTransform.TranslateXProperty );
                CGTransform.TranslateY = 0;
                ReaderSlideBack();
            };
            ContentAway.Begin();
        }

        private void ContentBeginAwayY( bool Next )
        {
            ContentAway = new Storyboard();

            if ( Next )
            {
                SimpleStory.DoubleAnimation(
                    ContentAway, CGTransform, "TranslateY"
                    , CGTransform.TranslateY
                    , -MainSplitView.ActualHeight );

                StepNextTitle();
            }
            else
            {
                SimpleStory.DoubleAnimation(
                    ContentAway, CGTransform, "TranslateY"
                    , CGTransform.TranslateY
                    , MainSplitView.ActualHeight );

                StepPrevTitle();
            }

            ContentAway.Completed += ( s, e ) =>
            {
                ContentAway.Stop();
                CGTransform.TranslateX = 0;
                CGTransform.TranslateY = -( double ) CGTransform.GetValue( CompositeTransform.TranslateYProperty );
                ReaderSlideBack();
            };
            ContentAway.Begin();
        }

        private void ContentBeginAway( bool Next )
        {
            if ( CurrManiState == ManiState.NORMAL ) return;

            if ( IsHorz ) ContentBeginAwayX( Next );
            else ContentBeginAwayY( Next );
        }

        private void StepPrevTitle()
        {
            if ( CurrManiState == ManiState.UP ) EpTitleStepper.Prev();
            else VolTitleStepper.Prev();
        }

        private void StepNextTitle()
        {
            if ( CurrManiState == ManiState.UP ) EpTitleStepper.Next();
            else VolTitleStepper.Next();
        }

        private void VEManiStart( object sender, ManipulationStartedRoutedEventArgs e )
        {
            CGTransform.SetValue( CompositeTransform.TranslateXProperty, CGTransform.GetValue( CompositeTransform.TranslateXProperty ) );
            CGTransform.SetValue( CompositeTransform.TranslateYProperty, CGTransform.GetValue( CompositeTransform.TranslateYProperty ) );
            ContentRestore.Stop();
        }

        private void VEZoomBackUpX( object sender, ManipulationDeltaRoutedEventArgs e )
        {
            VEZoomBackUp( e.Delta.Translation.X );
            VEZoomY( e.Delta.Translation.Y );
        }
        private void VEZoomBackUpY( object sender, ManipulationDeltaRoutedEventArgs e )
        {
            VEZoomX( e.Delta.Translation.X );
            VEZoomBackUp( e.Delta.Translation.Y );
        }
        private void VEZoomBackDownX( object sender, ManipulationDeltaRoutedEventArgs e )
        {
            VEZoomBackDown( e.Delta.Translation.X );
            VEZoomY( e.Delta.Translation.Y );
        }
        private void VEZoomBackDownY( object sender, ManipulationDeltaRoutedEventArgs e )
        {
            VEZoomBackDown( e.Delta.Translation.Y );
            VEZoomX( e.Delta.Translation.X );
        }

        private void VEZoomX( double dv ) { CGTransform.TranslateX += dv; }
        private void VEZoomY( double dv ) { CGTransform.TranslateY += dv; }

        private void VEZoomBackUp( double dv )
        {
            ZoomTrigger += dv;
            if ( 100 < ZoomTrigger ) ReaderSlideBack();
            else if ( ZoomTrigger < 0 ) ZoomTrigger = 0;
        }

        private void VEZoomBackDown( double dv )
        {
            ZoomTrigger += dv;
            if ( ZoomTrigger < -100 ) ReaderSlideBack();
            else if ( 0 < ZoomTrigger ) ZoomTrigger = 0;
        }

        private void StopZoom()
        {
            ZoomTrigger = 0;
            VESwipe.IsHitTestVisible = false;
            VESwipe.ManipulationDelta -= VEZoomBackUpX;
            VESwipe.ManipulationDelta -= VEZoomBackUpY;
            VESwipe.ManipulationDelta -= VEZoomBackDownX;
            VESwipe.ManipulationDelta -= VEZoomBackDownY;
            VESwipe.ManipulationCompleted -= VEManipulationEndX;
            VESwipe.ManipulationCompleted -= VEManipulationEndY;
        }

        private void StartZoom( bool Up )
        {
            ZoomTrigger = 0;
            VESwipe.IsHitTestVisible = true;

            CGTransform.TranslateX = CGTransform.TranslateY = 0;
            CGTransform.ScaleX = CGTransform.ScaleY = 1;
            ContentRestore.Stop();

            MaxVT = ( Up ? ES.PrevVolAvaible() : ES.PrevStepAvailable() ) ? double.PositiveInfinity : ( VT - 1 );
            MinVT = ( Up ? ES.NextVolAvaible() : ES.NextStepAvailable() ) ? double.NegativeInfinity : ( 1 - VT );

            if ( IsHorz )
            {
                Storyboard.SetTargetProperty( CRRestoreAni, "TranslateX" );
                VESwipe.ManipulationCompleted += VEManipulationEndX;

                if ( Up ) VESwipe.ManipulationDelta += VEZoomBackDownY;
                else VESwipe.ManipulationDelta += VEZoomBackUpY;
            }
            else
            {
                Storyboard.SetTargetProperty( CRRestoreAni, "TranslateY" );
                VESwipe.ManipulationCompleted += VEManipulationEndY;

                if ( Up ) VESwipe.ManipulationDelta += VEZoomBackDownX;
                else VESwipe.ManipulationDelta += VEZoomBackUpX;
            }
        }

        public void ReaderSlideBack()
        {
            if ( ContentSlideBack.GetCurrentState() != ClockState.Active )
            {
                StopZoom();
                ContentRestore.Begin();
                ContentSlideBack.Begin();
                TransitionDisplay.SetState( VolTitle, TransitionState.Inactive );
                TransitionDisplay.SetState( BookTitle, TransitionState.Inactive );
                TransitionDisplay.SetState( LowerBack, TransitionState.Inactive );
                TransitionDisplay.SetState( UpperBack, TransitionState.Inactive );

                // Compensate for Storyboard.Completed event not firing
                CurrManiState = ManiState.NORMAL;
            }
        }

        public void ReaderSlideUp()
        {
            if ( ContentSlideUp.GetCurrentState() != ClockState.Active )
            {
                StartZoom( false );
                ContentSlideUp.Begin();
                TransitionDisplay.SetState( VolTitle, TransitionState.Active );
                TransitionDisplay.SetState( BookTitle, TransitionState.Inactive );
                TransitionDisplay.SetState( LowerBack, TransitionState.Active );
                TransitionDisplay.SetState( UpperBack, TransitionState.Inactive );
            }
        }

        public void ReaderSlideDown()
        {
            if ( ContentSlideDown.GetCurrentState() != ClockState.Active )
            {
                StartZoom( true );
                ContentSlideDown.Begin();
                TransitionDisplay.SetState( VolTitle, TransitionState.Inactive );
                TransitionDisplay.SetState( BookTitle, TransitionState.Active );
                TransitionDisplay.SetState( LowerBack, TransitionState.Inactive );
                TransitionDisplay.SetState( UpperBack, TransitionState.Active );
            }
        }
    }
}
