using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

using Microsoft.Graphics.Canvas;

using wenku8.Config;
using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Effects.P2DFlow.Reapers;
using wenku8.Effects.P2DFlow.Spawners;

namespace wenku10.Scenes
{
	sealed class Glitter : PFScene, IScene
	{
		private Vector4 ThemeTint;

		private Wind ScrollWind = new Wind();

		public Glitter()
		{
			Color C = Properties.APPEARENCE_THEME_HORIZONTAL_RIBBON_COLOR;
			ThemeTint = new Vector4( C.R * 0.0039f, C.G * 0.0039f, C.B * 0.0039f, C.A * 0.0039f );

			PFSim.Create( MainStage.Instance.IsPhone ? 25 : 50 );
		}

		public void WindBlow( float Strength )
		{
			ScrollWind.Strength = Vector2.Clamp( Vector2.One * Strength, -3 * Vector2.One, 3 * Vector2.One ).X;
		}

		public void UpdateAssets( Size s )
		{
			lock ( PFSim )
			{
				PFSim.Reapers.Clear();
				PFSim.Reapers.Add( Age.Instance );
				PFSim.Reapers.Add( new Boundary( new Rect( -0.1 * s.Width, -0.1 * s.Height, s.Width * 1.2, s.Height * 1.2 ) ) );

				float SW = ( float ) s.Width;
				float SH = ( float ) s.Height;
				float HSW = 0.5f * SW;
				float HSH = 0.5f * SH;

				PFSim.Spawners.Clear();
				PFSim.Spawners.Add( new LinearSpawner( new Vector2( HSW, HSH ), new Vector2( HSW, HSH ), new Vector2( 10, 10 ) )
				{
					Chaos = new Vector2( 1, 1 )
					, otMin = 5, otMax = 10
					, Texture = Texture.Circle
					, SpawnTrait = PFTrait.IMMORTAL
					, SpawnEx = ( P ) =>
					{
						P.Tint.M11 = ThemeTint.X;
						P.Tint.M22 = ThemeTint.Y;
						P.Tint.M33 = ThemeTint.Z;
						P.Tint.M44 = ThemeTint.W * NTimer.LFloat();

						P.mf *= NTimer.LFloat();
						P.Scale = new Vector2( 0.5f, 0.5f ) + Vector2.One * ( NTimer.LFloat() - 0.25f );
						P.vt.Y += 5 * NTimer.RFloat();
					}
				} );

				ScrollWind.A = new Vector2( SW, 0 );
				ScrollWind.B = new Vector2( SW, SH );
				ScrollWind.MaxDist = SW;

				PFSim.Fields.Clear();
				PFSim.AddField( GenericForce.EARTH_GRAVITY );
				PFSim.AddField( ScrollWind );
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

					float A = Vector2.Transform( new Vector2( 0, 1 ), Matrix3x2.CreateRotation( P.ttl * 0.01f ) ).X;

					Vector4 Tint = new Vector4(
						P.Tint.M11 + P.Tint.M21 + P.Tint.M31 + P.Tint.M41 + P.Tint.M51,
						P.Tint.M12 + P.Tint.M22 + P.Tint.M32 + P.Tint.M42 + P.Tint.M52,
						P.Tint.M13 + P.Tint.M23 + P.Tint.M33 + P.Tint.M43 + P.Tint.M53,
						P.Tint.M14 + P.Tint.M24 + P.Tint.M34 + P.Tint.M44 + P.Tint.M54
					);

					Tint.W *= A;
					ScrollWind.Strength *= 0.5f;

					SBatch.Draw(
						Textures[ P.TextureId ]
						, P.Pos, Tint
						, Textures.Center[ P.TextureId ], 0, P.Scale
						, CanvasSpriteFlip.None );
				}

				DrawWireFrames( ds );
			}
		}

	}
}