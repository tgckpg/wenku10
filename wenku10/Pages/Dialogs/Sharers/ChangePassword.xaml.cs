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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku8.AdvDM;
using wenku8.Resources;
using wenku8.Model.REST;

namespace wenku10.Pages.Dialogs.Sharers
{
    public sealed partial class ChangePassword : ContentDialog
    {
        public bool Canceled = true;

        public ChangePassword()
        {
            this.InitializeComponent();

            StringResources stx = new StringResources( "Message" );
            PrimaryButtonText = stx.Str( "OK" );
            SecondaryButtonText = stx.Str( "Cancel" );
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
            string CurrPasswd = CurrentPassword.Password;
            string NewPasswd = Password.Password;
            string PasswdV = PasswordV.Password;

            if ( string.IsNullOrEmpty( CurrPasswd ) || string.IsNullOrEmpty( NewPasswd ) || string.IsNullOrEmpty( PasswdV ) )
            {
                if ( string.IsNullOrEmpty( CurrPasswd ) )
                {
                    CurrentPassword.Focus( FocusState.Keyboard );
                }
                else if ( string.IsNullOrEmpty( NewPasswd ) )
                {
                    Password.Focus( FocusState.Keyboard );
                }
                else if( string.IsNullOrEmpty( PasswdV ) )
                {
                    PasswordV.Focus( FocusState.Keyboard );
                }
            }
            else if( NewPasswd != PasswdV )
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
                    = CurrentPassword.IsEnabled
                    = Password.IsEnabled
                    = PasswordV.IsEnabled
                    = false
                    ;

                this.Focus( FocusState.Pointer );

                IndicateLoad();

                RuntimeCache RCache = new RuntimeCache() { EN_UI_Thead = true };
                RCache.POST(
                    Shared.ShRequest.Server
                    , Shared.ShRequest.ChangePassword( CurrPasswd, NewPasswd )
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
                CurrentPassword.Focus( FocusState.Keyboard );
            }
        }

        private void ErrorMessage( string Mesg )
        {
            ServerMessage.Text = Mesg;

            IsPrimaryButtonEnabled
                = IsSecondaryButtonEnabled
                = CurrentPassword.IsEnabled
                = Password.IsEnabled
                = PasswordV.IsEnabled
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
            Worker.UIInvoke( () =>
            {
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
            } );
        }
    }
}