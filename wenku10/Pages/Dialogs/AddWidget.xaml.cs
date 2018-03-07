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
using GR.Model.Section;
using Windows.UI;

namespace wenku10.Pages.Dialogs
{
	sealed partial class AddWidget : ContentDialog
	{
		private IEnumerable<GRViewSource> AvailableWidgets;
		public WidgetView SelectedWidget { get; private set; }

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
				{ "ThumbnailList - Horizontal", "HorzThumbnailList" },
				{ "TitleList - Horizontal", "TitleListHorz" }
			};
		}

		private async void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs e )
		{
			e.Cancel = true;

			GRViewSource GVS = ( GRViewSource ) WidgetList.SelectedItem;
			WidgetView SW = new WidgetView( GVS );

			await SW.ConfigureAsync();

			string NName = NewName.Text.Trim();
			string NQuery = QueryStr.Text.Trim();

			SW.Conf.Enable = true;
			SW.Conf.Name = string.IsNullOrEmpty( NName ) ? GVS.ItemTitle : NName;
			SW.Conf.Template = WidgetTemplateList.SelectedValue as string ?? "HorzThumbnailList";

			if ( SW.DataSource.Searchable )
			{
				if ( SW.SearchRequired && string.IsNullOrEmpty( NQuery ) )
				{
					QueryStr.BorderBrush = new SolidColorBrush( Colors.Red );
					QueryStr.BorderThickness = new Thickness( 1 );
					return;
				}

				SW.Conf.Query = NQuery;
				SW.DataSource.Search = NQuery;
			}

			SelectedWidget = SW;
			this.Hide();
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