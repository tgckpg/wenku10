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
	using ListItem;
	using Resources;

	sealed class BookLoader
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
				Task.Run( () => LoadInstruction( ( BookInstruction ) b, useCache ) );
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
							, ( e, id ) =>
							{
								CurrentBook.XCall( "ParseXml", e.ResponseString );
								CurrentBook.LastCache = DateTime.Now;
								CurrentBook.SaveInfo();
								OnComplete( CurrentBook );
							}
							, ( cache, id, ex ) =>
							{
								b.Info.Flags.Remove( Mode );
								OnComplete( null );
							}, true
						);
				}
			}
		}

		private async void LoadInstruction( BookInstruction B, bool useCache )
		{
			if ( !BookInstruction.OpLocks.AcquireLock( B.GID, out AsyncLocks<string, bool>.QueueToken QT ) )
			{
				await QT.Task;
			}

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

			QT.TrySetResult( true );
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
			StringResources stx = StringResources.Load( "Error" );
			CurrentBook.IntroError( stx.Str( "Download" ) );
		}

		private void SaveIntro( DRequestCompletedEventArgs e, string id )
		{
			CurrentBook.Intro = Shared.Conv.Chinese.Translate( e.ResponseString );
		}

		public void LoadCover( BookItem B, bool Cache )
		{
			var j = LoadCoverAsync( B, Cache );
		}

		private static AsyncLocks<Database.Models.Book, bool> CoverProcess = new AsyncLocks<Database.Models.Book, bool>();
		public async Task<bool> LoadCoverAsync( BookItem B, bool Cache )
		{
			if ( Cache && B.CoverExist )
				return true;

			if ( !CoverProcess.AcquireLock( B.Entry, out var QToken ) )
			{
				goto AwaitToken;
			}

			if ( B.IsEx() )
			{
				X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
					B.ZItemId, X.Call<XKey[]>( XProto.WRequest, "GetBookCover", B.ZItemId )
					, ( e, id ) =>
					{
						B.SaveCover( e.ResponseBytes );
						QToken.TrySetResult( true );
					}
					, ( c, i, ex ) => QToken.TrySetResult( false )
					, false
				);

				goto AwaitToken;
			}

			if ( string.IsNullOrEmpty( B.Info.CoverSrcUrl ) )
			{
				// Use bing service
				string ThumbUrl = await ImageService.GetProvider( B ).GetImage();
				if ( string.IsNullOrEmpty( ThumbUrl ) )
				{
					QToken.TrySetResult( false );
					goto AwaitToken;
				}

				B.Info.CoverSrcUrl = ThumbUrl;
			}

			// Set the referer, as it is required by some site such as fanfiction.net
			new RuntimeCache( a =>
			{
				HttpRequest R = new WHttpRequest( a ) { EN_UITHREAD = false };

				if ( !string.IsNullOrEmpty( B.Info.OriginalUrl ) )
				{
					R.RequestHeaders.Referrer = new Uri( B.Info.OriginalUrl );
				}

				return R;
			} ).GET( new Uri( B.Info.CoverSrcUrl )
			, ( e, id ) =>
			{
				B.SaveCover( e.ResponseBytes );
				QToken.TrySetResult( true );
			}
			, ( c, i, ex ) => QToken.TrySetResult( false )
			, false );

			AwaitToken:
			return await QToken.Task;
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