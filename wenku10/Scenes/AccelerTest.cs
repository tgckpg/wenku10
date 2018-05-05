using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

using Microsoft.Graphics.Canvas;

using GR.Effects;
using GR.Resources;

namespace wenku10.Scenes
{
	sealed class AccelerTest : IScene
	{
		private Rect[] Meter;

		private float PosX;
		private float CurrentA;
		private float _StopRange;
		private Vector2 PosY;

		private Func<float, float> PosXMultiplier = x => x;

		public AccelerTest()
		{
		}

		public float Accelerate( float a )
		{
			if ( _StopRange < Math.Abs( a ) )
			{
				CurrentA = a;
			}
			else
			{
				CurrentA = 0;
			}

			PosX = PosXMultiplier( a );
			return CurrentA;
		}

		public void StopRange( float d )
		{
			_StopRange = d;

			if ( Meter != null )
			{
				Rect R0 = Meter[ 0 ];
				Rect R1 = new Rect
				{
					Width = R0.Width * d,
					Height = R0.Height
				};

				R1.X = R0.X + 0.5 * ( R0.Width - R1.Width );
				R1.Y = R0.Y;
				Meter[ 1 ] = R1;

				Accelerate( CurrentA );
			}
		}

		public void UpdateAssets( Size s )
		{
			float SW = ( float ) s.Width;
			float SH = ( float ) s.Height;
			float HSW = 0.5f * SW;
			float HSH = 0.5f * SH;

			Rect R0 = new Rect
			{
				Y = 40,
				Width = SW * 0.8
			};
			R0.X = 0.5f * ( SW - R0.Width );
			R0.Height = 20;

			Meter = new Rect[] { R0, new Rect() };

			float RW = SW * 0.8f * 0.5f;
			PosXMultiplier = ( v ) => RW + v * RW + ( float ) R0.X;
			PosY = new Vector2( ( float ) R0.Y, ( float ) ( R0.Y + R0.Height ) );

			StopRange( _StopRange );
		}

		public void Draw( CanvasDrawingSession ds, CanvasSpriteBatch SBatch, TextureLoader Textures )
		{
			if ( CurrentA == 0 )
			{
				ds.FillRectangle( Meter[ 0 ], LayoutSettings.SubtleColor );
				ds.FillRectangle( Meter[ 1 ], LayoutSettings.RelativeMajorBackgroundColor );
			}
			else
			{
				ds.FillRectangle( Meter[ 0 ], LayoutSettings.RelativeMajorBackgroundColor );
				ds.FillRectangle( Meter[ 1 ], LayoutSettings.SubtleColor );
			}

			Color PinMask = LayoutSettings.MajorColor;
			PinMask.A = 96;

			ds.DrawLine( new Vector2( PosX, PosY.X ), new Vector2( PosX, PosY.Y ), PinMask, 10 );
			ds.DrawLine( new Vector2( PosX, PosY.X ), new Vector2( PosX, PosY.Y ), LayoutSettings.MajorColor, 1 );
		}

		public void Enter() { }
		public void Dispose() { }
	}
}