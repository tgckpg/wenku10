using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Effects.P2DFlow.Reapers;
using wenku8.Effects.P2DFlow.Spawners;

namespace wenku10.Pages
{
    public sealed partial class About : Page
    {
        private PFSimulator PFSim = new PFSimulator();

        private bool ShowWireFrame = false;

        private TextureLoader Texture;

        private const int Texture_Glitter = 1;
        private const int Texture_Circle = 2;

        public About()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            PFSim.Create( 500 );

            Texture = new TextureLoader();

            Stage.Draw += Stage_Draw;
            Stage.SizeChanged += Stage_SizeChanged;
            Stage.Unloaded += Stage_Unloaded;
        }

        private void Stage_Unloaded( object sender, RoutedEventArgs e )
        {
            lock ( PFSim )
            {
                Stage.Draw -= Stage_Draw;
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
            lock ( PFSim )
            {
                Size s = e.NewSize;
                PFSim.Reapers.Clear();
                PFSim.Reapers.Add( Age.Instance );
                PFSim.Reapers.Add( new Boundary( new Rect( 0, 0, s.Width * 1.2, s.Height * 1.2 ) ) );

                float SW = ( float ) s.Width;
                float SH = ( float ) s.Height;
                float HSW = 0.5f * SW;

                PFSim.Spawners.Clear();
                PFSim.Spawners.Add( new Trail() { Texture = Texture_Glitter } );
                PFSim.Spawners.Add( new ExplosionParticle()
                {
                    Texture = Texture_Circle
                    , SpawnEx = ( P ) => { P.Scale *= 0.125f; }
                } );
                PFSim.Spawners.Add( new LinearSpawner( new Vector2( HSW, SH ), new Vector2( 0, 0 ), new Vector2( 50, -200 ) )
                {
                    Chaos = new Vector2( 1, 0 ), SpawnTrait = PFTrait.THRUST | PFTrait.EXPLODE | PFTrait.TRAIL
                    , Texture = Texture_Glitter
                    , SpawnEx = ( P ) => { P.Tint.M44 = 0; }
                } );

                PFSim.Fields.Clear();
                PFSim.AddField( GenericForce.EARTH_GRAVITY );
                PFSim.AddField( new Thrust() { EndTime = 40f } );
            }
        }

        private Vector2 PCenter = new Vector2( 16, 16 );

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

                        float A = ( P.Trait & PFTrait.IMMORTAL ) == 0 ? P.ttl * 0.033f : 1;

                        P.Tint.M12 = 4 * ( 1 - A );
                        P.Tint.M21 = 3 * A;

                        Vector4 Tint = new Vector4(
                            P.Tint.M11 + P.Tint.M21 + P.Tint.M31 + P.Tint.M41 + P.Tint.M51,
                            P.Tint.M12 + P.Tint.M22 + P.Tint.M32 + P.Tint.M42 + P.Tint.M52,
                            P.Tint.M13 + P.Tint.M23 + P.Tint.M33 + P.Tint.M43 + P.Tint.M53,
                            P.Tint.M14 + P.Tint.M24 + P.Tint.M34 + P.Tint.M44 + P.Tint.M54
                        ) * 2;

                        Tint.W *= A * 0.125f;

                        SBatch.Draw( Texture[ P.TextureId ], P.Pos, Tint, Texture.Center[ P.TextureId ], 0, P.Scale * A, CanvasSpriteFlip.None );
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

    }
}
