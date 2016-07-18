using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

using Net.Astropenguin.Logging;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Linq;

namespace wenku8.Model.Comments
{
    using AdvDM;
    using Resources;
    using REST;

    sealed class HSCommentLoader : ILoader<HSComment>
    {
        public static readonly string ID = typeof( HSCommentLoader ).Name;

        public Action<IList<HSComment>> Connector { get; set; }

        public int CurrentPage { get; set; }

        public bool PageEnded { get; private set; }

        private string Id;
        private SharersRequest.CommentTarget Target;

        private RuntimeCache RCache;

        public HSCommentLoader( string Id, SharersRequest.CommentTarget Target )
        {
            RCache = new RuntimeCache();
            this.Id = Id;
            this.Target = Target;
        }

        public async Task<IList<HSComment>> NextPage( uint ExpectedCount = 30 )
        {
            TaskCompletionSource<HSComment[]> HSComments = new TaskCompletionSource<HSComment[]>();

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.GetComments( Target, CurrentPage, ExpectedCount, Id )
                , ( e, Id ) =>
                {
                    try
                    {
                        JsonObject JObj = JsonStatus.Parse( e.ResponseString );
                        JsonArray JComms = JObj.GetNamedArray( "data" );

                        int LoadedCount = JComms.Count();

                        List<HSComment> HSC = new List<HSComment>( LoadedCount );
                        foreach( JsonValue ItemDef in JComms )
                        {
                            HSC.Add( new HSComment( ItemDef.GetObject() ) );
                        }

                        PageEnded = LoadedCount < ExpectedCount;
                        CurrentPage += LoadedCount;

                        HSComments.SetResult( HSC.Flattern( x => x.Replies ) );
                    }
                    catch ( Exception ex )
                    {
                        Logger.Log( ID, ex.Message, LogType.WARNING );
                        PageEnded = true;

                        HSComments.TrySetResult( new HSComment[ 0 ] );
                    }
                }
                , ( cacheName, Id, ex ) =>
                {
                    Logger.Log( ID, ex.Message, LogType.WARNING );
                    PageEnded = true;
                }
                , false
            );

            HSComment[] Cs = await HSComments.Task;
            return Cs;
        }
    }
}
