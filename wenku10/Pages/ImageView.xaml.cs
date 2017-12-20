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

using Net.Astropenguin.Logging;

using GR.Model.ListItem;
using GR.Model.Loaders;
using GR.Model.Text;

namespace wenku10.Pages
{
	public sealed partial class ImageView : Page
	{
		public static readonly string ID = typeof( ImageView ).Name;

		private ImageThumb Img;

		public ImageView()
		{
			this.InitializeComponent();
			LoadingRing.IsActive = true;
		}

		protected override void OnNavigatedFrom( NavigationEventArgs e )
		{
			base.OnNavigatedFrom( e );
			Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
			var j = Img?.Set();
			Img = null;
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

			if ( e.Parameter is ImageThumb )
			{
				SetImage( ( ImageThumb ) e.Parameter );
			}
			else if ( e.Parameter is IllusPara )
			{
				SetImage( ( IllusPara ) e.Parameter );
			}
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
				SetImage( Para.ImgThumb );
			}

		}

		private void Para_PropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "Illus" )
			{
				IllusPara Para = ( IllusPara ) sender;
				Para.PropertyChanged -= Para_PropertyChanged;

				SetImage( Para.ImgThumb );
			}
		}

		private async void SetImage( ImageThumb Th )
		{
			Img = Th;
			MainImage.Source = await Img.GetFull();
			LoadingRing.IsActive = false;
		}

	}
}