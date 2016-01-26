using System;
using System.Threading.Tasks;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using wenku10;

namespace wenku8.Model.Loaders
{
    using Ext; 
    using Book;
    using Book.Spider;
    using Resources;
    using Settings;
    using System.Messages;

    class ChapterLoader
    {
        public static readonly string ID = typeof( ChapterLoader ).Name;

        public BookItem CurrentBook { get; private set; }

        private Action<Chapter> OnComplete;

        public bool ProtoMode { get; private set; }

        public ChapterLoader( BookItem b, Action<Chapter> CompleteHandler )
        {
            ProtoMode = true;
            CurrentBook = b;
            OnComplete = CompleteHandler;
        }

        public ChapterLoader( Action<Chapter> CompleteHandler = null )
        {
            ProtoMode = false;
            if( CompleteHandler == null )
            {
                OnComplete = x => { };
            }
            else
            {
                OnComplete = CompleteHandler;
            }
        }

        public async Task LoadAsync( Chapter C, bool Cache = true )
        {
            if ( Cache && Shared.Storage.FileExists( C.ChapterPath ) )
            {
                OnComplete( C );
            }
            else if ( C is SChapter )
            {
                // if this belongs to the spider
                SChapter SC = C as SChapter;
                await SC.SubProcRun( Cache );

                if ( SC.TempFile != null )
                {
                    await new ContentParser().OrganizeBookContent(
                        await SC.TempFile.ReadString()
                        , SC
                    );
                }

                OnComplete( C );
            }
            else
            {
                if ( !ProtoMode ) throw new InvalidOperationException( "ChapterLoader is in Bare mode" );
                IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );

                // Cancel thread if there is same job downloading
                App.RuntimeTransfer.CancelThread( C.ChapterPath );

                // Initiate download, precache should not be done internally.
                wCache.InitDownload(
                    C.ChapterPath
                    , X.Call<XKey[]>( XProto.WRequest, "GetBookContent", CurrentBook.Id, C.cid )

                    , async ( DRequestCompletedEventArgs e, string path ) =>
                    {
                        await new ContentParser().OrganizeBookContent( e.ResponseString, C );
                        OnComplete( C );
                    }

                    , ( string Request, string path, Exception ex ) =>
                    {
                        Logger.Log( ID, ex.Message, LogType.ERROR );
                        System.Utils.ShowError( () => { return new ErrorMessage().DOWNLOAD; } );
                        // OnComplete( C );
                    }

                    , false
                );
            }
        }

        public async void Load( Chapter C, bool Cache = true )
        {
            await LoadAsync( C, Cache );
        }
    }
}
