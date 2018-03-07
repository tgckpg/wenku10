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

using GR.GSystem;
using GR.Resources;
using GR.Settings;

namespace wenku10.Pages.Settings.Data
{
	public sealed partial class Cache : Page
	{
		public Cache()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			UpdateZCache();
			UpdateFTSData();
		}

		private async void UpdateZCache()
		{
			StringResources stx = new StringResources( "Settings" );
			CacheLimit.Text = stx.Text( "Data_CacheUsed" ) + " " + Utils.AutoByteUnit( await Shared.Storage.FileSize( FileLinks.DB_ZCACHE ) );
		}

		private async void UpdateFTSData()
		{
			StringResources stx = new StringResources( "Settings" );
			if ( Shared.Storage.FileExists( FileLinks.DB_FTS_DATA ) )
			{
				FTSSize.Text = stx.Text( "Data_CacheUsed" ) + " " + Utils.AutoByteUnit( await Shared.Storage.FileSize( FileLinks.DB_FTS_DATA ) );
			}
			else
			{
				FTSSize.Text = stx.Text( "Data_CacheUsed" ) + " " + Utils.AutoByteUnit( 0 );
			}
		}

		private void Button_Click_1( object sender, RoutedEventArgs e )
		{
			Shared.ZCacheDb.Reset();
			UpdateZCache();
		}

		private void Button_Click_2( object sender, RoutedEventArgs e )
		{
			GR.Database.ContextManager.RemoveFTSContext();
			// This removes the internal CFCache
			Shared.Storage.DeleteFile( FileLinks.DB_FTS_DATA );
			UpdateFTSData();
		}

	}
}