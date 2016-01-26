using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace wenku8.Model.Loaders
{
    using Ext;
    using Book;
    using Book.Spider;
    using Resources;
    using Settings;

    class VolumeLoader
    {
        public static readonly string ID = typeof( VolumeLoader ).Name;

        public BookItem CurrentBook { get; private set; }

        private Action<BookItem> OnComplete;

        public VolumeLoader( Action<BookItem> CompleteHandler )
        {
            OnComplete = CompleteHandler;
        }

        public void Load( BookItem b )
        {
            CurrentBook = b;
            if ( b.IsLocal || Shared.Storage.FileExists( CurrentBook.TOCPath ) )
            {
                OnComplete( b );
            }
            else if ( b is BookInstruction )
            {
                LoadInst( b as BookInstruction );
            }
            else // wenku8 Protocol
            {
                IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
                // This occurs when tapping pinned book but cache is cleared
                wCache.InitDownload(
                    CurrentBook.Id
                    , X.Call<XKey[]>( XProto.WRequest, "GetBookTOC", CurrentBook.Id )
                    , ( DRequestCompletedEventArgs e, string id ) =>
                    {
                        Shared.Storage.WriteString( CurrentBook.TOCPath, e.ResponseString );
                        Shared.Storage.WriteString( CurrentBook.TOCDatePath, CurrentBook.RecentUpdateRaw );
                        OnComplete( b );
                    }
                    , ( string RequestURI, string id, Exception ex ) =>
                    {
                        OnComplete( b );
                    }
                    , false
                );
            }
        }

        public async void LoadInst( BookInstruction b )
        {
            IEnumerable<SVolume> Vols = b.GetVolumes().Cast<SVolume>();
            foreach ( SVolume Vol in Vols )
            {
                // This should finalize the chapter info
                await Vol.SubProcRun( b );
            }

            await b.CompileTOC( Vols );
            OnComplete( b );
        }

    }
}
