using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Loaders;

using wenku8.Ext;
using wenku8.Config;

namespace wenku10.Pages.Dialogs
{
    public sealed partial class Login : ContentDialog
    {
        public bool Canceled = true;

        private IMember Member;

        public Login()
        {
            this.InitializeComponent();

            Member = X.Singleton<IMember>( XProto.Member );

            StringResources stx = new StringResources();
            PrimaryButtonText = stx.Text( "Login" );
            SecondaryButtonText = stx.Text( "Button_Back");

            Member.OnStatusChanged += Member_StatusUpdate;
        }

        void Member_StatusUpdate()
        {
            if ( Member.IsLoggedIn )
            {
                if ( IsRemember.IsChecked == true )
                {
                    Properties.ACCOUNT_NAME = Account.Text;
                    Properties.ACCOUNT_PASSWD = Password.Password;
                }
                Hide();
            }
            else
            {
                IsPrimaryButtonEnabled
                    = IsSecondaryButtonEnabled
                    = Account.IsEnabled
                    = Password.IsEnabled
                    = true
                    ;
                Account.Focus( FocusState.Keyboard );
            }
        }

        ~Login()
        {
            Member.OnStatusChanged -= Member_StatusUpdate;
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            args.Cancel = true;
            if ( Member.WillLogin || Member.IsLoggedIn ) return;

            DetectInputLogin();
            Canceled = false;
        }

        private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            if ( Member.WillLogin || Member.IsLoggedIn )
            {
                args.Cancel = true;
            }
        }

        private void OnKeyDown( object sender, KeyRoutedEventArgs e )
        {
            if ( e.Key == Windows.System.VirtualKey.Enter )
            {
                DetectInputLogin();
            }
        }

        private void DetectInputLogin()
        {
            string Name = Account.Text.Trim();
            string Passwd = Password.Password;

            if ( string.IsNullOrEmpty( Name ) || String.IsNullOrEmpty( Passwd ) )
            {
                if ( string.IsNullOrEmpty( Name ) )
                {
                    Account.Focus( FocusState.Keyboard );
                }
                else
                {
                    Password.Focus( FocusState.Keyboard );
                }
                return;
            }
            else
            {
                IsPrimaryButtonEnabled
                    = IsSecondaryButtonEnabled
                    = Account.IsEnabled
                    = Password.IsEnabled
                    = false
                    ;

                // Re-focus to disable keyboard
                this.Focus( FocusState.Pointer );
                // Request string
                Member.Login( Name, Passwd );
            }
        }
    }
}
