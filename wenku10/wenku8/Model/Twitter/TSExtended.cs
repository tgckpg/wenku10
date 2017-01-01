using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace wenku8.Model.Twitter
{
    using Ext;

    sealed class TSExtended
    {
        private const string BaseUrl = "https://api.twitter.com/1.1";

        private static TSExtended _Instance;
        public static TSExtended Instance
        {
            get { return _Instance ?? ( _Instance = new TSExtended() ); }
        }

        public async Task<bool> ReplyStatusAsync( string StatusText, string TweetId )
        {
            object OAuthRequest = X.Instance<object>( "Microsoft.Toolkit.Uwp.Services.Twitter.TwitterOAuthRequest, Microsoft.Toolkit.Uwp.Services" );

            string Result = await OAuthRequest.XCallAsync<string>(
                "ExecutePostAsync"
                , new Uri( $"{BaseUrl}/statuses/update.json?status={Uri.EscapeDataString( StatusText )}&in_reply_to_status_id={TweetId}" )
                , AuthData.Token );

            // XXX: Will need to impl later. But for now let's just assume it's true
            return true;
        }

    }
}