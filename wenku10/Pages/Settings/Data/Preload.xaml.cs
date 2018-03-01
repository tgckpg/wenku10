using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
	public sealed partial class Preload : Page
	{
		public Preload()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			StringResources stx = new StringResources( "LoadingMessage" );
			CoverSize.Text = stx.Str( "Calculating" );
			TextContentSize.Text = stx.Str( "Calculating" );
			CalculateCoverSize();
			CalculateTextSize();
		}

		private async void CalculateCoverSize()
		{
			StringResources stx = new StringResources( "Settings" );
			(int nFolders, int nFiles, ulong nSize) = await Shared.Storage.Stat( FileLinks.ROOT_COVER );
			CoverSize.Text = stx.Text( "Data_CacheUsed" )
				+ string.Format( ": {0} folders, {1} files, {2}", nFolders, nFiles, Utils.AutoByteUnit( nSize ) );
		}

		private async void CalculateTextSize()
		{
			StringResources stx = new StringResources( "Settings" );
			TextContentSize.Text = stx.Text( "Data_CacheUsed" )
				+ ": " + Utils.AutoByteUnit( await Shared.Storage.FileSize( FileLinks.DB_BOOKS ) );
		}

		private void Button_Click_1( object sender, RoutedEventArgs e )
		{
			Shared.Storage.CLEAR_COVER();
			SetTemplate();
		}

		private void Button_Click_2( object sender, RoutedEventArgs e )
		{
			GR.Database.ContextManager.ClearBookTexts();
			SetTemplate();
		}

	}
}