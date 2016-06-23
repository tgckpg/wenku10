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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas.Effects;

using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Effects.P2DFlow.Spawners;
using wenku8.Effects.P2DFlow.Reapers;

namespace wenku10
{
    public sealed partial class ParticleField : UserControl
    {
        static Random Rand = new Random();

        private PFSimulator PFSim = new PFSimulator();
        private CanvasBitmap pNote;

        private bool ShowWireFrame = true;

        private PointerSpawner PtrSpawn;

        public ParticleField()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            PFSim.Create( 500 );

            PtrSpawn = new PointerSpawner() { SpawnTrait = PFTrait.TRAIL };
            Stage.PointerMoved += Stage_PointerMoved;
        }

        private void Stage_CreateResources( CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args )
        {
            args.TrackAsyncAction( LoadTextures( sender ).AsAsyncAction() );
        }

        private async Task LoadTextures( CanvasAnimatedControl CC )
        {
            pNote = await CanvasBitmap.LoadAsync( CC, "Assets/glitter.dds" );
            PBounds = pNote.Bounds;
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
                PFSim.Spawners.Add( new Trail() );

                PFSim.Spawners.Add( PtrSpawn );

                // Temporary disable this code, but open for global renaming under Visual Studio
                if ( false )
                {
                    PFSim.Spawners.Add( new ExplosionParticle() );
                    PFSim.Spawners.Add( new LinearSpawner( new Vector2( HSW, SH ), new Vector2( 0, 0 ), new Vector2( 50, -200 ) )
                    {
                        Chaos = new Vector2( 1, 0 ),
                        SpawnTrait = PFTrait.TRAIL | PFTrait.THRUST | PFTrait.EXPLODE
                    } );
                }

                PFSim.Fields.Clear();
                PFSim.AddField( GenericForce.EARTH_GRAVITY );
                PFSim.AddField( new Thrust() { EndTime = 40f } );
                // PFSim.AddField( new Wind() { A = new Vector2( 0, 0 ), B = new Vector2( 0, SH ), MaxDist = HSW } );
                // PFSim.AddField( new Wind() { A = new Vector2( SW, 0 ), B = new Vector2( SW, SH ), MaxDist = SW } );
            }
        }

        private void Stage_PointerMoved( object sender, PointerRoutedEventArgs e )
        {
            if ( e.Pointer.IsInContact )
            {
                PtrSpawn.FeedPosition( e.GetCurrentPoint( Stage ).Position.ToVector2() );
            }
        }

        private void Stage_Update( ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args )
        {

        }

        private Vector2 PCenter = new Vector2( 16, 16 );
        private Rect PBounds;
        private Vector2 PScale = Vector2.One;

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

                        Vector4 Tint = new Vector4(
                            P.Tint.M11 + P.Tint.M21 + P.Tint.M31 + P.Tint.M41 + P.Tint.M51,
                            P.Tint.M12 + P.Tint.M22 + P.Tint.M32 + P.Tint.M42 + P.Tint.M52,
                            P.Tint.M13 + P.Tint.M23 + P.Tint.M33 + P.Tint.M43 + P.Tint.M53,
                            P.Tint.M14 + P.Tint.M24 + P.Tint.M34 + P.Tint.M44 + P.Tint.M54
                        );

                        Tint.X *= 0.2f + 0.3f * ( 1 - A );
                        Tint.Y *= 0.2f + 0.3f * ( 1 - A );

                        Tint.W *= A;

                        SBatch.Draw( pNote, P.Pos, Tint, PCenter, 0, 0.5f * PScale * ( 1 + A % 0.5f ), CanvasSpriteFlip.None );
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
