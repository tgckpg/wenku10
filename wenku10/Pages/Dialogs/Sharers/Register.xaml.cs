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

using wenku8.AdvDM;
using wenku8.Resources;
using wenku8.Model.REST;

namespace wenku10.Pages.Dialogs.Sharers
{
    public sealed partial class Register : ContentDialog
    {
        public bool Canceled = true;

        public Register()
        {
            this.InitializeComponent();

            StringResources stx = new StringResources( "AppResources", "ContextMenu" );
            PrimaryButtonText = stx.Text( "Register", "ContextMenu" );
            SecondaryButtonText = stx.Text( "Button_Back");
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            args.Cancel = true;

            DetectInputLogin();
            Canceled = false;
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
            string PasswdV = PasswordV.Password;
            string Email = EmailInput.Text.Trim();

            if ( string.IsNullOrEmpty( Name ) || string.IsNullOrEmpty( Passwd ) || string.IsNullOrEmpty( PasswdV ) || string.IsNullOrEmpty( Email ) )
            {
                if ( string.IsNullOrEmpty( Name ) )
                {
                    Account.Focus( FocusState.Keyboard );
                }
                else if ( string.IsNullOrEmpty( Passwd ) )
                {
                    Password.Focus( FocusState.Keyboard );
                }
                else if( string.IsNullOrEmpty( PasswdV ) )
                {
                    PasswordV.Focus( FocusState.Keyboard );
                }
                else if( string.IsNullOrEmpty( Email ) )
                {
                    EmailInput.Focus( FocusState.Keyboard );
                }
            }
            else if( Passwd != PasswdV )
            {
                StringResources stx = new StringResources( "Error" );
                ServerMessage.Text = stx.Str( "PasswordMismatch" );
                Password.Focus( FocusState.Keyboard );
            }
            else
            {
                ServerMessage.Text = "";

                IsPrimaryButtonEnabled
                    = IsSecondaryButtonEnabled
                    = Account.IsEnabled
                    = Password.IsEnabled
                    = PasswordV.IsEnabled
                    = EmailInput.IsEnabled
                    = false
                    ;

                this.Focus( FocusState.Pointer );

                IndicateLoad();

                RuntimeCache RCache = new RuntimeCache();
                RCache.POST(
                    Shared.ShRequest.Server
                    , Shared.ShRequest.Register( Name, Passwd, Email )
                    , RequestComplete, RequestFailed, false );
            }
        }

        private void RequestFailed( string cacheName, string QueryId, Exception e )
        {
            IndicateIdle();
            ErrorMessage( "Server error, please try again later" );
        }

        private void RequestComplete( DRequestCompletedEventArgs e, string QueryId )
        {
            IndicateIdle();
            try
            {
                JsonStatus.Parse( e.ResponseString );
                Canceled = false;
                Hide();
            }
            catch ( Exception ex )
            {
                ErrorMessage( ex.Message );
                Account.Focus( FocusState.Keyboard );
            }
        }

        private void ErrorMessage( string Mesg )
        {
            ServerMessage.Text = Mesg;

            IsPrimaryButtonEnabled
                = IsSecondaryButtonEnabled
                = Account.IsEnabled
                = Password.IsEnabled
                = PasswordV.IsEnabled
                = EmailInput.IsEnabled
                = true
                ;
        }

        private void IndicateLoad()
        {
            StringResources stx = new StringResources( "LoadingMessage" );
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            ServerMessage.Text = stx.Text( "ProgressIndicator_PleaseWait" );
        }

        private void IndicateIdle()
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }
}