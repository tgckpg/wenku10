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

using wenku8.Model.ListItem;

namespace wenku10.Pages.Dialogs
{
	public sealed partial class Announcements : ContentDialog
	{
		private global::wenku8.Model.Topics.Announcements AS;

		public Announcements()
		{
			this.InitializeComponent();

			StringResources stx = new StringResources( "Message" );
			PrimaryButtonText = stx.Str( "AllRead" );
			SecondaryButtonText = stx.Str( "OK" );

			FullVersion.Text = global::wenku8.Config.AppSettings.Version;
			version.Text = global::wenku8.System.Bootstrap.Version;

			SetTemplate();
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			AS.AllRead();
			args.Cancel = true;
		}

		private void SetTemplate()
		{
			AS = new global::wenku8.Model.Topics.Announcements();
			MainList.ItemsSource = AS.Topics;
		}

		private void MainList_ItemClick( object sender, ItemClickEventArgs e )
		{
			AS.MaskAsRead( ( NewsItem ) e.ClickedItem );
		}

		private void ShowVersion( object sender, RoutedEventArgs e )
		{
			FlyoutBase.ShowAttachedFlyout( ( FrameworkElement ) sender );
		}

	}
}