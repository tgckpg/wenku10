using System;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku10;

namespace wenku8.Model.Loaders
{
    using Book;
    using Ext;
    using Resources;

    class AutoCache : ActiveData, IAutoCache
    {
        public static readonly string ID = typeof( AutoCache ).Name;

        private const int AutoLimit = 2;
        private static int CurrentCount = 1;

        IRuntimeCache wCache;
        // FavItem fitm;
        BookItem ThisBook;
        EpisodeStepper ES;
        Action<BookItem> OnComplete;

        public string StatusText { get; private set; }

        public AutoCache( BookItem b, Action<BookItem> Handler )
            :this()
        {
            // fitm = f;
            ThisBook = b;
            OnComplete = Handler;

            StatusText = "Ready";
            if( CurrentCount < AutoLimit )
            {
                wCache.InitDownload(
                    ThisBook.Id
                    , X.Call<XKey[]>( XProto.WRequest, "GetBookTOC", ThisBook.Id )
                    , cacheInfo, cacheInfoFailed, false );
            }
        }

        private void cacheInfo( DRequestCompletedEventArgs e, string id )
        {
            Shared.Storage.WriteString( ThisBook.TOCPath, e.ResponseString );
            Shared.Storage.WriteString( ThisBook.TOCDatePath, ThisBook.RecentUpdateRaw );

            StepAutomation();
        }

        private async void StepAutomation()
        {
            ES = new EpisodeStepper( new VolumesInfo( ThisBook ) );

            if( AutoLimit < CurrentCount )
            {
                DispLog( string.Format( "Error: Limit Reached {0}/{1}", CurrentCount - 1, AutoLimit ) );
                return;
            }

            try
            {
                bool NotCached = false;
                for ( ES.Rewind(); ES.NextStepAvailable(); ES.stepNext() )
                {
                    Chapter C = new Chapter( ES.currentEpTitle, ThisBook.Id, ES.currentVid, ES.currentCid );
                    if ( !C.IsCached )
                    {
                        if ( !NotCached ) CurrentCount++;

                        NotCached = true;
                        // Register backgrountd transfer
                        await Task.Delay( TimeSpan.FromMilliseconds( 80 ) );

                        XKey[] Request = X.Call<XKey[]>( XProto.WRequest, "GetBookContent", ThisBook.Id, ES.currentCid );

                        DispLog( ES.CurrentVolTitle + "[" + ES.currentEpTitle + "]" );
                        App.RuntimeTransfer.RegisterRuntimeThread(
                            Request
                            , C.ChapterPath, Guid.NewGuid()
                            , Uri.EscapeDataString( ThisBook.Title ) + "&" + Uri.EscapeDataString( ES.CurrentVolTitle ) + "&" + Uri.EscapeDataString( ES.currentEpTitle )
                            , new XKey( "TOKENILLS", C.IllustrationPath )
                        );
                    }
                }
            }
            catch( Exception ex )
            {
                global::System.Diagnostics.Debugger.Break();
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }

            App.RuntimeTransfer.StartThreadCycle( LoadComplete );
            DispLog( "Complete" );

            OnComplete( ThisBook );
        }

        public AutoCache()
        {
            wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, false, false );

            // This runs when thread is being aborted when app quit
            if ( App.RuntimeTransfer.CurrentThread != null )
            {
                Logger.Log( ID, "Resuming Download Sessions ...", LogType.INFO );
                App.RuntimeTransfer.StartThreadCycle( LoadComplete );
            }
        }

        // Thread Complete Processor
        public static void LoadComplete( DRequestCompletedEventArgs e, XParameter PArgs )
        {
            new ContentParser().OrganizeBookContent( e.ResponseString, PArgs.ID, PArgs.GetValue( "TOKENILLS" ) );
        }

        private void cacheInfoFailed( string cacheName, string id, Exception ex )
        {
            if ( Shared.Storage.FileExists( ThisBook.TOCPath ) )
            {
                StepAutomation();
            }
        }

        private void DispLog( string p )
        {
            Logger.Log( ID, p, LogType.DEBUG );
            Worker.UIInvoke( () =>
            {
                StatusText = p;
                NotifyChanged( "StatusText" );
            } );
        }

        internal static void DownloadVolume( BookItem ThisBook, Volume Vol )
        {
            Worker.ReisterBackgroundWork( () =>
            {
                string id = ThisBook.Id;
                string CVid = Vol.vid;
                foreach ( Chapter c in Vol.ChapterList )
                {
                    if ( !c.IsCached )
                    {
                        Logger.Log( ID, "Registering: " + c.ChapterTitle, LogType.DEBUG );
                        App.RuntimeTransfer.RegisterRuntimeThread(
                            X.Call<XKey[]>( XProto.WRequest, "GetBookContent", id, c.cid )
                            , c.ChapterPath, Guid.NewGuid()
                            , Uri.EscapeDataString( ThisBook.Title ) + "&" + Uri.EscapeDataString( Vol.VolumeTitle ) + "&" + Uri.EscapeDataString( c.ChapterTitle )
                            , new XKey( "TOKENILLS", c.IllustrationPath )
                        );
                    }
                }

                App.RuntimeTransfer.StartThreadCycle( ( a, b ) =>
                {
                    LoadComplete( a, b );
                    Worker.UIInvoke( () => { foreach ( Chapter C in Vol.ChapterList ) C.UpdateStatus(); } );
                } );
                App.RuntimeTransfer.ResumeThread();
            } );
        }
    }
}
