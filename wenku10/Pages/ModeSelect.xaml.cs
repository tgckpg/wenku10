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
            PFSim.AddField( LoadingWind );

            NavigationHandler.OnNavigatedBack -= DisableBack;

            var j = Dispatcher.RunIdleAsync( x => {
                MainStage.Instance.ClearNavigate( typeof( LocalModeTxtList ), "" );
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
            SetPField();
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

        private object PFLock = new object();
        private PFSimulator PFSim = new PFSimulator();
        private TextureLoader Texture = new TextureLoader();
        private int Texture_Glitter = 1;
        private int Texture_Circle = 2;

        private bool ShowWireFrame = false;

        private PointerSpawner PtrSpawn;
        private Wind LoadingWind;
        private CyclicSp CountDown;

        private Vector4 LightFactor = Vector4.One;

        enum GestureDir { UP, DOWN, LEFT, RIGHT };
        private int GesTimes = 0;

        private void SetPField()
        {
            PFSim.Create( 500 );

            PtrSpawn = new PointerSpawner() { SpawnTrait = PFTrait.TRAIL_O, Texture = Texture_Circle };
            CountDown = new CyclicSp() { Texture = Texture_Glitter };

            ColorItem CItem = new ColorItem( "NaN", Properties.APPEARENCE_THEME_MAJOR_BACKGROUND_COLOR );
            Logger.Log( ID, "Theme lightness: " + CItem.L );

            if ( 50 < CItem.L ) LightFactor = new Vector4( 0.092f, 0.005f, 0.001f, 2 );

            Stage.Draw += Stage_Draw;
            Stage.PointerMoved += Stage_PointerMoved;
            Stage.PointerReleased += Stage_PointerReleased;
            Stage.Unloaded += Stage_Unloaded;
            Stage.SizeChanged += Stage_SizeChanged;
        }

        private void Stage_Unloaded( object sender, RoutedEventArgs e )
        {
            lock( PFLock )
            {
                Stage.Draw -= Stage_Draw;
                Stage.PointerMoved -= Stage_PointerMoved;
                Stage.PointerReleased -= Stage_PointerReleased;
                Stage.SizeChanged -= Stage_SizeChanged;
                Texture.Dispose();
                PFSim.Reapers.Clear();
                PFSim.Fields.Clear();
                PFSim.Spawners.Clear();
            }
        }

        private void Stage_CreateResources( CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args )
        {
            args.TrackAsyncAction( LoadTextures( sender ).AsAsyncAction() );
        }

        private async Task LoadTextures( CanvasAnimatedControl CC )
        {
            await Texture.Load( CC, Texture_Glitter, "Assets/glitter.dds" );
            await Texture.Load( CC, Texture_Circle, "Assets/circle.dds" );
        }

        private void Stage_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            lock( PFLock )
            {
                Size s = e.NewSize;
                PFSim.Reapers.Clear();
                PFSim.Reapers.Add( Age.Instance );
                PFSim.Reapers.Add( new Boundary( new Rect( 0, 0, s.Width * 1.2, s.Height * 1.2 ) ) );

                float SW = ( float ) s.Width;
                float SH = ( float ) s.Height;
                float HSW = 0.5f * SW;
                float HSH = 0.5f * SH;

                PFSim.Spawners.Clear();
                PFSim.Spawners.Add( new Trail() { mf = 1f, Texture = Texture_Glitter } );
                PFSim.Spawners.Add( new Trail() { mf = 1f, Texture = Texture_Circle, Bind = PFTrait.TRAIL_O, Scale = new Vector2( 0.125f, 0.125f ) } );

                CountDown.Center = new Vector2( HSW, HSH );
                PFSim.Spawners.Add( CountDown );
                PFSim.Spawners.Add( PtrSpawn );

                Vector2 Center = new Vector2( HSW, HSH );
                PFSim.Fields.Clear();
                PFSim.Fields.Add( new ExWind() { A = Center, B = Center, MaxDist = 200 } );

                LoadingWind = new Wind() { A = Center, B = Center, Strength = 30 };
            }
        }

        private class ExWind : Wind
        {
            override public void Apply( Particle P )
            {
                if ( NTimer.P( 120 ) )
                    base.Apply( P );
            }
        }

        private void Stage_PointerMoved( object sender, PointerRoutedEventArgs e )
        {
            if ( e.Pointer.IsInContact )
            {
                PtrSpawn.FeedPosition( e.GetCurrentPoint( Stage ).Position.ToVector2() );
            }
        }

        private Vector2 StartPoint;

        private void XStage_PointerPressed( object sender, PointerRoutedEventArgs e )
        {
            Stage.IsHitTestVisible = true;
            StartPoint = e.GetCurrentPoint( Stage ).Position.ToVector2();
        }

        private void Stage_PointerReleased( object sender, PointerRoutedEventArgs e )
        {
            Stage.IsHitTestVisible = false;
            GestureDirection( e.GetCurrentPoint( Stage ).Position.ToVector2() );
        }

        private void GestureDirection( Vector2 EndPoint )
        {
            Vector2 PosDiff = EndPoint - StartPoint;
            Vector2 AbsDiff = Vector2.Abs( PosDiff );

            bool Horizontal = AbsDiff.Y < AbsDiff.X;
            bool PositiveDir = Horizontal ? ( 0 < PosDiff.X ) : ( 0 < PosDiff.Y );

            GestureDir Dir;
            if ( Horizontal )
            {
                if ( AbsDiff.X < 30 ) return;

                Dir = PositiveDir ? GestureDir.RIGHT : GestureDir.LEFT;
            }
            else
            {
                if ( AbsDiff.Y < 30 ) return;

                Dir = PositiveDir ? GestureDir.DOWN : GestureDir.UP;
            }

            Pulsate();

            if( Dir == GestureDir.UP )
            {
                if ( 3 < GesTimes++ )
                {
                    GesTimes = int.MinValue;
                    PFSim.Spawners.Clear();
                    PFSim.Fields.Add( LoadingWind );
                    StartMultiplayer();
                }
            }
        }

        private async void Pulsate()
        {
            lock( PFLock ) PFSim.AddField( LoadingWind );
            await Task.Delay( 100 );
            lock( PFLock ) PFSim.Fields.Remove( LoadingWind );
        }

        private void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args )
        {
            lock( PFLock )
            {
                var Snapshot = PFSim.Snapshot();
                using ( CanvasDrawingSession ds = args.DrawingSession )
                using ( CanvasSpriteBatch SBatch = ds.CreateSpriteBatch() )
                {
                    while ( Snapshot.MoveNext() )
                    {
                        Particle P = Snapshot.Current;

                        float A = ( P.Trait & PFTrait.IMMORTAL ) == 0 ? P.ttl * 0.033f : 1;

                        P.Tint.M12 = 4 * ( 1 - A );
                        P.Tint.M21 = 3 * A;

                        Vector4 Tint = new Vector4(
                            P.Tint.M11 + P.Tint.M21 + P.Tint.M31 + P.Tint.M41 + P.Tint.M51,
                            P.Tint.M12 + P.Tint.M22 + P.Tint.M32 + P.Tint.M42 + P.Tint.M52,
                            P.Tint.M13 + P.Tint.M23 + P.Tint.M33 + P.Tint.M43 + P.Tint.M53,
                            P.Tint.M14 + P.Tint.M24 + P.Tint.M34 + P.Tint.M44 + P.Tint.M54
                        ) * 2 * LightFactor;

                        Tint.W = A * 0.125f;

                        SBatch.Draw( Texture[ P.TextureId ], P.Pos, Tint, Texture.Center[ P.TextureId ], 0, 0.5f * P.Scale * ( 1 + A % 0.5f ), CanvasSpriteFlip.None );
                    }

                    if ( ShowWireFrame )
                    {
                        foreach ( IForceField IFF in PFSim.Fields )
                        {
                            IFF.WireFrame( ds );
                        }
                    }
                }
            }
        }

        private class CyclicSp : ISpawner
        {
            public Vector2 Center;
            public float R = 120;
            public int Num = 6;
            private float MR = 2500;

            public int Texture;

            public CyclicSp() { }

            public int Acquire( int Quota )
            {
                return Num;
            }

            public void Prepare( IEnumerable<Particle> currParticles )
            {
            }

            public void Spawn( Particle p )
            {
                p.Pos = Vector2.Transform(
                    Center - new Vector2( 0, MR )
                    , Matrix3x2.CreateRotation( NTimer.RFloat() * 6.2832f, Center )
                );

                p.Trait = PFTrait.TRAIL;
                p.ttl = 2;
                p.TextureId = Texture;

                MR = 0.965f * MR + 0.035f * R;
            }
        }

    }
}