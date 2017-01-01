using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.Services.Twitter;

namespace wenku8.Model.Twitter
{
    sealed class AuthData
    {
        public static string KEY { get; set; }
        public static string SECRET { get; set; }
        public static string CALLBACK_URI { get; set; }

        private static TwitterOAuthTokens _Token = null;
        public static TwitterOAuthTokens Token
        {
            get
            {
                if ( _Token == null )
                {
                    _Token = new TwitterOAuthTokens()
                    {
                        ConsumerKey = KEY
                        , ConsumerSecret = SECRET
                        , CallbackUri = CALLBACK_URI
                    };

                    KEY = SECRET = CALLBACK_URI = null;
                }

                return _Token;
            }
        }

    }
}