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

using GR.Model.Section;

namespace wenku10.Pages.Explorer.Widgets
{
	public sealed partial class ThumbnailList : UserControl
	{
		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
			"ItemsSource", typeof( object ), typeof( ThumbnailList )
			, new PropertyMetadata( null, OnUpdateItemsSource ) );

		public object ItemsSource
		{
			get { return ( object ) GetValue( ItemsSourceProperty ); }
			set { SetValue( ItemsSourceProperty, value ); }
		}

		public ThumbnailList()
		{
			this.InitializeComponent();
		}

		private static void OnUpdateItemsSource( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( ThumbnailList ) d ).SourceUpdate();
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
	}
}