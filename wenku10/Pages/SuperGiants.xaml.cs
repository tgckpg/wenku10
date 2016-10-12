using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
using Windows.UI.Xaml.Navigation;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Effects.P2DFlow.Reapers;
using wenku8.Effects.P2DFlow.Spawners;

namespace wenku10.Pages
{
    public sealed partial class SuperGiants : Page
    {
        public SuperGiants()
        {
            this.InitializeComponent();
            SetBackground();
            NTimer.Instance.Start();
        }

        private void FloatyButton_Loaded( object sender, RoutedEventArgs e )
        {
            FloatyButton Floaty = ( ( FloatyButton ) sender );
            Floaty.BindTimer( NTimer.Instance );

            Floaty.TextSpeed = NTimer.RandDouble( -2, 2 );
        }

        #region Dynamic Background
        private PFSimulator PFSim = new PFSimulator();

#if DEBUG
        private bool ShowWireFrame = false;
#endif

        private TextureLoader Texture;

        private const int Texture_Circle = 2;

        private Vector4 ThemeTint;

        private Wind ScrollWind = new Wind();

        private void SetBackground()
        {
            PFSim.Create( MainStage.Instance.IsPhone ? 25 : 50 );

            Texture = new TextureLoader();

            Color C = Colors.White;
            ThemeTint = new Vector4( C.R * 0.0039f, C.G * 0.0039f, C.B * 0.0039f, C.A * 0.0039f );

            Stage.GameLoopStarting += Stage_GameLoopStarting;
            Stage.GameLoopStopped += Stage_GameLoopStopped;

            Stage.SizeChanged += Stage_SizeChanged;
            LayoutRoot.ViewChanged += LayoutRoot_ViewChanged;
        }

        private void Stage_GameLoopStopped( ICanvasAnimatedControl sender, object args )
        {
            Stage.Draw -= Stage_Draw;
        }

        private void Stage_GameLoopStarting( ICanvasAnimatedControl sender, object args )
        {
            Stage.Draw += Stage_Draw;
        }

        private float PrevOffset = 0;

        private void LayoutRoot_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
        {
            float CurrOffset = ( float ) LayoutRoot.VerticalOffset;
            ScrollWind.Strength = Vector2.Clamp( Vector2.One * ( CurrOffset - PrevOffset ), -3 * Vector2.One, 3 * Vector2.One ).X;
            PrevOffset = CurrOffset;
        }

        private void Stage_CreateResources( CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args )
        {
            args.TrackAsyncAction( LoadTextures( sender ).AsAsyncAction() );
        }

        private async Task LoadTextures( CanvasAnimatedControl CC )
        {
            await Texture.Load( CC, Texture_Circle, "Assets/circle.dds" );
        }

        private void Stage_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            lock ( PFSim )
            {
                Size s = e.NewSize;
                PFSim.Reapers.Clear();
                PFSim.Reapers.Add( Age.Instance );
                PFSim.Reapers.Add( new Boundary( new Rect( -0.1 * s.Width, -0.1 * s.Height, s.Width * 1.2, s.Height * 1.2 ) ) );

                float SW = ( float ) s.Width;
                float SH = ( float ) s.Height;
                float HSW = 0.5f * SW;
                float HSH = 0.5f * SH;

                PFSim.Spawners.Clear();
                PFSim.Spawners.Add( new LinearSpawner( new Vector2( HSW, SH * 4 / 5 ), new Vector2( HSW, HSH ), new Vector2( 10, 10 ) )
                {
                    Chaos = new Vector2( 1, 1 )
                    , otMin = 5, otMax = 10
                    , Texture = Texture_Circle
                    , SpawnEx = ( P ) =>
                    {
                        P.Tint.M11 = ThemeTint.X;
                        P.Tint.M22 = ThemeTint.Y;
                        P.Tint.M33 = ThemeTint.Z;
                        P.Tint.M44 = ThemeTint.W * NTimer.LFloat();
                        P.ttl = 500;

                        P.mf *= NTimer.LFloat();
                        P.Scale = new Vector2( 0.05f, 0.05f ) + Vector2.One * ( NTimer.LFloat() * 0.25f );
                    }
                } );

                ScrollWind.A = new Vector2( 0, SH );
                ScrollWind.B = new Vector2( SW, SH );
                ScrollWind.MaxDist = SH;

                PFSim.Fields.Clear();
                PFSim.AddField( ScrollWind );
            }
        }

        private void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args )
        {
            lock ( PFSim )
            {
                var Snapshot = PFSim.Snapshot();
                using ( CanvasDrawingSession ds = args.DrawingSession )
                using ( CanvasSpriteBatch SBatch = ds.CreateSpriteBatch() )
                {
                    while ( Snapshot.MoveNext() )
                    {
                        Particle P = Snapshot.Current;

                        float A = -Vector2.Transform( new Vector2( 0, 1 ), Matrix3x2.CreateRotation( 3.1415f * P.ttl * 0.002f ) ).X;

                        Vector4 Tint = new Vector4(
                            P.Tint.M11 + P.Tint.M21 + P.Tint.M31 + P.Tint.M41 + P.Tint.M51,
                            P.Tint.M12 + P.Tint.M22 + P.Tint.M32 + P.Tint.M42 + P.Tint.M52,
                            P.Tint.M13 + P.Tint.M23 + P.Tint.M33 + P.Tint.M43 + P.Tint.M53,
                            P.Tint.M14 + P.Tint.M24 + P.Tint.M34 + P.Tint.M44 + P.Tint.M54
                        );

                        Tint.W *= A;
                        ScrollWind.Strength *= 0.5f;

                        SBatch.Draw(
                            Texture[ P.TextureId ]
                            , P.Pos, Tint
                            , Texture.Center[ P.TextureId ], 0, P.Scale
                            , CanvasSpriteFlip.None );
                    }
#if DEBUG
                    if ( ShowWireFrame )
                    {
                        foreach ( IForceField IFF in PFSim.Fields )
                        {
                            IFF.WireFrame( ds );
                        }
                    }
#endif
                }
            }
        }
        #endregion
    }
}