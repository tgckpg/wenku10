using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;

using wenku8.CompositeElement;
using wenku8.Config;
using wenku8.Resources;
using wenku8.Settings;
using wenku8.System;

namespace wenku10.Pages.Settings.Advanced
{
    public sealed partial class Debug : Page
    {
        bool ActionBlocked = false;

        public Debug()
        {
            this.InitializeComponent();
            FileLogToggle.IsOn = Properties.ENABLE_SYSTEM_LOG;
            RemoteLogToggle.IsOn = Properties.ENABLE_RSYSTEM_LOG;
            RemoteAddress.Text = Properties.RSYSTEM_LOG_ADDRESS;

            string Level = Properties.LOG_LEVEL;
            LogLevelCB.SelectedItem = LogLevelCB.Items.FirstOrDefault( ( x ) => ( x as TextBlock ).Text == Level );
        }

        private void FileLog( object sender, RoutedEventArgs e )
        {
            Properties.ENABLE_SYSTEM_LOG = FileLogToggle.IsOn;
        }

        private void RemoteLog( object sender, RoutedEventArgs e )
        {
            Properties.ENABLE_RSYSTEM_LOG
                = RemoteAddress.IsEnabled
                = RemoteLogToggle.IsOn
                ;
        }

        private async void RemoteAddress_LostFocus( object sender, RoutedEventArgs e )
        {
            string IP = RemoteAddress.Text.Trim();

            IPAddress NotUsed;
            if ( !IPAddress.TryParse( IP, out NotUsed ) )
            {
                await Popups.ShowDialog( UIAliases.CreateDialog( "This IP Address is invalid" ) );
                RemoteAddress.Text = Properties.RSYSTEM_LOG_ADDRESS;
            }
            else
            {
                Properties.RSYSTEM_LOG_ADDRESS = IP;
            }
        }

        private void LogLevelCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            TextBlock T = LogLevelCB.SelectedItem as TextBlock;
            Properties.LOG_LEVEL = T.Text;
            LogControl.SetFilter( T.Text );
        }

        private async void ViewBgTaskConf( object sender, RoutedEventArgs e )
        {
            if ( ActionBlocked ) return;
            ActionBlocked = true;

            StorageFile ISF = await AppStorage.MkTemp();
            await ISF.WriteString( new XRegistry( "<tasks />", FileLinks.ROOT_SETTING + FileLinks.TASKS ).ToString() );

            await ControlFrame.Instance.CloseSubView();
            ControlFrame.Instance.SubNavigateTo( MainSettings.Instance, () => new DirectTextViewer( ISF ) );
        }

        private async void ViewDebugLog( object sender, RoutedEventArgs e )
        {
            if ( ActionBlocked ) return;
            ActionBlocked = true;

            StorageFile ISF = await AppStorage.MkTemp();

            if ( Shared.Storage.FileExists( "debug.log" ) )
            {
                Bootstrap.LogInstance.Stop();

                using ( Stream s = Bootstrap.LogInstance.GetStream() )
                using ( Stream ts = await ISF.OpenStreamForWriteAsync() )
                {
                    await s.CopyToAsync( ts );
                }

                Bootstrap.LogInstance.Start();
            }

            await ControlFrame.Instance.CloseSubView();
            ControlFrame.Instance.SubNavigateTo( MainSettings.Instance, () => new DirectTextViewer( ISF ) );
        }

        private async void ClearDebugLog( object sender, RoutedEventArgs e )
        {
            if ( ActionBlocked ) return;
            ActionBlocked = true;

            StorageFile ISF = await AppStorage.MkTemp();

            if ( Shared.Storage.FileExists( "debug.log" ) )
            {
                Bootstrap.LogInstance.Stop();
                Shared.Storage.DeleteFile( "debug.log" );
                Bootstrap.LogInstance.Start();
            }
        }

    }
}