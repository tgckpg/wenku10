using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media;

using Microsoft.Graphics.Canvas.UI.Xaml;

using Net.Astropenguin.IO;

using wenku10.Scenes;

namespace wenku8.Model.Topics
{
	using Effects;
	using Effects.P2DFlow;
	using ListItem;
	using Pages;
	using Settings;

	using BgContext = Settings.Layout.BookInfoView.BgContext;
	using Windows.UI.Xaml;
	using Windows.UI.Xaml.Controls;

	sealed class HyperBannerItem : ActiveItem, IDisposable
	{
		public CanvasAnimatedControl CanvasElem { get; private set; }

		public ActiveItem Source { get; private set; }
		public CanvasStage Stage { get; private set; }
		public HyperBanner Banner { get; private set; }

		public FireFlies FireFliesScene { get; private set; }

		public GridLength LWidth { get; private set; }
		public GridLength RWidth { get; private set; }

		public Thickness Margin { get; private set; }
		public int DescRow { get; private set; }
		public int DescRowSpan { get; private set; }
		public int DescCol { get; private set; }
		public int DescColSpan { get; private set; }

		private bool _NarrowScr = false;
		public bool NarrowScr
		{
			get { return _NarrowScr; }
			set
			{
				if ( _NarrowScr == value )
					return;
				_NarrowScr = value;
				UpdateCanvas();
			}
		}

		private int _Index = 0;
		public int Index
		{
			get { return _Index; }
			set
			{
				if ( _Index == value ) return;
				_Index = value;
				UpdateCanvas();
			}
		}

		public bool IsLeft { get { return _Index % 2 == 0; } }
		public int SideLen { get { return _NarrowScr ? 280 : 360; } }

		public HyperBannerItem( ActiveItem Item, Stack<Particle> PStack )
		{
			Source = Item;

			CanvasElem = new CanvasAnimatedControl();
			CanvasElem.UseSharedDevice = true;

			Stage = new CanvasStage( CanvasElem );

			// TheOrb LoadingTrails = new TheOrb( PStack, i % 2 == 0 );
			FireFliesScene = new FireFlies( PStack );
			Stage.Add( FireFliesScene );
			// Stage.Add( LoadingTrails );

			// var j = Stage.Remove( typeof( TheOrb ) );

			UpdateGrid();
		}

		public async void SetBanner( ScrollViewer LayoutRoot )
		{
			XRegistry SSettings = new XRegistry( "<sp />", FileLinks.ROOT_SETTING + FileLinks.LAYOUT_STAFFPICKS );

			// Set the bg context
			BgContext ItemContext = new BgContext( SSettings, "STAFF_PICKS" )
			{
				Book = await ItemProcessor.GetBookFromId( Source.Payload )
			};
			ItemContext.SetBackground( "Preset" );

			await Stage.Remove( typeof( HyperBanner ) );

			Banner = new HyperBanner( Source, ItemContext );
			Banner.Bind( LayoutRoot );

			Banner.TextSpeed = 0.005f * NTimer.RFloat();
			Banner.TextRotation = 6.2832f * NTimer.RFloat();

			Stage.Insert( 0, Banner );

			UpdateCanvas();
		}

		private void UpdateGrid()
		{
			LWidth = new GridLength( 1, GridUnitType.Star );
			RWidth = new GridLength( 1, GridUnitType.Star );

			if ( NarrowScr )
			{
				Margin = new Thickness( 20, 0, 20, 20 );
				DescCol = 0;
				DescRow = 1;
				DescColSpan = 2;
				DescRowSpan = 1;
			}
			else
			{
				DescRow = 0;
				DescColSpan = 1;
				DescRowSpan = 2;

				if ( _Index % 2 == 0 )
				{
					Margin = new Thickness( 5, 20, 20, 20 );
					DescCol = 1;

					LWidth = new GridLength( SideLen, GridUnitType.Pixel );
				}
				else
				{
					Margin = new Thickness( 20, 20, 5, 20 );
					DescCol = 0;

					RWidth = new GridLength( SideLen, GridUnitType.Pixel );
				}
			}

			NotifyChanged( "LWidth", "RWidth", "Margin", "DescRow", "DescCol", "DescRowSpan", "DescColSpan" );
		}

		private void UpdateCanvas()
		{
			if ( Banner == null ) return;

			if ( NarrowScr )
			{
				Banner.Align = HorizontalAlignment.Center;
				Banner.SideLen = SideLen;
			}
			else
			{
				Banner.Align = IsLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right;
				Banner.SideLen = SideLen;
			}

			UpdateGrid();
		}

		public void Dispose()
		{
			Stage?.Dispose();
			CanvasElem?.RemoveFromVisualTree();
		}

	}
}