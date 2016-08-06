using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Json;
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

using wenku8.AdvDM;
using wenku8.Model.REST;
using wenku8.Resources;

namespace wenku10.Pages.Sharers
{
    sealed partial class UserInfo : Page
    {
        private RuntimeCache RCache;

        private string CurrentDispName;

        public UserInfo()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            RCache = new RuntimeCache();

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.MyProfile()
                , ( e, id ) =>
                {
                    try
                    {
                        JsonObject JDef = JsonStatus.Parse( e.ResponseString );
                        JsonObject JData = JDef.GetNamedObject( "data" );
                        var j = Dispatcher.RunIdleAsync( ( x ) => SetProfileData( JData ) );
                    }
                    catch ( Exception ex )
                    {
                        ShowErrorMessage( ex.Message );
                    }
                    MarkIdle();
                }
                , ( a, b, ex ) =>
                {
                    ShowErrorMessage( ex.Message );
                    MarkIdle();
                }
                , false
            );
        }

        private void SetProfileData( JsonObject JData )
        {
            CurrentDispName = JData.GetNamedString( "display_name" );
            DisplayName.Text = CurrentDispName;
        }

        private void DispNameEnter( object sender, KeyRoutedEventArgs e )
        {
            if ( e.Key == Windows.System.VirtualKey.Enter )
            {
                Focus( FocusState.Pointer );
            }
        }

        private void DispNameLostFocus( object sender, RoutedEventArgs e )
        {
            SubmitDispName();
        }

        private void SubmitDispName()
        {
            string NewDispName = DisplayName.Text.Trim();
            if ( NewDispName == CurrentDispName ) return;

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.EditProfile( NewDispName )
                , ( e, id ) =>
                {
                    try
                    {
                        JsonObject JDef = JsonStatus.Parse( e.ResponseString );
                    }
                    catch ( Exception ex )
                    {
                        ShowErrorMessage( ex.Message );
                    }
                    MarkIdle();
                }
                , ( a, b, ex ) =>
                {
                    ShowErrorMessage( ex.Message );
                    MarkIdle();
                }
                , false
            );

            MarkBusy();
        }

        private void ShowErrorMessage( string Mesg )
        {
            var j = Dispatcher.RunIdleAsync( ( x ) => ErrorMessage.Text = Mesg );
        }

        private void MarkIdle()
        {
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                DisplayName.IsEnabled = true;
                LoadingRing.IsActive = false;
            } );
        }

        private void MarkBusy()
        {
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                DisplayName.IsEnabled = false;

                LoadingRing.IsActive = true;
            } );
        }

        private async void ChangePassword( object sender, RoutedEventArgs e )
        {
            await Popups.ShowDialog( new Dialogs.Sharers.ChangePassword() );
        }
    }
}