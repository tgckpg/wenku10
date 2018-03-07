using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using GR.AdvDM;
using GR.CompositeElement;
using GR.Database.Models;
using GR.Model.Book;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Loaders;
using GR.Resources;

namespace wenku10.Pages.ContentReaderPane
{
	sealed partial class ImageList : Page
	{
		private ContentReaderBase ReaderPage;
		public ImageList( ContentReaderBase R )
		{
			this.InitializeComponent();

			ReaderPage = R;
			SetTemplate();
		}

		private async void SetTemplate()
		{
			Chapter C = ReaderPage.CurrentChapter;
			if ( C.Image == null )
			{
				AsyncTryOut<Chapter> ASC;
				if ( ASC = await TryFoundIllustration() )
				{
					C = ASC.Out;
				}
				else
				{
					ChapterList.Visibility = Visibility.Collapsed;
					return;
				}
			}

			ChapterList.Visibility = Visibility.Collapsed;

			List<MViewUpdate> MViews = new List<MViewUpdate>();

			foreach( string url in C.Image.Urls )
			{
				// Retrive URL
				MViewUpdate MView = new MViewUpdate() { SrcUrl = url };
				ContentIllusLoader.Instance.RegisterImage( MView );
				MViews.Add( MView );
			}

			MainView.ItemsSource = MViews;
		}

		private void MainView_ItemClick( object sender, ItemClickEventArgs e )
		{
			MViewUpdate Img = ( MViewUpdate ) e.ClickedItem;
			if ( Img.IsDownloadNeeded ) return;

			ReaderPage.ClosePane();

			EventHandler<XBackRequestedEventArgs> ViewImage = null;
			ViewImage = ( sender2, e2 ) =>
			{
				NavigationHandler.OnNavigatedBack -= ViewImage;
				ReaderPage.RollOutLeftPane();
			};

			NavigationHandler.InsertHandlerOnNavigatedBack( ViewImage );

			ReaderPage.OverNavigate( typeof( ImageView ), Img.ImgThumb );
		}

		private async Task<AsyncTryOut<Chapter>> TryFoundIllustration()
		{
			Volume V = ReaderPage.CurrentChapter.Volume;
			ChapterImage CImage = Shared.BooksDb.ChapterImages.FirstOrDefault( x => V.Chapters.Contains( x.Chapter ) );

			if ( CImage != null )
			{
				return new AsyncTryOut<Chapter>( true, CImage.Chapter );
			}

			bool NeedDownload = Shared.BooksDb.Chapters.Any( x => x.Volume == V && x.Content == null );

			if ( !NeedDownload )
			{
				Message.Text = "No Image for this volume";
				return new AsyncTryOut<Chapter>();
			}

			NeedDownload = false;

			StringResources stm = new StringResources( "Message" );

			await Popups.ShowDialog( UIAliases.CreateDialog(
				 "Not enough information to see if there were any illustrations within this volume. Download this volume?"
				, () => NeedDownload = true
				, stm.Str( "Yes" ), stm.Str( "No" )
			) );

			if ( !NeedDownload )
			{
				Message.Text = "Not enough information for finding illustrations. Consider downloading a specific chapter";
				return new AsyncTryOut<Chapter>();
			}

			ChapterList.ItemsSource = V.Chapters.Select( x => new ChapterVModel( x ) );
			await AutoCache.DownloadVolumeAsync( ReaderPage.CurrentBook, V );

			Chapter ImageChapter = V.Chapters.FirstOrDefault( x => x.Image != null );

			if ( ImageChapter == null )
			{
				Worker.UIInvoke( () => Message.Text = "No Illustration available" );
				return new AsyncTryOut<Chapter>();
			}

			return new AsyncTryOut<Chapter>( true, ImageChapter );
		}

		private class MViewUpdate : ActiveData, IIllusUpdate
		{
			private ImageThumb _ImgThumb;
			public ImageThumb ImgThumb
			{
				get { return _ImgThumb; }
				set
				{
					_ImgThumb = value;
					_ImgThumb.PropertyChanged += _ImgThumb_PropertyChanged;
				}
			}

			public string SrcUrl { get; set; }

			public bool IsDownloadNeeded
			{
				get
				{
					if ( ImgThumb == null ) return true;
					return ImgThumb.IsDownloadNeeded;
				}
			}

			public ImageSource ImgSrc
			{
				get
				{
					if ( ImgThumb == null ) return null;
					return ImgThumb.ImgSrc;
				}
			}

			private void _ImgThumb_PropertyChanged( object sender, PropertyChangedEventArgs e )
			{
				NotifyChanged( e.PropertyName );
			}

			public async void Update()
			{
				await ImgThumb.Set();
				NotifyChanged( "IsDownloadNeeded", "ImgSrc" );
			}
		}

	}
}