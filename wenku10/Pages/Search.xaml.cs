using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.CompositeElement;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Resources;

namespace wenku10.Pages
{
    public sealed partial class Search : Page
    {
        public static readonly string ID = typeof( Search ).Name;

        private IListLoader LL;
        private StringResources stx;

        private string SearchKey = null;

        public Search()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        ~Search() { Dispose(); }

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

            if ( e.NavigationMode == NavigationMode.Back && Frame.CanGoBack )
            {
                if ( Frame.BackStack.Last().SourcePageType != typeof( MainPage ) )
                {
                    Frame.BackStack.Remove( Frame.BackStack.Last() );
                }
            }

            if( e.Parameter != null )
            {
                SearchKey = e.Parameter.ToString();
                SearchTerm.Text = SearchKey;
                SCondition.SelectedIndex = 1;

                GetSearch( SearchKey );
            }
        }

        private void Seacrh_ItemClick( object sender, ItemClickEventArgs e )
        {
            RestoreStatus();
            Frame.Navigate( typeof( BookInfoView ), e.ClickedItem.XProp<string>( "Id" ) );
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

        private void SetTemplate()
        {
            stx = new StringResources();
            SearchTerm.PlaceholderText = stx.Text( "Search_Tooltip" );
        }

        private void GetSearch( string Key )
        {
            IsLoading.IsActive = true;
            SearchTerm.MinWidth = 0;
            Status.Text = stx.Text( "ImageViewer_Loading" );

            Expression<Action<IList<BookItem>>> handler = x => BookLoaded( x );
            LL = X.Instance<IListLoader>( XProto.ListLoader
                , X.Call<XKey[]>( XProto.WRequest, "GetSearch", GetSearchMethod(), Key )
                , Shared.BooksCache
                , handler 
                , false
            );
        }

        private string GetSearchMethod()
        {
            return SCondition.SelectedIndex == 0 ? "articlename" : "author";
        }

        private void RestoreStatus()
        {
            if ( string.IsNullOrEmpty( SearchKey ) ) return;

            IsLoading.IsActive = false;
            SCondition.Visibility
                = SearchTerm.Visibility
                = Visibility.Collapsed
                ;
            Status.Visibility = Visibility.Visible;

            SearchTerm.Text = SearchKey;
        }

        private void BookLoaded( IList<BookItem> aList )
        {
            IsLoading.IsActive = false;
            SCondition.Visibility
                = SearchTerm.Visibility
                = Visibility.Collapsed
                ;
            Status.Visibility = Visibility.Visible;

            SearchTerm.IsEnabled = false;

            Status.Text = stx.Text( "Search_ResultStamp_A" )
                + " " + LL.TotalCount + " "
                + stx.Text( "Search_ResultStamp_B" );

            Observables<BookItem, BookItem> ItemsObservable = new Observables<BookItem, BookItem>( aList );
            ItemsObservable.LoadEnd += ( a, b ) =>
            {
                IsLoading.IsActive = false;
            };
            ItemsObservable.LoadStart += ( a, b ) =>
            {
                IsLoading.IsActive = true;
            };

            ItemsObservable.ConnectLoader( LL );
            VGrid.ItemsSource = ItemsObservable;
        }

        private void SearchBox_QuerySubmitted( AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args )
        {
            SearchKey = args.QueryText.Trim();

            if ( string.IsNullOrEmpty( SearchKey ) )
            {
                return;
            }

            // Re-focus to disable keyboard
            this.Focus( FocusState.Pointer );

            GetSearch( SearchKey );
        }

        private void OpenSearchBar( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrEmpty( SearchKey ) ) return;

            SCondition.Visibility
                = SearchTerm.Visibility
                = Visibility.Visible
                ;

            SearchTerm.IsEnabled = true;
            Status.Visibility = Visibility.Collapsed;
            SearchTerm.Focus( FocusState.Keyboard );
        }

        private void Grid_Tapped( object sender, TappedRoutedEventArgs e )
        {
            RestoreStatus();
        }
    }
}
