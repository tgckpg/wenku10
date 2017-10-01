﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;

namespace wenku10.Scenes
{
	sealed class CircleTextRenderer : ICanvasTextRenderer
	{
		public float Offset = 0;

		private CanvasDrawingSession ds;
		private ICanvasBrush Brush;
		private float[] TextWidths;
		private float R;

		private Vector2 Origin;

		public CircleTextRenderer( Vector2 Origin, float R, float[] TextWidths, CanvasDrawingSession ds, ICanvasBrush Brush )
		{
			this.R = R;
			this.Origin = Origin;
			this.TextWidths = TextWidths;
			this.ds = ds;
			this.Brush = Brush;

			PTextW = TextWidths[ 0 ];
		}

		public float Dpi => 96;
		public bool PixelSnappingDisabled => true;
		public Matrix3x2 Transform => Matrix3x2.Identity;

		float MovingRad = 0;
		float PTextW;

		public void DrawGlyphRun( Vector2 point, CanvasFontFace fontFace, float fontSize, CanvasGlyph[] glyphs, bool isSideways, uint bidiLevel, object brush, CanvasTextMeasuringMode measuringMode, string localeName, string textString, int[] clusterMapIndices, uint characterIndex, CanvasGlyphOrientation glyphOrientation )
		{
			Matrix3x2 OTrans = ds.Transform;

			int i = 0;
			if ( characterIndex == 0 )
			{
				ds.Transform = Matrix3x2.CreateTranslation( new Vector2( -0.5f * PTextW, -R ) ) * Matrix3x2.CreateRotation( Offset, Origin );
				ds.DrawGlyphRun( Origin, fontFace, fontSize, new CanvasGlyph[] { glyphs[ 0 ] }, isSideways, bidiLevel, Brush );

				MovingRad = 0;
				i++;
			}

			while ( i < glyphs.Length )
			{
				CanvasGlyph[] G = new CanvasGlyph[] { glyphs[ i ] };

				float TextW = TextWidths[ i + characterIndex ];
				float Rad = TextW / R;
				float OffsetRad = 0.5f * ( PTextW + TextW ) / R + MovingRad;

				ds.Transform = Matrix3x2.CreateTranslation( new Vector2( -0.5f * TextW, -R ) ) * Matrix3x2.CreateRotation( OffsetRad + Offset, Origin );
				ds.DrawGlyphRun( Origin, fontFace, fontSize, G, isSideways, bidiLevel, Brush );

				MovingRad += Rad;
				i++;
			}

			ds.Transform = OTrans;
		}

		public void DrawStrikethrough( Vector2 point, float strikethroughWidth, float strikethroughThickness, float strikethroughOffset, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation )
		{
			throw new NotImplementedException();
		}

		public void DrawUnderline( Vector2 point, float underlineWidth, float underlineThickness, float underlineOffset, float runHeight, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation )
		{
			throw new NotImplementedException();
		}

		public void DrawInlineObject( Vector2 point, ICanvasTextInlineObject inlineObject, bool isSideways, bool isRightToLeft, object brush, CanvasGlyphOrientation glyphOrientation )
		{
			throw new NotImplementedException();
		}
	}
}