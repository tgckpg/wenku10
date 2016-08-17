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

namespace wenku10.Pages.Dialogs
{
    sealed partial class Login : ContentDialog
    {
        public bool Canceled = true;

        private IMember Member;

        public Login( IMember Member )
        {
            this.InitializeComponent();
            this.Member = Member;

            StringResources stx = new StringResources();
            PrimaryButtonText = stx.Text( "Login" );
            SecondaryButtonText = stx.Text( "Button_Back" );

            if ( Member.Status == MemberStatus.RE_LOGIN_NEEDED )
            {
                ShowMessage( stx.Text( "Login_Expired" ) );
            }

            Member.OnStatusChanged += Member_StatusUpdate;

            if( Member.CanRegister )
            {
                RegisterBtn.Visibility = Visibility.Visible;
            }
        }

        void Member_StatusUpdate( object sender, MemberStatus st )
        {
            if ( Member.IsLoggedIn )
            {
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

                ShowMessage( Member.ServerMessage );
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
                e.Handled = DetectInputLogin();
            }
        }

        private bool DetectInputLogin()
        {
            string Name = Account.Text.Trim();
            string Passwd = Password.Password;

            if ( string.IsNullOrEmpty( Name ) || string.IsNullOrEmpty( Passwd ) )
            {
                if ( string.IsNullOrEmpty( Name ) )
                {
                    Account.Focus( FocusState.Keyboard );
                }
                else
                {
                    Password.Focus( FocusState.Keyboard );
                }
                return false;
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

                return true;
            }
        }

        private void ShowMessage( string Mesg )
        {
            if ( Mesg == null ) return;

            ServerMessage.Text = Mesg;
            ServerMessage.Visibility = Visibility.Visible;
        }

        private void RegisterBtn_Click( object sender, RoutedEventArgs e )
        {
            this.Hide();
            Member.Register();
        }
    }
}