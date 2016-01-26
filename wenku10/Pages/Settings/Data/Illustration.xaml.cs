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

using wenku8.Config;
using wenku8.Resources;

namespace wenku10.Pages.Settings.Data
{
    public sealed partial class Illustration : Page
    {
        public Illustration()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private async void SetTemplate()
        {
            SaveLocation.Loaded += SaveLocation_Loaded;
            Shared.Storage.IsLibraryValid = await Shared.Storage.TestLibraryValid();
            ErrorMessage.Visibility = Shared.Storage.IsLibraryValid ? Visibility.Collapsed : Visibility.Visible;
            UpdateFields();
        }

        void SaveLocation_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            Properties.DATA_IMAGE_SAVE_TO_MEDIA_LIBRARY = IsSaveToMediaLibrary();
        }

        void UpdateFields()
        {
            StringResources stx = new StringResources( "Settings" );
            illus_Size.Text = stx.Text( "Data_CacheUsed" )
                + ": " + global::wenku8.System.Utils.AutoByteUnit( Shared.Storage.ImageSize() );
        }

        void SaveLocation_Loaded( object sender, RoutedEventArgs e )
        {
            SaveLocation.SelectedIndex = Properties.DATA_IMAGE_SAVE_TO_MEDIA_LIBRARY ? 1 : 0;
            if ( !Shared.Storage.IsLibraryValid )
            {
                SaveLocation.IsEnabled = false;
                SaveLocation.SelectedIndex = 0;
            }
            SaveLocation.SelectionChanged += SaveLocation_SelectionChanged;
        }

        private bool IsSaveToMediaLibrary()
        {
            return ( SaveLocation.SelectedIndex == 1 );
        }

        private void Data_Clear( object sender, RoutedEventArgs e )
        {
            Shared.Storage.CLEAR_IMAGE();
            UpdateFields();
        }

    }
}
