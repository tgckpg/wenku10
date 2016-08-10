using System;
using System.Collections.Generic;
using System.Net;
using Windows.Foundation;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.AdvDM;
using wenku8.Ext;
using wenku8.Resources;
using wenku8.Settings;
using wenku8.Model.REST;

namespace wenku10.SHHub
{
    sealed class SHMember : IMember
    {
        public static readonly string ID = typeof( SHMember ).Name;

        private RuntimeCache RCache = new RuntimeCache();

        public event TypedEventHandler<object, MemberStatus> OnStatusChanged;

        public bool IsLoggedIn { get; private set; }
        public bool WillLogin { get; private set; }

        public MemberStatus Status { get; set; }

        public string ServerMessage
        {
            get; private set;
        }

        private XRegistry AuthReg;

        public SHMember()
        {
            WillLogin = false;

            AuthReg = new XRegistry( "<SHAuth />", FileLinks.ROOT_SETTING + FileLinks.SH_AUTH_REG );

            XParameter MemberAuth = AuthReg.Parameter( "member-auth" );
            if ( MemberAuth != null ) RestoreAuth( MemberAuth );
        }

        public void Login( string Account, string Password )
        {
            WillLogin = true;
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.Login( Account, Password )
                , LoginResponse, LoginFailed
                , false
            );
        }

        public void Logout()
        {
            IsLoggedIn = false;
            UpdateStatus( MemberStatus.LOGGED_OUT );

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.Logout()
                , ClearAuth, ClearAuth
                , false
            );
        }

        private void UpdateStatus( MemberStatus Status )
        {
            this.Status = Status;
            if ( OnStatusChanged != null )
            {
                Net.Astropenguin.Helpers.Worker.UIInvoke( () => OnStatusChanged( this, Status ) );
            }
        }

        private void LoginResponse( DRequestCompletedEventArgs e, string id )
        {
            WillLogin = false;
            if ( e.ResponseHeaders[ "Set-Cookie" ] != null )
            {
                SaveAuth( e.Cookies );
                return;
            }

            try
            {
                JsonStatus.Parse( e.ResponseString );
            }
            catch( Exception ex )
            {
                ServerMessage = ex.Message;
            }

            UpdateStatus( MemberStatus.LOGGED_OUT );
        }

        private void LoginFailed( string CachedData, string id, Exception ex )
        {
            WillLogin = false;
            UpdateStatus( IsLoggedIn ? MemberStatus.LOGGED_IN : MemberStatus.LOGGED_OUT );
        }

        private void ValidateSession()
        {
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.SessionValid()
                , CheckResponse, ClearAuth
                , false
            );
        }

        private void CheckResponse( DRequestCompletedEventArgs e, string QueryId )
        {
            try
            {
                JsonStatus.Parse( e.ResponseString );
                IsLoggedIn = true;
                UpdateStatus( MemberStatus.LOGGED_IN );
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.DEBUG );
                ClearAuth();
                UpdateStatus( MemberStatus.RE_LOGIN_NEEDED );
            }
        }

        private void RestoreAuth( XParameter MAuth )
        {
            try
            {
                Cookie MCookie = new Cookie(
                    MAuth.GetValue( "name" )
                    , MAuth.GetValue( "domain" )
                    , MAuth.GetValue( "path" )
                );
                MCookie.Value = MAuth.GetValue( "value" );
                WHttpRequest.Cookies.Add( Shared.ShRequest.Server, MCookie );
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.WARNING );
            }

            ValidateSession();
        }

        private void SaveAuth( CookieCollection Cookies )
        {
            foreach ( Cookie cookie in Cookies )
            {
                if ( cookie.Name == "sid" )
                {
                    Logger.Log( ID, string.Format( "Set-Cookie: {0}=...", cookie.Name ), LogType.DEBUG );

                    XParameter MAuth = new XParameter( "member-auth" );
                    MAuth.SetValue( new XKey[] {
                        new XKey( "name", cookie.Name )
                        , new XKey( "domain" , cookie.Domain )
                        , new XKey( "path", cookie.Path )
                        , new XKey( "value" , cookie.Value )
                    } );

                    AuthReg.SetParameter( MAuth );
                    AuthReg.Save();
                    break;
                }
            }

            ValidateSession();
        }

        private void ClearAuth( string arg1, string arg2, Exception arg3 ) { ClearAuth(); }
        private void ClearAuth( DRequestCompletedEventArgs arg1, string arg2 ) { ClearAuth(); }
        private void ClearAuth()
        {
            WHttpRequest.Cookies = new CookieContainer();

            AuthReg.RemoveParameter( "member-auth" );
            AuthReg.Save();
        }
    }
}