using System;

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

    class BookLoader
    {
        public static readonly string ID = typeof( BookLoader ).Name;

        private BookItem CurrentBook;

        private Action<BookItem> CompleteHandler;

        public BookLoader( Action<BookItem> Handler )
        {
            CompleteHandler = Handler;
        }

        public void Load( BookItem b, bool useCache = false )
        {
            CurrentBook = b;

            if( b.IsLocal )
            {
                Net.Astropenguin.Helpers.Worker.UIInvoke( () => CompleteHandler( b ) );
                return;
            }

            if ( b is BookInstruction )
            {
                if ( useCache && Shared.Storage.FileExists( b.TOCPath ) )
                {
                    CompleteHandler( b );
                }
                else
                {
                    LoadInstruction( b as BookInstruction );
                }
                return;
            }

            string id = b.Id;
            string Mode = X.Static<string>( "wenku8.System.WProtocols", "ACTION_BOOK_INFO" );

            if( CurrentBook.XTest( XProto.BookItemEx ) )
            {
                Mode = CurrentBook.XProp<string>( "Mode" );
            }

            XKey[] ReqKeys = X.Call<XKey[]>( XProto.WRequest, "DoBookAction", Mode, id );
            if ( useCache )
            {
                string cacheName = X.Call<string>( XProto.WRuntimeCache, "GetCacheString", ReqKeys );
                if ( Shared.Storage.FileExists( FileLinks.ROOT_CACHE + cacheName ) )
                {
                    ExtractBookInfo( Shared.Storage.GetString( FileLinks.ROOT_CACHE + cacheName ), id );
                    return;
                }
            }
            X.Instance<IRuntimeCache>( XProto.WRuntimeCache )
                .InitDownload( id, ReqKeys, PrelaodBookInfo, PrelaodBookInfo, true );
        }

        public async void LoadInstruction( BookInstruction B )
        {
            if ( !B.Packable )
            {
                SpiderBook SBook = new SpiderBook( B );
                await SBook.Process();
            }

            B.PackVolumes();
            CompleteHandler( B );
        }

        public void LoadIntro( BookItem b, bool useCache = true )
        {
            if ( b is BookInstruction ) return;

            CurrentBook = b;
            // Description
            if ( Shared.Storage.FileExists( b.IntroPath ) )
            {
                CurrentBook.Intro = "OK";
            }
            else
            {
                X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
                    b.Id
                    , X.Call<XKey[]>( XProto.WRequest, "GetBookIntro", b.Id )
                    , SaveIntro, IntroFailed, useCache
                );
            }
        }

        private void IntroFailed( string arg1, string arg2, Exception arg3 )
        {
            CurrentBook.Intro = new ErrorMessage().DOWNLOAD;
        }

        private void SaveIntro( DRequestCompletedEventArgs e, string id )
        {
            Shared.Storage.WriteString( CurrentBook.IntroPath, e.ResponseString );
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
				// Should inform user would using previous cache as data.
				ExtractBookInfo( Shared.Storage.GetString( FileLinks.ROOT_CACHE + cacheName ) , id );
				// MessageBox.Show( "Some information could not be downloaded, using previous cache." );
			}
			else
			{
				// Download failed and no cache is available.
				// Inform user there is a network problem
				// MessageBox.Show( "Some information could not be downloaded, please try again later." );
				CompleteHandler( null );
			}
		}

		private void PrelaodBookInfo( DRequestCompletedEventArgs e, string id )
		{
			// When download is successful
			ExtractBookInfo( e.ResponseString, id );
		}

        private async void ExtractBookInfo( string InfoData, string id )
        {
            ////// App-specific approach
            CurrentBook.ParseXml( InfoData );

            if ( !Shared.Storage.FileExists( CurrentBook.CoverPath ) )
            {
                ///// App-specific approach
                X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
                    id, X.Call<XKey[]>( XProto.WRequest, "GetBookCover", id )
                    ,cacheCover, Utils.DoNothing, false
                );
            }
            else
            {
                CurrentBook.Cover = await Image.GetSourceFromUrl( CurrentBook.CoverPath );
                // Cover cached immediately. Call once
                CompleteHandler( CurrentBook );
            }
            ////////// Active informations: Can not store in AppCache
        }

        private async void cacheCover( DRequestCompletedEventArgs e, string id )
        {
            // Write Cache
            Shared.Storage.WriteBytes( CurrentBook.CoverPath, e.ResponseBytes );
            // Read Image
            CurrentBook.Cover = await Image.GetSourceFromUrl( CurrentBook.CoverPath );
            // Cover cached. Call once
            CompleteHandler( CurrentBook );
        }
    }
}
