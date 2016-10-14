using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;

using Microsoft.Graphics.Canvas;

using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Effects.P2DFlow.Reapers;
using wenku8.Effects.P2DFlow.Spawners;

namespace wenku10.Scenes
{
    sealed class MagicTrails : PFScene, IScene
    {
        private LinearSpawner Spawner;

        public MagicTrails( Stack<Particle> ParticleQueue )
        {
            ShowWireFrame = true;

            PFSim.Create( ParticleQueue );
            Spawner = new LinearSpawner( Vector2.Zero, Vector2.Zero, new Vector2( 100, 100 ) )
            {
                SpawnTrait = PFTrait.TRAIL_O | PFTrait.IMMORTAL
                , Texture = 1
            };
        }

        public void UpdateAssets( Size s )
        {
            lock ( PFSim )
            {
                PFSim.Reapers.Clear();
                PFSim.Reapers.Add( Age.Instance );
                PFSim.Reapers.Add( new Boundary( new Rect( 0, 0, s.Width * 1.2, s.Height * 1.2 ) ) );

                float SW = ( float ) s.Width;
                float SH = ( float ) s.Height;
                float HSW = 0.5f * SW;
                float HSH = 0.5f * SH;

                PFSim.Spawners.Clear();
                // PFSim.Spawners.Add( new Trail() { mf = 0f, Texture = Texture.Circle, Bind = PFTrait.TRAIL_O, Scale = new Vector2( 0.125f, 0.125f ) } );

                PFSim.Spawners.Add( Spawner );

                Vector2 Center = new Vector2( HSW, HSH );
                PFSim.Fields.Clear();
                PFSim.Fields.Add( GenericForce.EARTH_GRAVITY );
                // PFSim.Fields.Add( new Wind() { A = new Vector2( 0, SH ), B = new Vector2( SW, 1.5f * SH ) } );

                PFSim.Fields.Add( new Vortex( 100, 150 ) { Center = new Vector2( HSH, HSH ) } );
            }
        }

        public void Draw( CanvasDrawingSession ds, CanvasSpriteBatch SBatch, TextureLoader Textures )
        {
            lock ( PFSim )
            {
                var Snapshot = PFSim.Snapshot();
                while ( Snapshot.MoveNext() )
                {
                    Particle P = Snapshot.Current;
                    if ( P.TextureId == 0 ) continue;

                    float A = ( P.Trait & PFTrait.IMMORTAL ) == 0 ? P.ttl * 0.033f : 1;

                    P.Tint.M12 = 4 * ( 1 - A );
                    P.Tint.M21 = 3 * A;

                    Vector4 Tint = new Vector4(
                        P.Tint.M11 + P.Tint.M21 + P.Tint.M31 + P.Tint.M41 + P.Tint.M51,
                        P.Tint.M12 + P.Tint.M22 + P.Tint.M32 + P.Tint.M42 + P.Tint.M52,
                        P.Tint.M13 + P.Tint.M23 + P.Tint.M33 + P.Tint.M43 + P.Tint.M53,
                        P.Tint.M14 + P.Tint.M24 + P.Tint.M34 + P.Tint.M44 + P.Tint.M54
                    ) * 2;

                    Tint.W = A * 0.125f;

                    SBatch.Draw( Textures[ P.TextureId ], P.Pos, Tint, Textures.Center[ P.TextureId ], 0, 0.5f * P.Scale * ( 1 + A % 0.5f ), CanvasSpriteFlip.None );
                }

                DrawWireFrames( ds );
            }
        }

    }
}