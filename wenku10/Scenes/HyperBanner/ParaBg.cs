using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;

using GR.Effects;
using GR.Effects.Stage;
using GR.Resources;
using GR.Model.Section;

namespace wenku10.Scenes
{
	sealed partial class HyperBanner
	{
		private Rect StageRect;

		public Uri BackgroundUri { get; private set; }
		public CanvasBitmap BgBmp;

		private ScrollViewer BoundControl;
		private BgContext DataContext;

		private Rect BgFillRect;
		private ICanvasBrush MaskBrush;

		private float BgY_c = 0;
		private float BgY_t = 0;
		private float vh = 0;

		private Action<CanvasDrawingSession> _BgDraw;
		private Action<CanvasDrawingSession> _SusBgDraw;

		public void Bind( ScrollViewer SV )
		{
			if ( BoundControl != null ) BoundControl.ViewChanged -= SV_ViewChanged;
			SV.ViewChanged += SV_ViewChanged;
			BoundControl = SV;
		}

		private void InitBackground( BgContext Context )
		{
			_BgDraw = DrawNothing;

			DataContext = Context;
			Context.PropertyChanged += Context_PropertyChanged;
		}

		private void ActivateBgDraw( Action<CanvasDrawingSession> F = null )
		{
			if ( BgBmp == null )
			{
				_SusBgDraw = F ?? DrawNothing;
			}
			else
			{
				_BgDraw = F ?? _SusBgDraw ?? DrawNothing;
			}
		}

		private void SV_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			float sh = ( float ) BoundControl.ScrollableHeight;
			if ( sh != 0 )
			{
				BgY_t = vh * ( float ) BoundControl.VerticalOffset / sh;
			}
		}

		private void Context_PropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "Background" )
			{
				BitmapImage Bmp = ( BitmapImage ) DataContext.Background;
				BackgroundUri = Bmp.UriSource;
				ReloadBackground();
			}
		}

		float BgR_c;
		float BgR_t;

		private void DrawIdleBg( CanvasDrawingSession ds )
		{
			CubicTween( ref BgY_c, BgY_t, 0.85f, 0.15f );
			BgFillRect.Y = BgY_c;

			CubicTween( ref BgR_c, BgR_t, 0.75f, 0.25f );
			if ( BgR_c < 0 )
			{
				ds.DrawImage( BgBmp, StageRect, BgFillRect );
				ds.FillRectangle( StageRect, MaskBrush );
			}
			else
			{
				ds.DrawImage( BgBmp, StageRect, BgFillRect );

				CanvasGeometry MaskFill = CanvasGeometry.CreateRectangle( ds, StageRect );
				CanvasGeometry DrillMask = CanvasGeometry.CreateCircle( ds, PCenter, BgR_c );
				CanvasGeometry Combined = MaskFill.CombineWith( DrillMask, Matrix3x2.CreateTranslation( 0, 0 ), CanvasGeometryCombine.Exclude );

				ds.FillGeometry( Combined, MaskBrush );
			}
		}

		private async void ReloadBackground()
		{
			if ( ResCreator == null || BackgroundUri == null || StageSize.IsZero() ) return;

			if ( BackgroundUri.Scheme == "ms-appx" )
			{
				BgBmp = new RandomStripe( Seed ).DrawBitmap( ResCreator, ( int ) LayoutSettings.DisplayWidth, ( int ) LayoutSettings.DisplayHeight );
			}
			else
			{
				BgBmp = await CanvasBitmap.LoadAsync( ResCreator, BackgroundUri );
			}

			FitBackground();
		}

		private void FitBackground()
		{
			if ( StageSize.IsZero() ) return;

			if( BgBmp == null )
			{
				ReloadBackground();
				return;
			}

			(StageRect, BgFillRect) = ImageUtils.FitImage( StageSize, BgBmp );
			vh = 0.5f * ( BgBmp.SizeInPixels.Height - ( float ) BgFillRect.Height );

			ActivateBgDraw();
		}

	}
}