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

using Net.Astropenguin.Messaging;

using GR.Model.Section;
using GR.Settings;

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

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			"Title", typeof( string ), typeof( ThumbnailList )
			, new PropertyMetadata( null, OnUpdateTitle ) );

		public string Title
		{
			get { return ( string ) GetValue( TitleProperty ); }
			set { SetValue( TitleProperty, value ); }
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

		private static void OnUpdateTitle( DependencyObject d, DependencyPropertyChangedEventArgs e ) => ( ( ThumbnailList ) d ).TitleUpdate();
		private void TitleUpdate() => NameText.Text = Title;

		private void ShowMore_Click( object sender, RoutedEventArgs e )
		{
			if ( DataContext is WidgetView WV )
			{
				MessageBus.SendUI( GetType(), AppKeys.OPEN_VIEWSOURCE, WV.ViewSource );
			}
		}
	}
}