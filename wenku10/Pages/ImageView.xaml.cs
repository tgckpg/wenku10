using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;

using GR.Model.ListItem;
using GR.Model.Loaders;
using GR.Model.Text;

namespace wenku10.Pages
{
	public sealed partial class ImageView : Page
	{
		public static readonly string ID = typeof( ImageView ).Name;

		public ImageView()
		{
			this.InitializeComponent();
			LoadingRing.IsActive = true;
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

			if ( e.Parameter is Tuple<IList<ImageThumb>, ImageThumb> Params )
			{
				SetImages( Params.Item1, Params.Item2 );
			}
			else if ( e.Parameter is IllusPara )
			{
				SetImage( ( IllusPara ) e.Parameter );
			}
		}

		private void SetImages( IList<ImageThumb> ImageList, ImageThumb ActiveImage )
		{
			ImagesView.ItemsSource = ImageList;
			ImagesView.SelectedIndex = ImageList.IndexOf( ActiveImage );
		}

		private void SetImage( IllusPara Para )
		{
			if ( Para.ImgThumb == null || Para.ImgThumb.IsDownloadNeeded )
			{
				Para.PropertyChanged += Para_PropertyChanged;
				ContentIllusLoader.Instance.RegisterImage( Para );
			}
			else
			{
				ImagesView.ItemsSource = new ImageThumb[] { Para.ImgThumb };
			}
		}

		private void Para_PropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "Illus" )
			{
				IllusPara Para = ( IllusPara ) sender;
				Para.PropertyChanged -= Para_PropertyChanged;

				ImagesView.ItemsSource = new ImageThumb[] { Para.ImgThumb };
			}
		}

		private void ResetZoom( ScrollViewer SV, ImageThumb ImgThumb )
		{
			void FitZoom( object NOP_0, PropertyChangedEventArgs NOP_1 )
			{
				if ( !ImgThumb.Equals( SV.DataContext ) )
				{
					ImgThumb.PropertyChanged -= FitZoom;
					return;
				}

				if ( ImgThumb.ImgSrc != null )
				{
					double SVRatio = SV.ViewportWidth / SV.ViewportHeight;
					double ImgRatio = ImgThumb.FullWidth / ImgThumb.FullHeight;
					if ( ImgRatio < SVRatio )
					{
						SV.ChangeView( null, null, ( float ) ( SV.ViewportHeight / ImgThumb.FullHeight ) );
					}
					else
					{
						SV.ChangeView( null, null, ( float ) ( SV.ViewportWidth / ImgThumb.FullWidth ) );

					}
				}
			}

			if ( ImgThumb.ImgSrc == null )
			{
				ImgThumb.PropertyChanged += FitZoom;
			}
			else
			{
				FitZoom( null, null );
			}
		}

		private VirtualizingStackPanel VSP;

		private void ImagesView_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			VSP = ImagesView.ChildAt<VirtualizingStackPanel>( 0, 0, 0, 0, 0, 0, 1 );

			if ( VSP == null )
			{
				// Items has not yet been visualized yet. We'll wait.
				ImagesView.LayoutUpdated += ImagesView_LayoutUpdated;
			}
			else if ( e.AddedItems.Any() )
			{
				if ( !TryResetZoom( e.AddedItems.First() ) )
				{
					// Cannot reset, item is not yet visualized
					ImagesView.LayoutUpdated += ImagesView_LayoutUpdated;
				}
			}
		}

		private void ImagesView_LayoutUpdated( object sender, object e )
		{
			if ( VSP == null )
			{
				VSP = ImagesView.ChildAt<VirtualizingStackPanel>( 0, 0, 0, 0, 0, 0, 1 );
			}

			if ( VSP != null && TryResetZoom( ImagesView.SelectedItem ) )
			{
				ImagesView.LayoutUpdated -= ImagesView_LayoutUpdated;
			}
		}

		private bool TryResetZoom( object DataItem )
		{
			uint i = 0;
			FlipViewItem Item = VSP.ChildAt<FlipViewItem>( i );

			while ( Item != null )
			{
				if ( Item.DataContext.Equals( DataItem ) )
				{
					ScrollViewer SV = Item.Child_0<ScrollViewer>( 1 );
					ResetZoom( SV, ( ImageThumb ) DataItem );
					return true;
				}
				Item = VSP.ChildAt<FlipViewItem>( ++i );
			}

			return false;
		}

	}
}