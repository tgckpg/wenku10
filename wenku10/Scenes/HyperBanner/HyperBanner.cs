using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

using wenku8.Effects;
using wenku8.Resources;
using wenku8.Model.ListItem;

namespace wenku10.Scenes
{
	using BgContext = wenku8.Settings.Layout.BookInfoView.BgContext;

	sealed partial class HyperBanner : ITextureScene, ISceneExitable
	{
		private ICanvasResourceCreatorWithDpi ResCreator;
		private int Seed;

		public HyperBanner( ActiveItem Item, BgContext ItemContext )
		{
			Seed = wenku8.System.Utils.Md5Int( Item.Name );
			InitRipple( Item );
			InitBackground( ItemContext );
		}

		public Task LoadTextures( CanvasAnimatedControl Canvas, TextureLoader Textures )
		{
			// ParaBg 
			ResCreator = Canvas;

			Color MaskColor = LayoutSettings.MajorBackgroundColor;
			MaskColor.A = ( byte ) Math.Floor( 255 * 0.8 );
			MaskBrush = new CanvasSolidColorBrush( ResCreator, MaskColor );

			ReloadBackground();

			// RippleEx
			CoverBmp = CanvasBitmap.CreateFromColors( Canvas, new Color[] { Colors.Transparent }, 1, 1 );
			RingBrush = new CanvasSolidColorBrush( Canvas, LayoutSettings.Shades90 );
			TextBrush = new CanvasSolidColorBrush( ResCreator, Colors.White );
			CoverBrush = new CanvasSolidColorBrush( Canvas, Colors.Transparent );

			UpdateText( Canvas );
			return ReloadCover();
		}

		public void UpdateAssets( Size S )
		{
			StageSize = S;

			// ParaBg
			FitBackground();

			// RippleEx
			double SideLen = Math.Min( S.Height, S.Width );
			EyeBox = new Size( SideLen, SideLen );

			FitCover();
		}

		public void Draw( CanvasDrawingSession ds, CanvasSpriteBatch SBatch, TextureLoader Textures )
		{
			_BgDraw( ds );
			_RippleDraw( ds );
		}

		public void Click()
		{
			if ( Clicked ) return;
			Clicked = true;

			BgR_t = Math.Max(
				Vector2.Distance( PCenter, new Vector2( ( float ) StageSize.Width, ( float ) StageSize.Height ) )
				, Vector2.Distance( PCenter, Vector2.Zero )
			);
			BgR_c = -20 * BgR_t; 

			ActivateRippleDraw( DrawClickedRipple );
		}

		public void Blur() 
		{
			if ( Clicked ) return;

			TextR_t = MaxR * ( IrisFactor + 0.25f * ( 1 - IrisFactor ) );
			TextBrush_t = LayoutSettings.RelativeShadesBrush;
			ImgR_t = MaxR * IrisFactor;
			RingR_t = MaxR;

			BgR_t = ImgR_t;
		}

		public void Hover()
		{
			if ( Clicked ) return;
			ImgR_t = MaxR * ( IrisFactor + 0.25f * ( 1 - IrisFactor ) );
			RingR_t = MaxR * ( IrisFactor + 0.75f * ( 1 - IrisFactor ) );

			TextBrush_t = LayoutSettings.RelativeMajorBackgroundColor;
			TextR_t = MaxR;

			BgR_t = RingR_t;
		}

		public void Focus()
		{
			if ( Clicked ) return;
			ImgR_t = MaxR * ( IrisFactor * 0.75f );
			RingR_t = ImgR_t * IrisFactor;
			TextR_t = 0;

			BgR_t = MaxR * ( 1.25f - 0.25f * IrisFactor );
		}

		public void Enter()
		{
			Clicked = false;
			ActivateRippleDraw( DrawIdleRipple );
			ActivateBgDraw( DrawIdleBg );
			Blur();
		}

		public async Task Exit()
		{
			await Task.Delay( 100 );
		}

		public void Dispose()
		{
			BgBmp?.Dispose();
			CoverBmp?.Dispose();
			RingBrush?.Dispose();
			TextBrush?.Dispose();
			CoverBrush?.Dispose();

			// Background
			if ( BoundControl != null ) BoundControl.ViewChanged -= SV_ViewChanged;
			DataContext.PropertyChanged -= Context_PropertyChanged;

			// RippleEx
			BindItem.PropertyChanged -= BindItem_PropertyChanged;
		}

		private void DrawNothing( CanvasDrawingSession ds ) { }

		private void CubicTween( ref float a, float b, float dx = 0.5f, float dy = 0.5f )
		{
			a = dx * a + dy * b;
		}

		private Color CubicTween( Color a, Color b, float dx = 0.5f, float dy = 0.5f )
		{
			return new Color()
			{
				A = ( byte ) ( dx * a.A + dy * b.A )
				, R = ( byte ) ( dx * a.R + dy * b.R )
				, G = ( byte ) ( dx * a.G + dy * b.G )
				, B = ( byte ) ( dx * a.B + dy * b.B )
			};
		}
	}
}