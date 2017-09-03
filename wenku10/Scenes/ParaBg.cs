using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;

using wenku8.Effects;

namespace wenku10.Scenes
{
	using BgContext = wenku8.Settings.Layout.BookInfoView.BgContext;

	sealed class ParaBg : ITextureScene, ISceneExitable
	{
		private Size StageSize;
		private Rect StageRect;

		public Uri UriSource { get; private set; }
		public CanvasBitmap SrcBmp;

		private BgContext DataContext;
		private ICanvasResourceCreator ResCreator;

		private ScrollViewer BoundControl;
		private long SVToken;

		private Rect FillRect;

		private float v = 0;
		private float vh = 0;

		private float d = 0;

		public ParaBg( BgContext Context )
		{
			StageSize = Size.Empty;

			DataContext = Context;
			Context.PropertyChanged += Context_PropertyChanged;
		}

		public void Bind( ScrollViewer SV )
		{
			if ( BoundControl != null ) BoundControl.ViewChanged -= SV_ViewChanged;
			SV.ViewChanged += SV_ViewChanged;
			BoundControl = SV;
		}

		private void SV_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			d = vh * ( float ) BoundControl.VerticalOffset / ( float ) BoundControl.ScrollableHeight;
			if ( float.IsNaN( d ) ) d = 0;
		}

		private void Context_PropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "Background" )
			{
				BitmapImage Bmp = ( BitmapImage ) DataContext.Background;
				UriSource = Bmp.UriSource;
				ReloadImage();
			}
		}

		public void Dispose()
		{
			if ( BoundControl != null ) BoundControl.ViewChanged -= SV_ViewChanged;
			DataContext.PropertyChanged -= Context_PropertyChanged;
		}

		public void Draw( CanvasDrawingSession ds, CanvasSpriteBatch SBatch, TextureLoader Textures )
		{
			if ( SrcBmp == null ) return;

			v = 0.85f * v + 0.15f * d;
			FillRect.Y = v;
			ds.DrawImage( SrcBmp, StageRect, FillRect, 0.2f );
		}

		public async Task Exit()
		{
			await Task.Delay( 1000 );
		}

		public Task LoadTextures( CanvasAnimatedControl Canvas, TextureLoader Textures )
		{
			ResCreator = Canvas;
			ReloadImage();
			return Task.Delay( 0 );
		}

		public void UpdateAssets( Size S )
		{
			StageSize = S;
			FitImage();
		}

		private async void ReloadImage()
		{
			if ( ResCreator == null || UriSource == null ) return;
			SrcBmp = await CanvasBitmap.LoadAsync( ResCreator, UriSource );

			FitImage();
		}

		private void FitImage()
		{
			if ( StageSize.IsEmpty || SrcBmp == null ) return;

			float DestWidth = ( float ) StageSize.Width;
			float DestHeight = ( float ) StageSize.Height;

			StageRect = FillRect = new Rect( 0, 0, DestWidth, DestHeight );

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

			vh = 0.5f * ( SrcHeight - ( float ) FillRect.Height );
		}

	}
}