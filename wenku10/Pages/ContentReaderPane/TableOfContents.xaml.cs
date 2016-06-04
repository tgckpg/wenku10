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

using Net.Astropenguin.Logging;

using wenku8.Model.Book;
using wenku8.Model.ListItem;
using wenku8.Model.Section;

namespace wenku10.Pages.ContentReaderPane
{
    /// <summary>
    /// Table of content view
    /// </summary>
    sealed partial class TableOfContents : Page
    {
        public static readonly string ID = typeof( TableOfContents ).Name;

        private ContentReader Reader;

        private TOCPane TOC;

        public TableOfContents()
        {
            InitializeComponent();
        }

        public TableOfContents( ContentReader MainReader )
            :this()
        {
            Reader = MainReader;

            if( Reader.CurrentBook == null )
            {
                Logger.Log( ID, "Cannot init TOC: CurrentBook is null... is pages unloaded ?", LogType.WARNING );
                return;
            }

            TOC = new TOCPane( Reader.CurrentBook.GetVolumes() );

            TOCContext.DataContext = TOC;
            TOCList.SelectedItem = TOC.GetItem( Reader.CurrentChapter );
            TOCList.Loaded += TOCListLoaded;
            TOCList.SelectionChanged += TOCList_SelectionChanged;
        }

        public void UpdateDisplay()
        {
            TOCList.SelectedItem = TOC.GetItem( Reader.CurrentChapter );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            if( e.Parameter is Tuple<Volume[], SelectionChangedEventHandler>)
            {
                Tuple<Volume[], SelectionChangedEventHandler> Args = e.Parameter as Tuple<Volume[], SelectionChangedEventHandler>;
                Load( Args.Item1, Args.Item2 );
            }
        }

        /// <summary>
        /// Standalone mode, use it for preview or something
        /// </summary>
        /// <param name="Vols"> The Volumes needed to be shown </param>
        /// <param name="SelectEvent"> EventHandler when an item is selected </param>
        private void Load( Volume[] Vols, SelectionChangedEventHandler SelectEvent = null )
        {
            TOC = new TOCPane( Vols );

            TOCContext.DataContext = TOC;

            if ( SelectEvent != null )
            {
                TOCList.SelectionChanged += SelectEvent;
            }
        }

        private void TOCListLoaded( object sender, RoutedEventArgs e )
        {
            TOCList.ScrollIntoView( TOCList.SelectedItem );
        }

        private void TOCList_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count() < 1 ) return;

            Reader.OpenBook( ( e.AddedItems[ 0 ] as TOCItem ).GetChapter() );
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            TOC.SearchTerm = sender.Text.Trim();
        }
    }
}
