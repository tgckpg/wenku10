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
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace wenku10.Scenes
{
	sealed class TheOrb : PFScene, ITextureScene, ISceneExitable
	{
		private bool Left = true;
		private bool Exited = false;

		private float SW = 100.0f;
		private float SH = 100.0f;

		private Vector2 Center;
		private Vector4 OrbTint;

		private int tCircle;

		public TheOrb( Stack<Particle> ParticleQueue, bool Left )
		{
			this.Left = Left;

			SetColor();
			PFSim.Create( ParticleQueue );
		}
		private void SetColor()
		{
			Color MColor = wenku8.Resources.LayoutSettings.MajorColor;

			OrbTint.X = MColor.R / 255f;
			OrbTint.Y = MColor.G / 255f;
			OrbTint.Z = MColor.B / 255f;
			OrbTint.W = MColor.A / 255f;
		}

		public async Task LoadTextures( CanvasAnimatedControl Canvas, TextureLoader Textures )
		{
			tCircle = await Textures.Load( Canvas, Texture.Glitter, "Assets/circle.dds" );
		}

		public void UpdateAssets( Size s )
		{
			if ( Exited ) return;

			lock ( PFSim )
			{
				PFSim.Reapers.Clear();
				PFSim.Reapers.Add( Age.Instance );
				PFSim.Reapers.Add( new Boundary( new Rect( 0, 0, s.Width * 1.2, s.Height * 1.2 ) ) );

				SW = ( float ) s.Width;
				SH = ( float ) s.Height;
				float HSW = 0.5f * SW;
				float HSH = 0.5f * SH;

				Center = new Vector2( Left ? HSH : ( SW - HSH ), HSH );

				LinearSpawner OrbAura = new LinearSpawner( Center, Vector2.One, Vector2.One )
				{
					SpawnTrait = PFTrait.TRAIL
					, ttl = 10
				};

				LinearSpawner Mantra = new LinearSpawner( Center, 30 * Vector2.One, 30 * Vector2.One )
				{
					Texture = tCircle
					, ttl = 30
					, spf = 3
					, SpawnEx = P =>
					{
						P.mf = 1;
						P.Scale = new Vector2( 0.125f, 0.125f );
					}
				};

				PFSim.Spawners.Clear();
				PFSim.Spawners.Add( new Trail() { mf = 0f, Texture = tCircle, Scale = new Vector2( 0.125f, 0.125f ) } );
				PFSim.Spawners.Add( OrbAura );
				PFSim.Spawners.Add( Mantra );

				PFSim.Fields.Clear();
				PFSim.AddField( new Wind() { A = Center, B = Center, Strength = -15f } );
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

					Vector4 Tint = OrbTint;
					Tint.W = A;

					SBatch.Draw( Textures[ P.TextureId ], P.Pos, Tint, Textures.Center[ P.TextureId ], 0, 0.5f * P.Scale * ( 1 + A % 0.5f ), CanvasSpriteFlip.None );
				}

				DrawWireFrames( ds );
			}
		}

		public async Task Exit()
		{
			lock( PFSim )
			{
				Exited = true;
				PFSim.Spawners.Clear();
				PFSim.Fields.Clear();

				PFSim.AddField( new Wind() { A = Center, B = Center, MaxDist = 10, Strength = 50f } );
			}

			await Task.Delay( 1000 );
		}
	}
}