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
using Net.Astropenguin.Helpers;

namespace wenku10.Pages.Explorer
{
	public sealed partial class GShortcuts : Page
	{
		public GShortcuts()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
		}

		public void LoadWidgets( IEnumerable<GRViewSource> GVSs )
		{
			foreach( GRViewSource GVS in GVSs )
			{
				CreateWidget( GVS.ItemTitle, "HThumbnails", GVS.DataSource );
			}
		}

		public void RegisterWidgets( IEnumerable<GRViewSource> GVSs )
		{
			LoadWidgets( GVSs );
		}

		private void CreateWidget( string Name, string ItemTemplate, GRDataSource DataSource )
		{
			try
			{
				DataSource.StructTable();
				DataSource.Reload();
			}
			catch ( EmptySearchQueryException )
			{
				return;
			}

			TextBlock HeaderText = new TextBlock() { Text = Name, FontSize = 25 };
			ListView DataList = new ListView();

			DataList.ItemTemplate = ( DataTemplate ) Resources[ ItemTemplate ];
			DataList.Style = ( Style ) Application.Current.Resources[ "VerticalListView" ];
			DataList.ItemContainerStyle = ( Style ) Application.Current.Resources[ "ListItemNoSelect" ];

			ScrollViewer.SetHorizontalScrollMode( DataList, ScrollMode.Disabled );
			ScrollViewer.SetHorizontalScrollBarVisibility( DataList, ScrollBarVisibility.Hidden );
			Binding TableItems = new Binding() { Path = new PropertyPath( "Items" ), Source = DataSource.Table };
			BindingOperations.SetBinding( DataList, ItemsControl.ItemsSourceProperty, TableItems );

			MainContents.Children.Add( HeaderText );
			MainContents.Children.Add( DataList );
		}

	}
}