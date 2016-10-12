using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Services.Store.Engagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;

using wenku8.Config;
using wenku8.Effects;
using wenku8.Effects.Stage;
using wenku8.Effects.Stage.RectangleParty;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.Spawners;
using wenku8.Effects.P2DFlow.Reapers;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Settings.Theme;
using wenku8.System;
using wenku8.ThemeIcons;

namespace wenku10.Pages
{
    public sealed partial class ModeSelect : Page
    {
        public static readonly string ID = typeof( ModeSelect ).Name;

        public bool ProtoUnLocked { get; private set; }

        private bool ModeSelected = false;
        Frame RootFrame;

        private Scenes.StartScreen PFScene;

        public ModeSelect()
        {
            InitializeComponent();
            NavigationHandler.InsertHandlerOnNavigatedBack( DisableBack );
            SetTemplate();
        }

        private void DisableBack( object sender, XBackRequestedEventArgs e )
        {
            e.Handled = true;
        }

        private void SinglePlayer( object sender, RoutedEventArgs e )
        {
            if ( ModeSelected ) return;
#if !DEBUG
            StoreServicesCustomEventLogger.GetDefault().Log( wenku8.System.ActionEvent.NORMAL_MODE );
#endif
            ModeSelected = true;
            PFScene.Fire();

            NavigationHandler.OnNavigatedBack -= DisableBack;

            var j = Dispatcher.RunIdleAsync( x => {
                MainStage.Instance.ClearNavigate( typeof( SuperGiants ), "" );
            } );
        }

        private void SetTemplate()
        {
            RootFrame = MainStage.Instance.RootFrame;

            Func<UIElement>[] RandIcon = new Func<UIElement>[]
            {
                () => new IconExoticHexa() {
                    Foreground = new SolidColorBrush( Properties.APPEARENCE_THEME_MINOR_COLOR )
                    , Background = new SolidColorBrush( Properties.APPEARENCE_THEME_MAJOR_BACKGROUND_COLOR )
                }
                , () => new IconExoticQuad() {
                    Foreground = new SolidColorBrush( Properties.APPEARENCE_THEME_MINOR_COLOR )
                    , Background = new SolidColorBrush( Properties.APPEARENCE_THEME_MAJOR_BACKGROUND_COLOR )
                }
                , () => new IconExoticTri() {
                    Foreground = new SolidColorBrush( Properties.APPEARENCE_THEME_MINOR_COLOR )
                    , Background = new SolidColorBrush( Properties.APPEARENCE_THEME_MAJOR_BACKGROUND_COLOR )
                }
            };

            SenseGround.Children.Add( NTimer.RandChoiceFromList( RandIcon ).Invoke() );

            if( MainStage.Instance.IsPhone )
            {
                InfoButtons.HorizontalAlignment = HorizontalAlignment.Left;
                InfoButtons.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else
            {
                InfoButtons.FlowDirection = FlowDirection.RightToLeft;
            }

            SetFeedbackButton();
            GetAnnouncements();

            PFScene = new Scenes.StartScreen( Stage );
            PFScene.BindXStage( XStage );
            PFScene.Unlock = StartMultiplayer;
            PFScene.Start();
        }

        private void SetFeedbackButton()
        {
            if ( StoreServicesFeedbackLauncher.IsSupported() )
            {
                feedbackButton.Visibility = Visibility.Visible;
            }
        }

        private async void StartMultiplayer()
        {
#if !DEBUG
            StoreServicesCustomEventLogger.GetDefault().Log( wenku8.System.ActionEvent.SECRET_MODE );
#endif
            TransitionDisplay.SetState( StartButton, TransitionState.Inactive );
            TransitionDisplay.SetState( ForeText, TransitionState.Active );

            LayoutRoot.Children.Remove( ForeText );

            MainStage.Instance.ObjectLayer.Children.Add( ForeText );

            await Task.Delay( 350 );

            CoverTheWholeScreen();
        }

        private void CoverTheWholeScreen()
        {
            RectWaltzPrelude RP = new RectWaltzPrelude( MainStage.Instance.Canvas );
            RP.SetParty();
            RP.OnComplete( PlayForAMoment );
            RP.Play();
        }

        private void PlayForAMoment()
        {
            RectWaltzInterlude RP = new RectWaltzInterlude( MainStage.Instance.Canvas );
            RP.SetParty();
            RP.Play();

            TextTransition TextTrans = new TextTransition( t => SenseText.Text = t );
            TextTrans.SetTransation( SenseText.Text, "wenku8" );
            TextTrans.Play();

            TextTrans.OnComplete( GoMultiplayer );
        }

        private void GoMultiplayer()
        {
            NavigationHandler.OnNavigatedBack -= DisableBack;
            MainStage.Instance.ClearNavigate( typeof( MainPage ), "" );

            var j = Dispatcher.RunIdleAsync( x => DimTheWholeScreen() );
        }

        private void DimTheWholeScreen()
        {
            ScreenColorTransform SC = new ScreenColorTransform( MainStage.Instance.Canvas );
            SC.SetScreen(
                Properties.APPEARENCE_THEME_MAJOR_COLOR
                , ThemeManager.StringColor( "#FF0F65C0" )
            );

            SC.Play();

            SC.OnComplete( () =>
            {
                SC = new ScreenColorTransform( MainStage.Instance.Canvas );
                SC.SetScreen(
                    ThemeManager.StringColor( "#FF0F65C0" )
                    , ThemeManager.StringColor( "#00000000" )
                );

                SC.DisposeOnComplete = true;
                SC.Play();

                Storyboard sb = ForeText.Resources[ "FadeOut" ] as Storyboard;
                sb.Begin();
                sb.Completed += ( s, e ) =>
                {
                    MainStage.Instance.ObjectLayer.Children.Remove( ForeText );
                };

                TextTransition TextTrans = new TextTransition( t => SenseText.Text = t );
                TextTrans.SetDuration( 100 );
                TextTrans.SetTransation( SenseText.Text, "     " );
                TextTrans.Play();
            } );
        }

        private async void GetAnnouncements()
        {
            global::wenku8.Model.Loaders.NewsLoader AS = new global::wenku8.Model.Loaders.NewsLoader();
            await AS.Load();

            NewsLoading.IsActive = false;
            if ( AS.HasNewThings )
            {
                Storyboard sb = ( Storyboard ) NotiRect.Resources[ "Notify" ];
                sb?.Begin();
            }
        }

        private void feedbackButton_Click( object sender, RoutedEventArgs e )
        {
            var j = StoreServicesFeedbackLauncher.GetDefault()?.LaunchAsync();
        }

        private void ShowNews( object sender, RoutedEventArgs e ) { ShowNews(); }

        private async void ShowNews()
        {
            Dialogs.Announcements NewsDialog = new Dialogs.Announcements();
            await Popups.ShowDialog( NewsDialog );

            Storyboard sb = ( Storyboard ) NotiRect.Resources[ "Notify" ];
            sb?.Stop();
        }


    }
}