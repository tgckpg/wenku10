using System;
using System.Linq;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace GR.Model.Loaders
{
	using AdvDM;
	using Ext;
	using Book;
	using Book.Spider;
	using GSystem;
	using ListItem;
	using Resources;
	using Text;

	sealed class BookLoader : IBookLoader
	{
		public static readonly string ID = typeof( BookLoader ).Name;

		private BookItem CurrentBook;

		private Action<BookItem> CompleteHandler;

		public BookLoader() { }

		public BookLoader( Action<BookItem> Handler )
		{
			CompleteHandler = Handler;
		}

		public void Load( BookItem b, bool useCache = false )
		{
			CurrentBook = b;

			if ( b.IsLocal() )
			{
				OnComplete( b );
			}
			else if ( b.IsSpider() )
			{
				LoadInstruction( ( BookInstruction ) b, useCache );
			}
			else if ( CurrentBook.IsEx() )
			{
				string BookId = b.ZItemId;
				string Mode = CurrentBook.XField<string>( "Mode" );
				string FlagMode = "MODE_" + Mode;

				if ( useCache && b.Info.Flags.Contains( FlagMode ) )
				{
					OnComplete( b );
				}
				else
				{
					b.Info.Flags.Add( FlagMode );
					X.Instance<IRuntimeCache>( XProto.WRuntimeCache )
						.InitDownload( BookId, X.Call<XKey[]>( XProto.WRequest, "DoBookAction", Mode, BookId ), PrelaodBookInfo, PrelaodBookInfo, true );
				}
			}
		}

		public async void LoadInstruction( BookInstruction B, bool useCache )
		{
			SpiderBook SBook = await SpiderBook.CreateSAsync( B.ZoneId, B.ZItemId, B.BookSpiderDef );

			if ( Shared.Storage.FileExists( SBook.MetaLocation ) )
			{
				B.LastCache = Shared.Storage.FileTime( SBook.MetaLocation ).LocalDateTime;
			}


			if ( useCache && ( B.Packed == true || Shared.BooksDb.Volumes.Any( x => x.Book == B.Entry ) ) )
			{
				if ( B.Packed != true )
				{
					B.PackSavedVols( SBook.PSettings );
				}
			}
			else
			{
				await SBook.Process();

				// Cannot download content, use cache if available
				if ( !( B.Packed == true || B.Packable ) && Shared.BooksDb.Volumes.Any( x => x.Book == B.Entry ) )
				{
					Logger.Log( ID, "Spider failed to produce instructions, using cache instead", LogType.WARNING );
					B.PackSavedVols( SBook.PSettings );
				}
			}

			if ( B.Packed != true && B.Packable )
			{
				B.PackVolumes( SBook.GetPPConvoy() );
			}

			if ( SBook.Processed && SBook.ProcessSuccess )
				B.LastCache = DateTime.Now;

			await CacheCover( B, true );

			OnComplete( B );
		}

		public void LoadIntro( BookItem b, bool useCache = true )
		{
			if ( b.IsSpider() ) return;

			CurrentBook = b;

			if ( !useCache || string.IsNullOrEmpty( b.Info.LongDescription ) )
			{
				X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
					b.ZItemId
					, X.Call<XKey[]>( XProto.WRequest, "GetBookIntro", b.ZItemId )
					, SaveIntro, IntroFailed, false
				);
			}
		}

		private void IntroFailed( string arg1, string arg2, Exception arg3 )
		{
			StringResources stx = new StringResources( "Error" );
			CurrentBook.IntroError( stx.Str( "Download" ) );
		}

		private void SaveIntro( DRequestCompletedEventArgs e, string id )
		{
			CurrentBook.Intro = Manipulation.PatchSyntax( Shared.TC.Translate( e.ResponseString ) );
		}

		private void PrelaodBookInfo( string cacheName, string id, Exception ex )
		{
			// Deprecated as info are already present in database
			OnComplete( null );
		}

		private void PrelaodBookInfo( DRequestCompletedEventArgs e, string id )
		{
			CurrentBook.LastCache = DateTime.Now;
			ExtractBookInfo( e.ResponseString, id );
		}

		private void ExtractBookInfo( string InfoData, string id )
		{
			InfoData = Shared.TC.Translate( InfoData );
			CurrentBook.ParseXml( InfoData );
			CurrentBook.SaveInfo();

			if ( Shared.Storage.FileExists( CurrentBook.CoverPath ) )
			{
				OnComplete( CurrentBook );
			}
			else
			{
				X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
					id, X.Call<XKey[]>( XProto.WRequest, "GetBookCover", id )
					, CoverDownloaded, Utils.DoNothing, false
				);
			}
		}

		public async void LoadCover( BookItem Book, bool Cache )
		{
			CurrentBook = Book;
			await CacheCover( Book, Cache );
			OnComplete( Book );
		}

		private async Task CacheCover( BookItem B, bool Cache )
		{
			if ( Cache && Shared.Storage.FileExists( B.CoverPath ) ) return;

			if ( string.IsNullOrEmpty( B.Info.CoverSrcUrl ) )
			{
				// Use bing service
				string ThumbUrl = await new BingService( B ).GetImage();
				if ( string.IsNullOrEmpty( ThumbUrl ) ) return;

				B.Info.CoverSrcUrl = ThumbUrl;
			}

			TaskCompletionSource<int> Awaitable = new TaskCompletionSource<int>();

			// Set the referer, as it is required by some site such as fanfiction.net
			new RuntimeCache( a => {
				HttpRequest R = new WHttpRequest( a ) { EN_UITHREAD = false };

				if ( !string.IsNullOrEmpty( B.Info.OriginalUrl ) )
				{
					R.RequestHeaders.Referrer = new Uri( B.Info.OriginalUrl );
				}

				return R;
			} ).GET( new Uri( B.Info.CoverSrcUrl ), ( a, b ) => {
				CoverDownloaded( a, b );
				Awaitable.TrySetResult( 0 );
			}
			// Failed handler
			, ( a, b, c ) =>
			{
				Awaitable.TrySetResult( 0 );
			}, false );

			await Awaitable.Task;
		}

		private void CoverDownloaded( DRequestCompletedEventArgs e, string id )
		{
			Shared.Storage.WriteBytes( CurrentBook.CoverPath, e.ResponseBytes );

			SetCover( CurrentBook );
			OnComplete( CurrentBook );
		}

		private void SetCover( BookItem B )
		{
			B.CoverUpdate();
		}

		private void OnComplete( BookItem b )
		{
			if ( CompleteHandler != null )
			{
				Worker.UIInvoke( () => { CompleteHandler( b ); } );
			}
		}

	}
}