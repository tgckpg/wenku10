using System;
using System.Linq;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
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

				if ( useCache && b.Info.Flags.Contains( Mode ) )
				{
					OnComplete( b );
				}
				else
				{
					b.Info.Flags.Add( Mode );
					X.Instance<IRuntimeCache>( XProto.WRuntimeCache )
						.InitDownload(
							BookId, X.Call<XKey[]>( XProto.WRequest, "DoBookAction", Mode, BookId )
							, PreloadBookInfo
							, ( cache, id, ex ) =>
							{
								b.Info.Flags.Remove( Mode );
								OnComplete( null );
							}, true
						);
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
				bool UpdateFailed = true;

				await SBook.Process();
				if ( SBook.Processed && SBook.ProcessSuccess )
				{
					B.LastCache = DateTime.Now;
					BookInstruction BUpdate = SBook.GetBook();

					if ( BUpdate.Packable && BUpdate.Packed != true )
					{
						BUpdate.PackVolumes( SBook.GetPPConvoy() );
						B.Update( BUpdate );
						UpdateFailed = false;
					}
				}

				// Cannot download content, use cache if available
				if ( UpdateFailed && Shared.BooksDb.Volumes.Any( x => x.Book == B.Entry ) )
				{
					Logger.Log( ID, "Spider failed to produce instructions, using cache instead", LogType.WARNING );
					B.PackSavedVols( SBook.PSettings );
				}
			}

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
			StringResBg stx = new StringResBg( "Error" );
			CurrentBook.IntroError( stx.Str( "Download" ) );
		}

		private void SaveIntro( DRequestCompletedEventArgs e, string id )
		{
			CurrentBook.Intro = Manipulation.PatchSyntax( Shared.TC.Translate( e.ResponseString ) );
		}

		private void PreloadBookInfo( DRequestCompletedEventArgs e, string id )
		{
			CurrentBook.XCall( "ParseXml", Shared.TC.Translate( e.ResponseString ) );
			CurrentBook.SaveInfo();
			OnComplete( CurrentBook );
		}

		public async void LoadCover( BookItem B, bool Cache )
		{
			if ( Cache && B.CoverExist ) return;

			if ( B.IsEx() )
			{
				X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
					B.ZItemId, X.Call<XKey[]>( XProto.WRequest, "GetBookCover", B.ZItemId )
					, ( e, id ) => B.SaveCover( e.ResponseBytes )
					, Utils.DoNothing, false
				);
				return;
			}

			if ( string.IsNullOrEmpty( B.Info.CoverSrcUrl ) )
			{
				// Use bing service
				string ThumbUrl = await new BingService( B ).GetImage();
				if ( string.IsNullOrEmpty( ThumbUrl ) ) return;

				B.Info.CoverSrcUrl = ThumbUrl;
			}

			TaskCompletionSource<int> Awaitable = new TaskCompletionSource<int>();

			// Set the referer, as it is required by some site such as fanfiction.net
			new RuntimeCache( a =>
			{
				HttpRequest R = new WHttpRequest( a ) { EN_UITHREAD = false };

				if ( !string.IsNullOrEmpty( B.Info.OriginalUrl ) )
				{
					R.RequestHeaders.Referrer = new Uri( B.Info.OriginalUrl );
				}

				return R;
			} ).GET( new Uri( B.Info.CoverSrcUrl ), ( e, id ) => B.SaveCover( e.ResponseBytes ), Utils.DoNothing, false );
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