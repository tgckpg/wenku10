using System;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

using libtaotu.Models.Procedure;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Loaders
{
    using AdvDM;
    using Ext;
    using Book;
    using Book.Spider;
    using ListItem;
    using Resources;
    using Settings;
    using System;
    using System.Messages;
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

            if( b.IsLocal )
            {
                OnComplete( b );
                return;
            }

            if ( b is BookInstruction )
            {
                LoadInstruction( ( BookInstruction ) b, useCache );
                return;
            }

            string id = b.Id;
            string Mode = X.Const<string>( XProto.WProtocols, "ACTION_BOOK_INFO" );

            if( CurrentBook.XTest( XProto.BookItemEx ) )
            {
                Mode = CurrentBook.XField<string>( "Mode" );
            }

            XKey[] ReqKeys = X.Call<XKey[]>( XProto.WRequest, "DoBookAction", Mode, id );
            if ( useCache )
            {
                string cacheName = X.Call<string>( XProto.WRuntimeCache, "GetCacheString", new object[] { ReqKeys } );
                if ( Shared.Storage.FileExists( FileLinks.ROOT_CACHE + cacheName ) )
                {
                    b.LastCache = Shared.Storage.FileTime( FileLinks.ROOT_CACHE + cacheName ).LocalDateTime;
                    ExtractBookInfo( Shared.Storage.GetString( FileLinks.ROOT_CACHE + cacheName ), id );
                    return;
                }
            }

            X.Instance<IRuntimeCache>( XProto.WRuntimeCache )
                .InitDownload( id, ReqKeys, PrelaodBookInfo, PrelaodBookInfo, true );
        }

        public async void LoadInstruction( BookInstruction B, bool useCache )
        {
            SpiderBook SBook = new SpiderBook( B );

            if ( Shared.Storage.FileExists( SBook.MetaLocation ) )
            {
                B.LastCache = Shared.Storage.FileTime( SBook.MetaLocation ).LocalDateTime;
            }

            if ( useCache && Shared.Storage.FileExists( B.TOCPath ) )
            {
                B.PackSavedVols( SBook.PSettings );
            }
            else
            {
                await SBook.Process();

                // Cannot download content, use cache if available
                if ( !( B.Packed == true || B.Packable ) && Shared.Storage.FileExists( B.TOCPath ) )
                {
                    Logger.Log( ID, "Spider failed to produce instructions, using cache instead", LogType.WARNING );
                    B.PackSavedVols( SBook.PSettings );
                }
            }

            if ( B.Packed != true && B.Packable )
            {
                B.PackVolumes( SBook.GetPPConvoy() );
            }

            await CacheCover( B );

            OnComplete( B );
        }

        public void LoadIntro( BookItem b, bool useCache = true )
        {
            if ( b is BookInstruction ) return;

            CurrentBook = b;

            if ( useCache && Shared.Storage.FileExists( b.IntroPath ) )
            {
                // This will trigger NotifyChanged for Intro
                // getter will automatically retieved intro stored in IntroPath
                CurrentBook.Intro = "OK";
            }
            else
            {
                X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
                    b.Id
                    , X.Call<XKey[]>( XProto.WRequest, "GetBookIntro", b.Id )
                    , SaveIntro, IntroFailed, false
                );
            }
        }

        private void IntroFailed( string arg1, string arg2, Exception arg3 )
        {
            if ( !Shared.Storage.FileExists( CurrentBook.IntroPath ) )
            {
                CurrentBook.Intro = new ErrorMessage().DOWNLOAD;
            }
        }

        private void SaveIntro( DRequestCompletedEventArgs e, string id )
        {
            Shared.Storage.WriteString( CurrentBook.IntroPath, Manipulation.PatchSyntax( e.ResponseString ) );
            CurrentBook.Intro = "OK";
        }

        private void PrelaodBookInfo( string cacheName, string id, Exception ex )
		{
			// This method is called when download is failed.
			Logger.Log( ID, "Download failed: " + cacheName, LogType.INFO );
			// Check if cache exist
			cacheName = Uri.EscapeDataString( cacheName );
			if ( Shared.Storage.FileExists( FileLinks.ROOT_CACHE + cacheName ) )
			{
                CurrentBook.LastCache = Shared.Storage.FileTime( FileLinks.ROOT_CACHE + cacheName ).LocalDateTime;
                // Should inform user would using previous cache as data.
                ExtractBookInfo( Shared.Storage.GetString( FileLinks.ROOT_CACHE + cacheName ), id );
				// MessageBox.Show( "Some information could not be downloaded, using previous cache." );
			}
			else
			{
				// Download failed and no cache is available.
				// Inform user there is a network problem
				// MessageBox.Show( "Some information could not be downloaded, please try again later." );
				OnComplete( null );
			}
		}

		private void PrelaodBookInfo( DRequestCompletedEventArgs e, string id )
		{
            CurrentBook.LastCache = DateTime.Now;
			ExtractBookInfo( e.ResponseString, id );
		}

        private void ExtractBookInfo( string InfoData, string id )
        {
            CurrentBook.ParseXml( InfoData );

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

        private async Task CacheCover( BookItem B )
        {
            if( Shared.Storage.FileExists( CurrentBook.CoverPath ) ) return;

            if( !string.IsNullOrEmpty( B.CoverSrcUrl ) )
            {
                TaskCompletionSource<int> Awaitable = new TaskCompletionSource<int>();

                // Set the referer, as it is required by some site such as fanfiction.net
                new RuntimeCache( a => {
                    HttpRequest R = new WHttpRequest( a );
                    R.EN_UITHREAD = true;

                    if ( !string.IsNullOrEmpty( B.OriginalUrl ) )
                    {
                        R.RequestHeaders[ HttpRequestHeader.Referer ] = B.OriginalUrl;
                    }

                    return R;
                } ).GET( new Uri( B.CoverSrcUrl ), ( a, b ) => {
                    CoverDownloaded( a, b );
                    Awaitable.TrySetResult( 0 );
                }
                // Failed handler
                , ( a, b, c ) => {
                    Awaitable.TrySetResult( 0 );
                }, false );

                await Awaitable.Task;
            }
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