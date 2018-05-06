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
		private Vector4 R0;
		private Vector4 R1;

		private bool InRange = false;

		private float _R;
		private float _OffsetR;
		private float PosX;
		private Vector2 PosY;

		public float Accelerate( float a )
		{
			PosX = R0.X + ( 1 + a ) * ( 0.5f * R0.W );
			InRange = ( PosX < R1.X || R1.X + R1.W < PosX );

			if ( InRange )
			{
				return a - _OffsetR;
			}
			else
			{
				return 0;
			}
		}

		public void Brake( float offset, float d )
		{
			_R = d;
			_OffsetR = offset;

			R1.W = R0.W * d;
			R1.Z = R0.Z;

			R1.X = R0.X + ( 1 + offset ) * ( 0.5f * ( R0.W - R1.W ) );
			R1.Y = R0.Y;
		}

		public void UpdateAssets( Size s )
		{
			float SW = ( float ) s.Width;

			R0.W = SW * 0.8f;
			R0.Z = 20.0f;
			R0.X = 0.5f * ( SW - R0.W );
			R0.Y = 40.0f;

			PosY = new Vector2( R0.Y, R0.Y + R0.Z );
			Brake( _OffsetR, _R );
		}

		public void Draw( CanvasDrawingSession ds, CanvasSpriteBatch SBatch, TextureLoader Textures )
		{
			if ( InRange )
			{
				ds.FillRectangle( R0.X, R0.Y, R0.W, R0.Z, LayoutSettings.RelativeMajorBackgroundColor );
				ds.FillRectangle( R1.X, R1.Y, R1.W, R1.Z, LayoutSettings.SubtleColor );
			}
			else
			{
				ds.FillRectangle( R0.X, R0.Y, R0.W, R0.Z, LayoutSettings.SubtleColor );
				ds.FillRectangle( R1.X, R1.Y, R1.W, R1.Z, LayoutSettings.RelativeMajorBackgroundColor );
			}

			Color PinMask = LayoutSettings.MajorColor;
			PinMask.A = 48;

			ds.DrawLine( PosX, PosY.X, PosX, PosY.Y, PinMask, 10 );
			ds.DrawLine( PosX, PosY.X, PosX, PosY.Y, LayoutSettings.MajorColor, 1 );
		}

		public void Enter() { }
		public void Dispose() { }
	}
}