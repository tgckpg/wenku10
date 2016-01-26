using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using wenku8.Model.ListItem;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace wenku10.Pages.ContentReaderPane
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    sealed partial class BookmarkList : Page
    {
        private ContentReader Reader;
        private BookmarkListItem FlyoutTargetItem;

        public BookmarkList()
        {
            this.InitializeComponent();
        }

        public BookmarkList( ContentReader MainReader )
            :this()
        {
            Reader = MainReader;
            Reader.ContentView.Reader.PropertyChanged += Reader_PropertyChanged;
            MainList.ItemsSource = Reader.ContentView.Reader.CustomAnchors;
        }

        ~BookmarkList()
        {
            Reader.ContentView.Reader.PropertyChanged -= Reader_PropertyChanged;
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

        private void RemoveBookmark( object sender, TappedRoutedEventArgs e )
        {
            Reader.ContentView.Reader.RemoveAnchor( FlyoutTargetItem );
        }
    }
}