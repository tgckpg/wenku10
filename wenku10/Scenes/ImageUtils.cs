using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

using Microsoft.Graphics.Canvas;

namespace wenku10.Scenes
{
	static class ImageUtils
	{
		public static (Rect, Rect) FitImage( Size StageSize, CanvasBitmap SrcBmp )
		{
			float DestWidth = ( float ) StageSize.Width;
			float DestHeight = ( float ) StageSize.Height;

			Rect StageRect = new Rect( 0, 0, DestWidth, DestHeight );
			Rect FillRect;

			float SrcWidth = SrcBmp.SizeInPixels.Width;
			float SrcHeight = SrcBmp.SizeInPixels.Height;

			float SrcRatio = SrcHeight / SrcWidth;
			float DestRatio = DestHeight / DestWidth;

			if ( DestRatio < SrcRatio )
			{
				FillRect = new Rect( 0, 0, SrcWidth, SrcWidth * DestHeight / DestWidth );
			}
			else if ( SrcRatio < DestRatio )
			{
				FillRect = new Rect( 0, 0, SrcHeight * DestWidth / DestHeight, SrcHeight );
			}
			else
			{
				FillRect = StageRect;
			}

			return (StageRect, FillRect);
		}
	}
}