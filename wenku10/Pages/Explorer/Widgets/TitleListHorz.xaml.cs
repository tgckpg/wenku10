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

using GR.Data;
using GR.Model.Section;

namespace wenku10.Pages.Explorer.Widgets
{
	public sealed partial class TitleListHorz : UserControl
	{
		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
			"ItemsSource", typeof( object ), typeof( TitleListHorz )
			, new PropertyMetadata( null, OnUpdateItemsSource ) );

		public object ItemsSource
		{
			get { return ( object ) GetValue( ItemsSourceProperty ); }
			set { SetValue( ItemsSourceProperty, value ); }
		}

		public TitleListHorz()
		{
			this.InitializeComponent();
		}

		private static void OnUpdateItemsSource( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( TitleListHorz ) d ).SourceUpdate();
		}

		private void SourceUpdate()
		{
			if ( ItemsSource is IEnumerable<object> EnumSource )
			{
				MainItems.ItemsSource = EnumSource;
			}
		}

		private void ShowMore_Click( object sender, RoutedEventArgs e )
		{
			if ( DataContext is WidgetView WV )
			{
				WV.OpenViewSource();
			}
		}

		private void MainItems_ItemClick( object sender, ItemClickEventArgs e )
		{
			if ( DataContext is WidgetView WV && e.ClickedItem is IGRRow Row )
			{
				WV.ViewSource.ItemAction( Row );
			}
		}
	}
}