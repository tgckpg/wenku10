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

using GR.AdvDM;
using GR.CompositeElement;
using GR.Database.Models;
using GR.Model.Book.Spider;
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
			bool NeedDownload = false;

			Volume V = ReaderPage.CurrentChapter.Volume;
			NeedDownload = Shared.BooksDb.Chapters.Any( x => x.Volume == V && x.Content == null );

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

			// Really, this desperate?
			TaskCompletionSource<AsyncTryOut<Chapter>> TCSChapter = new TaskCompletionSource<AsyncTryOut<Chapter>>();
			ChapterList.ItemsSource = V.Chapters;

			Action<object, DCycleCompleteArgs> CycleComp = null;

			CycleComp = delegate ( object sender, DCycleCompleteArgs e )
			{
				bool AllSet = V.Chapters.All( x => !string.IsNullOrEmpty( x.Content.Text ) );

				Chapter C = V.Chapters.FirstOrDefault( x => x.Image != null );

				if ( C == null )
				{
					if ( AllSet ) Worker.UIInvoke( () => Message.Text = "No Illustration available" );
					TCSChapter.TrySetResult( new AsyncTryOut<Chapter>() );
					return;
				}

				TCSChapter.TrySetResult( new AsyncTryOut<Chapter>( true, C ) );
			};

			AutoCache.DownloadVolume( ReaderPage.CurrentBook, V );

			return await TCSChapter.Task;
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