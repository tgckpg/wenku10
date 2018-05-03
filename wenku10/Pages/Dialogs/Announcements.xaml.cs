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
using GR.Model.ListItem;
using GAnnouncements = GR.Model.Topics.Announcements;

namespace wenku10.Pages.Dialogs
{
	public sealed partial class Announcements : ContentDialog
	{
		private GAnnouncements AS;

		public Announcements()
		{
			this.InitializeComponent();

			StringResources stx = StringResources.Load( "Message" );
			PrimaryButtonText = stx.Str( "AllRead" );
			SecondaryButtonText = stx.Str( "OK" );

			FullVersion.Text = AppSettings.Version;
			version.Text = Bootstrap.Version;

			SetTemplate();
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			AS.AllRead();
			args.Cancel = true;
		}

		private void SetTemplate()
		{
			AS = new GAnnouncements();
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