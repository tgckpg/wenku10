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

using Net.Astropenguin.Controls;
using Net.Astropenguin.Logging;

using wenku8.CompositeElement;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.ListItem;

namespace wenku10.Pages
{
    public sealed partial class NavList : Page
    {
        public static readonly string ID = typeof( NavList ).Name;

        private VariableGridView VGrid;
        public NavList()
        {
            this.InitializeComponent();
        }

        ~NavList() { Dispose(); }

        private void Dispose()
        {
            NavigationHandler.OnNavigatedBack -= OnBackRequested;
        }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            BackMask.HandleBack( Frame, e );
            Dispose();
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );
            NavigationHandler.InsertHandlerOnNavigatedBack( OnBackRequested );

            Type ParamType = e.Parameter.GetType();

            if ( ParamType == typeof( SubtleUpdateItem ) )
            {
                DisplayTopList( e.Parameter as SubtleUpdateItem );
            }
            else
            {
                MainSplitView.DataContext = e.Parameter;
            }
        }

        private void DisplayTopList( SubtleUpdateItem Item )
        {
            ISectionItem PS = X.Instance<ISectionItem>( XProto.NavListSection, Item.Name );
            MainSplitView.DataContext = PS;
            PS.Load( Item.Payload.ToString(), true );
        }

        private void GridView_Loaded( object sender, RoutedEventArgs e )
        {
            VGrid = sender as VariableGridView;
            VGrid.ViewChanged += VGrid_ViewChanged;
        }

        private void VGrid_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
        {
            if( VGrid.HorizontalOffset == 0 && VGrid.VerticalOffset == 0 )
            {
                MainSplitView.OpenPane();
            }
            // This is to avoid internal code calling
            else if( MainSplitView.State == PaneStates.Opened )
            {
                MainSplitView.ClosePane();
            }
        }

        private void VariableGridView_ItemClick( object sender, ItemClickEventArgs e )
        {
            BookItem b = e.ClickedItem as BookItem;
            Frame.Navigate( typeof( BookInfoView ), b.Id );
        }
    }
}
