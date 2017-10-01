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
		public static ( Rect, Rect ) FitImage( Size StageSize, CanvasBitmap SrcBmp )
		{
			float DestWidth = ( float ) StageSize.Width;
			float DestHeight = ( float ) StageSize.Height;

			Rect StageRect = new Rect( 0, 0, DestWidth, DestHeight );
			Rect FillRect;

			float SrcWidth = SrcBmp.SizeInPixels.Width;
			float SrcHeight = SrcBmp.SizeInPixels.Height;

			float SrcRatio = SrcWidth / SrcHeight;
			float DestRatio = DestWidth / DestHeight;

			if ( DestRatio < SrcRatio )
			{
				// Scale the Height
				if ( DestHeight < SrcHeight )
				{
					FillRect = new Rect( 0, 0, DestWidth * SrcHeight / DestHeight, SrcHeight );
				}
				else
				{
					FillRect = new Rect( 0, 0, DestWidth * SrcHeight / DestHeight, DestHeight );
				}
			}
			else if ( SrcRatio < DestRatio )
			{
				// Scale the Width
				if ( DestWidth < SrcWidth )
				{
					FillRect = new Rect( 0, 0, SrcWidth, DestHeight * SrcWidth / DestWidth );
				}
				else
				{
					FillRect = new Rect( 0, 0, DestWidth, DestHeight * SrcWidth / DestWidth );
				}
			}
			else
			{
				FillRect = StageRect;
			}

			return (StageRect, FillRect);
		}
	}
}