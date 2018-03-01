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

using GR.Config;
using GR.GSystem;
using GR.Resources;
using GR.Settings;

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

		private void SaveLocation_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			Properties.DATA_IMAGE_SAVE_TO_MEDIA_LIBRARY = IsSaveToMediaLibrary();
		}

		private async void UpdateFields()
		{
			StringResources stx = new StringResources( "Settings", "LoadingMessage" );
			illus_Size.Text = stx.Str( "Calculating", "LoadingMessage" );

			(int nFolders, int nFiles, ulong nSize) = await Shared.Storage.Stat( FileLinks.ROOT_IMAGE );
			illus_Size.Text = stx.Text( "Data_CacheUsed" )
				+ string.Format( ": {0} folders, {1} files, {2}", nFolders, nFiles, Utils.AutoByteUnit( nSize ) );
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