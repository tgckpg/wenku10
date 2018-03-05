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

using GR.DataSources;

namespace wenku10.Pages.Dialogs
{
	public sealed partial class AddWidget : ContentDialog
	{
		private IEnumerable<GRViewSource> AvailableWidgets;
		public GRViewSource SelectedWidget { get; private set; }
		public string WidgetName { get; private set; }
		public string WidgetTemplate { get; private set; }
		public string SearchKey { get; private set; }

		public AddWidget( IEnumerable<GRViewSource> AvailableWidgets )
		{
			this.AvailableWidgets = AvailableWidgets;
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			StringResources stx = new StringResources( "Message", "AppBar" );

			PrimaryButtonText = stx.Str( "OK" );
			SecondaryButtonText = stx.Str( "Cancel" );

			Title = stx.Text( "AddWidget", "AppBar" );

			WidgetList.ItemsSource = AvailableWidgets;
			WidgetTemplateList.ItemsSource = new Dictionary<string, string>()
			{
				{ "Banner", "Banner" },
				{ "ThumbnailList - Horizontal", "HorzThumbnailList" }
			};
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			SelectedWidget = ( GRViewSource ) WidgetList.SelectedItem;
			WidgetName = NewName.Text;
			WidgetTemplate = WidgetTemplateList.SelectedValue as string ?? "ThumbnailList";

			if ( SelectedWidget.DataSource.Searchable )
			{
				SearchKey = QueryStr.Text.Trim();
			}
		}

		private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
		}

		private void WidgetList_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if ( WidgetList.SelectedItem is GRViewSource GVS )
			{
				NewName.PlaceholderText = GVS.ItemTitle;
				QKeyword.Visibility = GVS.DataSource.Searchable ? Visibility.Visible : Visibility.Collapsed;
			}
		}

	}
}