using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Loaders;

using libtaotu.Controls;
using libtaotu.Crawler;
using libtaotu.Models.Procedure;

namespace wenku8.Model.Loaders
{
    sealed class ZSFeedbackLoader<T> : ILoader<T>
    {
        public static readonly string ID = typeof( ZSFeedbackLoader<T> ).Name;

        public Action<IList<T>> Connector { get; set; }

        public int CurrentPage { get; set; }

        public bool PageEnded { get; private set; }

        private ProceduralSpider Spider;

        private string FeedParam = null;
        private bool FirstLoad = true;

        public ZSFeedbackLoader( ProceduralSpider Spider )
        {
            this.Spider = Spider;
        }

        public async Task<IList<T>> NextPage( uint ExpectedCount = 30 )
        {
            TaskCompletionSource<T[]> Ts = new TaskCompletionSource<T[]>();

            try
            {
                ProcPassThru RunMode;
                if ( FirstLoad )
                {
                    FirstLoad = false;
                    RunMode = new ProcPassThru( null );
                }
                else
                {
                    RunMode = new ProcPassThru( null, ProcType.FEED_RUN );
                }

                ProcConvoy Convoy = await Spider.Crawl( new ProcConvoy( RunMode, FeedParam ) );
                FeedParam = null;

                if ( Convoy.Payload is IEnumerable<IStorageFile> )
                {
                    FeedParam = await ( ( IEnumerable<IStorageFile> ) Convoy.Payload ).FirstOrDefault()?.ReadString();
                }
                else if ( Convoy.Payload is IEnumerable<string> )
                {
                    FeedParam = ( ( IEnumerable<string> ) Convoy.Payload ).FirstOrDefault();
                }
                else if ( Convoy.Payload is IStorageFile )
                {
                    FeedParam = await ( ( IStorageFile ) Convoy.Payload ).ReadString();
                }
                else if ( Convoy.Payload is string )
                {
                    FeedParam = ( string ) Convoy.Payload;
                }

                Convoy = ProcManager.TracePackage( Convoy, ( P, C ) => C.Payload is IEnumerable<T> );

                if ( Convoy != null )
                {
                    IEnumerable<T> Items = ( IEnumerable<T> ) Convoy.Payload;
                    Ts.SetResult( Items.ToArray() );
                }
                else
                {
                    Ts.TrySetResult( new T[ 0 ] );
                }
            }
            catch ( Exception ex )
            {
                Ts.TrySetResult( new T[ 0 ] );
                Logger.Log( ID, ex.Message, LogType.WARNING );
            }

            T[] Cs = await Ts.Task;

            PageEnded = ( Cs.Length == 0 );
            return Cs;
        }

    }
}