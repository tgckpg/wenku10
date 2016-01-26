using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Loader
{
    using Ext;
    using Topics;
    using ListItem;

    class NewsLoader : ActiveData
    {
        private const string BULLETIN = "https://blog.astropenguin.net/rss/bulletin/uwp+wenku8+"
#if TESTING || DEBUG
        , BULLETIN_CH = "testing channel"
#elif BETA
        , BULLETIN_CH = "beta channel"
#else
        , BULLETIN_CH = "production channel"
#endif
        , BULLETIN_ALL = "all channel"
            ;

        public static readonly string ID = typeof( Announcements ).Name;

        public bool HasNewThings = false;

        public NewsLoader()
        {

        }

        public async Task Load()
        {
            IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, false, false );

            TaskCompletionSource<int> TCS = new TaskCompletionSource<int>();

            List<Announcements> News = new List<Announcements>();
            int i = 0;
            wCache.GET(
                new Uri( BULLETIN + BULLETIN_CH )
                , ( DRequestCompletedEventArgs e, string id ) =>
                {
                    News.Add( new Announcements( e.ResponseString ) );
                    if ( ++i == 2 ) TCS.SetResult( i );
                }
                , ( string id, string url, Exception ex ) =>
                {
                    PushItem();
                    if ( ++i == 2 ) TCS.SetResult( i );
                }
                , false );

            wCache.GET(
                new Uri( BULLETIN + BULLETIN_ALL )
                , ( DRequestCompletedEventArgs e, string id ) =>
                {
                    News.Add( new Announcements( e.ResponseString ) );
                    if ( ++i == 2 ) TCS.SetResult( i );
                }
                , ( string id, string url, Exception ex ) =>
                {
                    PushItem();
                    if ( ++i == 2 ) TCS.SetResult( i );
                }
                , false );


            await TCS.Task;

            foreach( Announcements A in News )
            {
                PushItem( A );
            }
        }

        private void PushItem( Announcements Pull )
        {
            Announcements Local = new Announcements();
            IEnumerable<Topic> NewTopics = Pull.Topics.Except( Local.Topics, new GDiff() );

            bool IsLocalNew = Local.Topics.Count() == 0;

            foreach( Topic C in NewTopics )
            {
                Local.MarkNew( Pull.GetItem( C.Payload ) );
            }

            if ( IsLocalNew )
            {
                Pull.Save();
                // Drop the Pulled data,  use locally parsed one instead
                Local = new Announcements();
            }
            else
            {
                Local.Save();
            }

            HasNewThings = Local.IsNew;
        }

        private void PushItem()
        {
            PushItem( new Announcements() );
        }

        private class GDiff : IEqualityComparer<Topic>
        {
            public bool Equals( Topic x, Topic y )
            {
                return x.Payload == y.Payload;
            }

            public int GetHashCode( Topic obj )
            {
                return 0;
            }
        }
    }

}
