using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace GR.Model.Section
{
	using AdvDM;
	using Book;
	using Config.Scopes;
	using Database.Models;
	using Effects;
	using Resources;
	using Settings;

	sealed class BgContext : ActiveData
	{
		IConf_BgContext Conf;

		private BookItem _Book;
		public BookItem Book
		{
			get { return _Book ?? Shared.CurrentBook; }
			set { _Book = value; }
		}

		private ImageSource bg, bg2;
		public ImageSource Background
		{
			get { return bg; }
			private set
			{
				bg = value;
				NotifyChanged( "Background" );
			}
		}
		public ImageSource Background2
		{
			get { return bg2; }
			private set
			{
				bg2 = value;
				NotifyChanged( "Background2" );
			}
		}

		private bool bgs = false, bgs2 = false;
		public bool BGState
		{
			get { return bgs; }
			private set
			{
				bgs = value;
				NotifyChanged( "BGState" );
			}
		}
		public bool BGState2
		{
			get { return bgs2; }
			private set
			{
				bgs2 = value;
				NotifyChanged( "BGState2" );
			}
		}

		private bool SwState = false;
		private string CurrLocation;

		public BgContext( IConf_BgContext LayoutSettings )
		{
			this.Conf = LayoutSettings;
		}

		public void Reload()
		{
			ApplyBackgrounds();
		}

		public async void ApplyBackgrounds()
		{
			string BgType = Conf.BgType;

			// Default value
			if ( BgType == null )
			{
				SetBackground( "System" );
				return;
			}

			string BgValue = Conf.BgValue;

			bool UseDefault = false;

			switch ( BgType )
			{
				case "None":
					ApplyImage( null );
					break;
				case "Custom":
					if ( BgValue == null ) return;

					IStorageFolder ISD = null;
					try { ISD = await AppStorage.FutureAccessList.GetFolderAsync( BgValue ); }
					catch ( Exception ) { }

					if ( ISD == null ) return;

					// Randomly pick an image
					string[] Acceptables = new string[] { ".JPG", ".PNG", ".GIF" };
					IEnumerable<IStorageFile> ISFs = await ISD.GetFilesAsync();

					IStorageFile[] ISImgs = ISFs.Where( x => Acceptables.Contains( x.FileType.ToUpper() ) && x.Path != CurrLocation ).ToArray();
					if ( ISImgs.Length == 0 ) return;

					ApplyImage( NTimer.RandChoice( ISImgs ) );
					break;
				case "Preset":
					UseDefault = true;
					try
					{
						List<string> ImagePaths = new List<string>();
						if ( Book != null )
						{
							foreach ( ChapterImage C in Shared.BooksDb.GetBookImages( Book.Id ) )
							{
								ImagePaths.AddRange( C.Urls );
							}
						}

						if ( 0 < ImagePaths.Count )
						{
							string Url = ImagePaths[ NTimer.RandInt( ImagePaths.Count() ) ];
							TryUseImage( Url );
							UseDefault = false;
						}
					}
					catch ( Exception ex )
					{
						Logger.Log( "BgContext", ex.Message, LogType.ERROR );
					}
					break;
				default:
				case "System":
					UseDefault = true;
					break;
			}

			if ( UseDefault )
			{
				ApplyImage( BgValue, true );
			}
		}

		public async void SetBackground( string BgType )
		{
			string BgValue = null;
			switch ( BgType )
			{
				case "Custom":
					IStorageFolder Location = await PickDirFromPicLibrary();
					if ( Location == null ) return;

					BgValue = Conf.BgValue;
					if ( BgValue == null ) BgValue = Guid.NewGuid().ToString();

					AppStorage.FutureAccessList.AddOrReplace( BgValue, Location );

					break;
				// Preset value fall offs to system as default value
				case "Preset":
				case "System":
					break;
			}

			Conf.BgType = BgType;
			Conf.BgValue = BgValue;

			ApplyBackgrounds();
		}

		private async Task<IStorageFolder> PickDirFromPicLibrary()
		{
			return await AppStorage.OpenDirAsync( x =>
			{
				x.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
				x.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
			} );
			;
		}

		private void SwapImage( BitmapImage b )
		{
			Action<BitmapImage> Front = async x =>
			{
				if ( BGState = ( x != null ) )
				{
					Background = x;
					BGState2 = false;
					await Task.Delay( 1000 );
					Image.Destroy( Background2 );
				}
			};

			Action<BitmapImage> Back = async x =>
			{
				if ( BGState2 = ( x != null ) )
				{
					Background2 = x;
					BGState = false;
					await Task.Delay( 1000 );
					Image.Destroy( Background );
				}
			};

			if ( SwState = !SwState ) Back = Front;

			// Show the back
			Back( b );
		}

		private async void TryUseImage( string Url )
		{
			WBackgroundTransfer Transfer = new WBackgroundTransfer();
			Guid id = Guid.Empty;

			Transfer.OnThreadComplete += ( DTheradCompleteArgs DArgs ) =>
			{
				if ( DArgs.Id.Equals( id ) )
				{
					ApplyImage( DArgs.FileLocation );
				}
			};

			string fileName = Url.Substring( Url.LastIndexOf( '/' ) + 1 );
			string imageLocation = FileLinks.ROOT_IMAGE + fileName;

			if ( Shared.Storage.FileExists( imageLocation ) )
			{
				ApplyImage( imageLocation );
			}
			else
			{
				id = await Transfer.RegisterImage( Url, imageLocation );
			}
		}

		private async void ApplyImage( IStorageFile ISF )
		{
			string Location = ISF?.Path;

			if ( CurrLocation == Location ) return;
			CurrLocation = Location;

			BitmapImage b = await Image.NewBitmap();
			b.SetSourceFromISF( ISF );

			SwapImage( b );
		}

		private async void ApplyImage( string Location, bool FromSystem = false )
		{
			if ( CurrLocation == Location ) return;
			CurrLocation = Location;

			BitmapImage b;
			if ( FromSystem )
			{
				b = await Image.NewBitmap( new Uri( Location, UriKind.Absolute ) );
			}
			else
			{
				b = await Image.NewBitmap();
				b.SetSourceFromUrl( Location );
			}

			SwapImage( b );
		}

	}
}