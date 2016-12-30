using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.Services.Twitter;

using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Loaders
{
    sealed class TwitterLoader : ILoader<Tweet>
    {
        public static readonly string ID = typeof( TwitterLoader ).Name;

        public static string KEY { get; set; }
        public static string SECRET { get; set; }
        public static string CALLBACK_URI { get; set; }

        public bool Valid { get; private set; }

        public string[] Tags { get; set; }

        public async Task Authenticate()
        {
            TwitterService.Instance.Initialize( KEY, SECRET, CALLBACK_URI );
            Valid = await TwitterService.Instance.LoginAsync();
        }

        public Action<IList<Tweet>> Connector { get; set; }

        public int CurrentPage
        {
            get
            {
                if ( !Valid ) return 0;
                return 0;
            }
        }

        public bool PageEnded
        {
            get
            {
                if ( !Valid ) return true;
                return false;
            }
        }

        private string SinceId;

        public async Task<IList<Tweet>> NextPage( uint count )
        {
            try
            {
                TwitterDataConfig TDC = new TwitterDataConfig();
                TDC.QueryType = TwitterQueryType.Search;
                TDC.Query = NextQuery();

                List<Tweet> Tweets = await TwitterService.Instance.RequestAsync( TDC );

                if( Tweets.Any() )
                {
                    SinceId = "since_id: " + Tweets.Last().Id;
                }

                return Tweets.ToList();
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.WARNING );
            }

            return new Tweet[ 0 ];
        }

        private string NextQuery()
        {
            string TagsQ = "#" + string.Join( " #", Tags );
            string PageQ = SinceId + " ";

            return TagsQ + " " + PageQ;
        }

    }
}