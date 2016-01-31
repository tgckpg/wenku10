using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.UI;

namespace wenku8.Model.Section
{
    using Ext;
    using Resources;
    using Settings;

    class LoginStatus : ActiveData
    {
        public Page _frameContent;
        public Page FrameContent
        {
            get
            {
                return _frameContent;
            }
            private set
            {
                _frameContent = value;
                NotifyChanged( "FrameContent" );
            }
        }

        private const string AvatarLocation = FileLinks.ROOT_IMAGE + "USER_AVATAR";

        public BitmapImage Avatar { get; private set; }

        private ControlState _pagestatus = ControlState.Foreatii;
        public ControlState PageStatus
        {
            get
            {
                return _pagestatus;
            }
            private set
            {
                _pagestatus = value;
                NotifyChanged( "PageStatus" );
            }
        }

        private string _loginOrInfo;
        public string LoginOrInfo
        {
            get
            {
                return _loginOrInfo;
            }
            private set
            {
                _loginOrInfo = value;
                NotifyChanged( "LoginOrInfo" );
            }
        }

        private IMember Member;

        public LoginStatus()
        {
            Avatar = new BitmapImage();
            Member = X.Singleton<IMember>( XProto.Member );
            Member_OnStatusChanged();
            Member.OnStatusChanged += Member_OnStatusChanged;
        }

        ~LoginStatus()
        {
            Member.OnStatusChanged -= Member_OnStatusChanged;
        }

        private void Member_OnStatusChanged()
        {
            StringResources stx = new StringResources();
            LoginOrInfo = Member.IsLoggedIn
                ? stx.Text( "Login_AccountName" )
                : stx.Text( "Login" )
                ;

            TryLoadAvatar();

            if( !Member.IsLoggedIn )
            {
                if ( Member.WillLogin ) return;

                Avatar = null;
                NotifyChanged( "Avatar" );
                return;
            }

            X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, false )
                .InitDownload(
                "USER_AVATAR"
                , X.Call<XKey[]>( XProto.WRequest, "GetUserAvatar" )
                , AvatarLoaded
                , ( string id, string url, Exception ex ) => { TryLoadAvatar(); }
                , false
            );
        }


        private async void TryLoadAvatar()
        {
            if ( !Shared.Storage.FileExists( AvatarLocation ) ) return;

            Avatar.SetSourceFromUrl( AvatarLocation );
            NotifyChanged( "Avatar" );
        }

        private void AvatarLoaded( DRequestCompletedEventArgs e, string id )
        {
            Avatar.SetSourceFromUrl( null );
            NotifyChanged( "Avatar" );

            Shared.Storage.WriteBytes( AvatarLocation, e.ResponseBytes );
        }

        public async void PopupLoginOrInfo()
        {
            if ( Member.IsLoggedIn )
            {
                FrameContent = new wenku10.Pages.Account( ClosePopup );
                PageStatus = ControlState.Reovia;
            }
            else
            {
                wenku10.Pages.Dialogs.Login Login = new wenku10.Pages.Dialogs.Login();
                await Popups.ShowDialog( Login );
            }
        }

        private void ClosePopup()
        {
            FrameContent = null;
            PageStatus = ControlState.Foreatii;
        }
    }
}
