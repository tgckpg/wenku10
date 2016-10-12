using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

using Net.Astropenguin.Logging;

using wenku8.Config;
using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Effects.P2DFlow.Reapers;
using wenku8.Effects.P2DFlow.Spawners;
using wenku8.Settings.Theme;

namespace wenku10.Scenes
{
    sealed class StartScreen : BasicScene
    {
        public static readonly string ID = typeof( StartScreen ).Name;

        private PointerSpawner PtrSpawn;
        private Wind LoadingWind;
        private CyclicSp CountDown;

        private Vector4 LightFactor = Vector4.One;

        enum GestureDir { UP, DOWN, LEFT, RIGHT };

        public Action Unlock;
        private int GesTimes = 0;

        public StartScreen( CanvasAnimatedControl Stage ) : base( Stage ) { }

        override public void Start()
        {
            PFSim.Create( 500 );

            PtrSpawn = new PointerSpawner() { SpawnTrait = PFTrait.TRAIL_O, Texture = Texture_Circle };
            CountDown = new CyclicSp() { Texture = Texture_Glitter };

            ColorItem CItem = new ColorItem( "NaN", Properties.APPEARENCE_THEME_MAJOR_BACKGROUND_COLOR );
            Logger.Log( ID, "Theme lightness: " + CItem.L );

            if ( 50 < CItem.L ) LightFactor = new Vector4( 0.092f, 0.005f, 0.001f, 2 );

            Stage.PointerMoved += Stage_PointerMoved;
            Stage.PointerReleased += Stage_PointerReleased;
        }

        public void BindXStage( FrameworkElement e )
        {
            e.PointerPressed += XStage_PointerPressed;
        }

        public void Fire()
        {
            lock ( PFSim )
            {
                PFSim.AddField( LoadingWind );
            }
        }

        override protected void Stage_Unloaded( object sender, RoutedEventArgs e )
        {
            base.Stage_Unloaded( sender, e );

            lock( PFSim )
            {
                Stage.PointerMoved -= Stage_PointerMoved;
                Stage.PointerReleased -= Stage_PointerReleased;
            }
        }

        override protected void Stage_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            lock( PFSim )
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
                    Unlock?.Invoke();
                }
            }
        }

        private async void Pulsate()
        {
            lock( PFSim ) PFSim.AddField( LoadingWind );
            await Task.Delay( 100 );
            lock( PFSim ) PFSim.Fields.Remove( LoadingWind );
        }

        override protected void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args )
        {
            lock( PFSim )
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

                        SBatch.Draw( Textures[ P.TextureId ], P.Pos, Tint, Textures.Center[ P.TextureId ], 0, 0.5f * P.Scale * ( 1 + A % 0.5f ), CanvasSpriteFlip.None );
                    }

                    DrawWireFrames( ds );
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

        private class ExWind : Wind
        {
            override public void Apply( Particle P )
            {
                if ( NTimer.P( 120 ) )
                    base.Apply( P );
            }
        }

    }
}