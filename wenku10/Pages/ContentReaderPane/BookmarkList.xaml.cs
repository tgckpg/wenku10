using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using GR.Model.ListItem;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace wenku10.Pages.ContentReaderPane
{
	sealed partial class BookmarkList : Page
	{
		private ContentReaderBase Reader;
		private BookmarkListItem FlyoutTargetItem;

		public BookmarkList()
		{
			this.InitializeComponent();
		}

		public BookmarkList( ContentReaderBase MainReader )
			:this()
		{
			Reader = MainReader;
			Reader.ContentView.Reader.PropertyChanged += Reader_PropertyChanged;
			MainList.ItemsSource = Reader.ContentView.Reader.CustomAnchors;
		}

		private void Reader_PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
		{
			if( e.PropertyName == "CustomAnchors" )
			{
				MainList.ItemsSource = Reader.ContentView.Reader.CustomAnchors;
			}
		}

		private void ListView_ItemClick( object sender, ItemClickEventArgs e )
		{
			BookmarkListItem Item = e.ClickedItem as BookmarkListItem;
			Reader.OpenBookmark( Item );
		}

		private void Grid_RightTapped( object sender, RightTappedRoutedEventArgs e )
		{
			Grid ItemGrid = ( Grid ) sender;
			FlyoutTargetItem = ItemGrid.DataContext as BookmarkListItem;

			if ( FlyoutTargetItem.AnchorIndex != -1 )
			{
				FlyoutBase.ShowAttachedFlyout( ItemGrid );
			}
		}

		private void RemoveBookmark( object sender, RoutedEventArgs e )
		{
			Reader.ContentView.Reader.RemoveAnchor( FlyoutTargetItem );
		}
	}
}