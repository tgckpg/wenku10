using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;

using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Loaders
{
    using AdvDM;
    using ListItem.Sharers;
    using Resources;
    using REST;

    sealed class SHSearchLoader : ILoader<HubScriptItem>
    {
        public static readonly string ID = typeof( SHSearchLoader ).Name;

        public Action<IList<HubScriptItem>> Connector { get; set; }

        public bool PageEnded { get; private set; }
        public int CurrentPage { get; private set; }

        private string Query;
        private IEnumerable<string> AccessTokens;

        private RuntimeCache RCache = new RuntimeCache();

        public SHSearchLoader( string Query, IEnumerable<string> AccessTokens )
        {
            this.AccessTokens = AccessTokens;
            this.Query = Query;
        }

        public async Task<IList<HubScriptItem>> NextPage( uint ExpectedCount = 0 )
        {
            TaskCompletionSource<HubScriptItem[]> HSItems = new TaskCompletionSource<HubScriptItem[]>();

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.Search( Query, CurrentPage, ExpectedCount, AccessTokens )
                , ( e, Id ) =>
                {
                    try
                    {
                        JsonObject JResponse = JsonStatus.Parse( e.ResponseString );
                        JsonArray JHS = JResponse.GetNamedArray( "data" );

                        int LoadedCount = JHS.Count();

                        PageEnded = LoadedCount < ExpectedCount;
                        CurrentPage += LoadedCount;

                        HSItems.SetResult( JHS.Remap( x => HubScriptItem.Create( x.GetObject() ) ).ToArray() );
                    }
                    catch ( Exception ex )
                    {
                        Logger.Log( ID, ex.Message, LogType.WARNING );
                        PageEnded = true;
                        HSItems.TrySetResult( new HubScriptItem[ 0 ] );
                    }
                }
                , ( cacheName, Id, ex ) =>
                {
                    Logger.Log( ID, ex.Message, LogType.WARNING );
                    PageEnded = true;
                    HSItems.TrySetResult( new HubScriptItem[ 0 ] );
                }
                , false
            );

            return await HSItems.Task;
        }
    }

}