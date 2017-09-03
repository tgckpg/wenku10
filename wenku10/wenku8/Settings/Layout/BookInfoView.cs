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

namespace wenku8.Settings.Layout
{
	using AdvDM;
	using Effects;
	using Model.Book;
	using Resources;

	sealed class BookInfoView
	{
		public enum JumpMode { CONTENT_READER = 0, INFO_VIEW = 1 }

		public static readonly string ID = typeof( BookInfoView ).Name;

		private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_BOOKINFOVIEW;
		private const string RightToLeft = "RightToLeft";
		private const string HrTOCName = "HorizontalTOC";
		private const string TwConfirm = "TwitterConfirmed";
		private const string JumpModeName = "JumpMode";

		private Dictionary<string, BgContext> SectionBgs;

		public bool IsRightToLeft
		{
			get
			{
				return LayoutSettings.Parameter( RightToLeft ).GetBool( "enable" );
			}
			set
			{
				LayoutSettings.SetParameter( RightToLeft, new XKey( "enable", value ) );
				LayoutSettings.Save();
			}
		}

		public bool HorizontalTOC
		{
			get
			{
				return LayoutSettings.Parameter( HrTOCName ).GetBool( "enable" );
			}
			set
			{
				LayoutSettings.SetParameter( HrTOCName, new XKey( "enable", value ) );
				LayoutSettings.Save();
			}
		}

		public bool TwitterConfirmed
		{
			get
			{
				return LayoutSettings.Parameter( TwConfirm ).GetBool( "val" );
			}
			set
			{
				LayoutSettings.SetParameter( TwConfirm, new XKey( "val", value ) );
				LayoutSettings.Save();
			}
		}

		public JumpMode ItemJumpMode
		{
			get
			{
				return ( JumpMode ) LayoutSettings.Parameter( JumpModeName ).GetSaveInt( "val" );
			}
			set
			{
				LayoutSettings.SetParameter( JumpModeName, new XKey( "val", ( int ) value ) );
				LayoutSettings.Save();
			}
		}

		private XRegistry LayoutSettings;

		public BookInfoView()
		{
			LayoutSettings = new XRegistry( AppKeys.TS_CXML, TFileName );
			SectionBgs = new Dictionary<string, BgContext>();
			InitParams();
		}

		public void InitParams()
		{
			bool Changed = false;

			if ( LayoutSettings.Parameter( RightToLeft ) == null )
			{
				LayoutSettings.SetParameter(
					RightToLeft
					, new XKey( "enable", Shared.LocaleDefaults.Get<bool>( "BookInfoView.IsRightToLeft" ) )
				);
				Changed = true;
			}

			if ( LayoutSettings.Parameter( HrTOCName ) == null )
			{
				LayoutSettings.SetParameter(
					HrTOCName
					, new XKey( "enable", Shared.LocaleDefaults.Get<bool>( "BookInfoView.HorizontalTOC" ) )
				);
				Changed = true;
			}

			if ( LayoutSettings.Parameter( TwConfirm ) == null )
			{
				LayoutSettings.SetParameter( TwConfirm, new XKey( "val", false ) );
				Changed = true;
			}

			if ( LayoutSettings.Parameter( JumpModeName ) == null )
			{
				LayoutSettings.SetParameter( JumpModeName, new XKey( "val", ( int ) JumpMode.CONTENT_READER ) );
				Changed = true;
			}

			if ( Changed ) LayoutSettings.Save();
		}

		public BgContext GetBgContext( string Section )
		{
			if ( SectionBgs.ContainsKey( Section ) ) return SectionBgs[ Section ];

			BgContext b = new BgContext( LayoutSettings, Section );

			return SectionBgs[ Section ] = b; ;
		}

		/// <summary>
		/// Background Context Object, Controls section backgrounds
		/// </summary>
		internal class BgContext : ActiveData
		{
			XRegistry LayoutSettings;

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

			public string Section { get; private set; }
			public string BgType { get { return LayoutSettings.Parameter( Section )?.GetValue( "type" ); } }

			private bool SwState = false;
			private string CurrLocation;

			public BgContext( XRegistry LayoutSettings, string Section )
			{
				this.LayoutSettings = LayoutSettings;
				this.Section = Section;
			}

			public void Reload()
			{
				LayoutSettings = new XRegistry( "<NaN />", LayoutSettings.Location );
				ApplyBackgrounds();
			}

			public async void ApplyBackgrounds()
			{
				XParameter P = LayoutSettings.Parameter( Section );

				// Default value
				if ( P == null )
				{
					SetBackground( "System" );
					return;
				}

				string value = P.GetValue( "value" );
				if ( value == null ) return;

				bool UseDefault = false;

				switch ( P.GetValue( "type" ) )
				{
					case "None":
						ApplyImage( null );
						break;
					case "Custom":
						IStorageFolder ISD = await AppStorage.FutureAccessList.GetFolderAsync( value );
						if ( ISD == null ) return;

						// Randomly pick an image
						string[] Acceptables = new string[] { ".JPG", ".PNG", ".GIF" };
						IEnumerable<IStorageFile> ISFs = await ISD.GetFilesAsync();

						IStorageFile[] ISImgs = ISFs.Where( x => Acceptables.Contains( x.FileType.ToUpper() ) && x.Path != CurrLocation ).ToArray();
						if ( ISImgs.Length == 0 ) return;

						ApplyImage( NTimer.RandChoice( ISImgs ) );
						break;
					case "Preset":
						try
						{
							List<string> ImagePaths = new List<string>();
							foreach ( Volume V in Book.GetVolumes() )
							{
								foreach ( Chapter C in V.ChapterList )
								{
									if ( C.HasIllustrations )
									{
										ImagePaths.AddRange(
											Shared.Storage.GetString( C.IllustrationPath )
											.Split( new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries )
										);
									}
								}
							}

							if ( 0 < ImagePaths.Count )
							{
								string Url = ImagePaths[ NTimer.RandInt( ImagePaths.Count() ) ];
								TryUseImage( Url );
							}
							else
							{
								UseDefault = true;
							}
						}
						catch ( Exception ex )
						{
							Logger.Log( ID, ex.Message, LogType.ERROR );
						}
						break;
					default:
					case "System":
						UseDefault = true;
						break;
				}

				if ( UseDefault )
				{
					ApplyImage( value, true );
				}
			}

			public async void SetBackground( string type )
			{
				XParameter SecParam = LayoutSettings.Parameter( Section );
				if ( SecParam == null ) SecParam = new XParameter( Section );

				string value = null;
				switch ( type )
				{
					case "Custom":
						IStorageFolder Location = await PickDirFromPicLibrary();
						if ( Location == null ) return;

						value = SecParam.GetValue( "value" );
						if ( value == null ) value = Guid.NewGuid().ToString();

						AppStorage.FutureAccessList.AddOrReplace( value, Location );

						break;
					// Preset value fall offs to system as default value
					case "Preset":
					case "System":
						switch ( Section )
						{
							case "INFO_VIEW":
								value = "ms-appx:///Assets/Samples/BgInfoView.jpg";
								break;
							case "CONTENT_READER":
								value = "ms-appx:///Assets/Samples/BgContentReader.jpg";
								break;
							case "STAFF_PICKS":
								value = "ms-appx:///Assets/Samples/BgContentReader.jpg";
								break;
						}

						break;
				}

				SecParam.SetValue( new XKey[] {
					new XKey( "type", type )
					, new XKey( "value", value )
				} );

				LayoutSettings.SetParameter( SecParam );

				ApplyBackgrounds();
				LayoutSettings.Save();
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
}