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

using wenku8.Config;
using wenku8.Storage;
using Net.Astropenguin.Logging;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace wenku10.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FirstTimeSettings : Page
    {
        private static readonly string ID = typeof( FirstTimeSettings ).Name;

        public FirstTimeSettings()
        {
            this.InitializeComponent();

            SetTemplate();
            Prev( false ); Next( false );
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            if ( Frame.CanGoBack ) Frame.BackStack.Clear();
        }

        private void SetTemplate()
        {
            OneDriveToggle.IsOn = Properties.ENABLE_ONEDRIVE;
        }

        private void Prev( object sender, RoutedEventArgs e ) { Prev(); }
        private void Next( object sender, RoutedEventArgs e ) { Next(); }

        private void Complete( object sender, RoutedEventArgs e )
        {
            Properties.FIRST_TIME_RUN = false;
            SetTheme();
            Windows.ApplicationModel.Core.CoreApplication.Exit();
        }

        private void Prev( bool Auto = true )
        {
            if ( Auto && 0 < MainView.SelectedIndex ) MainView.SelectedIndex--;

            if( MainView.SelectedIndex == 0 )
            {
                PrevBtn.Visibility = Visibility.Collapsed;
                CompBtn.Visibility = Visibility.Collapsed;
            }

            if( MainView.SelectedIndex < MainView.Items.Count - 1)
            {
                NextBtn.Visibility = Visibility.Visible;
            }
        }

        private void Next( bool Auto = true )
        {
            if ( Auto )
            {
                MainView.SelectedIndex++;
            }

            if ( 0 < MainView.SelectedIndex )
            {
                PrevBtn.Visibility = Visibility.Visible;
            }

            if( MainView.SelectedIndex == MainView.Items.Count - 1 )
            {
                NextBtn.Visibility = Visibility.Collapsed;
                CompBtn.Visibility = Visibility.Visible;
            }
            else
            {
                CompBtn.Visibility = Visibility.Collapsed;
            }
        }

        private async void OneDrive( object sender, RoutedEventArgs e )
        {
            if( Properties.ENABLE_ONEDRIVE = OneDriveToggle.IsOn )
            {
                OneDriveSync.Instance = new OneDriveSync();
                await OneDriveSync.Instance.Authenticate();
            }
            else
            {
                await OneDriveSync.Instance.UnAuthenticate();
            }
        }

        private void ToggleSS( object sender, RoutedEventArgs e )
        {
            Properties.ENABLE_SERVER_SEL = EnableSS.IsOn;
        }

        private void SetTheme()
        {
            global::wenku8.Settings.Theme.ThemeSet T;
            if( ThemeToggle.IsOn )
            {
                T = global::wenku8.System.ThemeManager.DefaultDark();
                T.GreyShades();

                Properties.APPEARANCE_CONTENTREADER_BACKGROUND = Windows.UI.Color.FromArgb( 255, 10, 10, 10 );
                Properties.APPEARANCE_CONTENTREADER_FONTCOLOR = Windows.UI.Color.FromArgb( 255, 220, 220, 220 );
                Properties.APPEARANCE_CONTENTREADER_NAVBG = Windows.UI.Color.FromArgb( 255, 50, 50, 50 );
                Properties.APPEARANCE_CONTENTREADER_ASSISTBG = Windows.UI.Colors.Gray;
            }
            else
            {
                T = global::wenku8.System.ThemeManager.DefaultLight();
                T.BlackShades();

                Properties.APPEARANCE_CONTENTREADER_BACKGROUND = Windows.UI.Color.FromArgb( 255, 64, 64, 64 );
                Properties.APPEARANCE_CONTENTREADER_FONTCOLOR = Windows.UI.Color.FromArgb( 255, 217, 234, 225 );
                Properties.APPEARANCE_CONTENTREADER_TAPBRUSHCOLOR = Windows.UI.Color.FromArgb( 255, 255, 88, 9 );
                Properties.APPEARANCE_CONTENTREADER_NAVBG = Windows.UI.Color.FromArgb( 255, 81, 94, 108 );
                Properties.APPEARANCE_CONTENTREADER_ASSISTBG = Windows.UI.Color.FromArgb( 23, 0, 0, 0 );
            }

            T.Apply();
        }

        private void MainView_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            Prev( false ); Next( false );
        }
    }
}
