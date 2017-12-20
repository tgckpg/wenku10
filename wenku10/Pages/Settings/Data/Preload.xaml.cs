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

using GR.Resources;

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
			CalculateCoverSize();
			TextContentSize.Text = stx.Str( "Calculating" );
			CalculateTextSize();
		}

		private async void CalculateCoverSize()
		{
			StringResources stx = new StringResources( "Settings" );
			CoverSize.Text = stx.Text( "Data_CacheUsed" )
				+ ": " + await Task.Run( () => global::GR.GSystem.Utils.AutoByteUnit( Shared.Storage.CoverSize() ) );
		}

		private async void CalculateTextSize()
		{
			StringResources stx = new StringResources( "Settings" );
			TextContentSize.Text = stx.Text( "Data_CacheUsed" )
				+ ": " + await Task.Run( () => global::GR.GSystem.Utils.AutoByteUnit( Shared.Storage.GetStaticContentsUsage() ) );
		}

		private void Button_Click_1( object sender, RoutedEventArgs e )
		{
			Shared.Storage.CLEAR_COVER();
			SetTemplate();
		}

		private void Button_Click_2( object sender, RoutedEventArgs e )
		{
			Shared.Storage.CLEAR_INTRO();
			Shared.Storage.CLEAR_VOLUME();
			SetTemplate();
		}

	}
}