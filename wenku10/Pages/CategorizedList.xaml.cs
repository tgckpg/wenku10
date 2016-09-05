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

using wenku8.Model.ListItem;
using wenku8.Model.Section;

namespace wenku10.Pages
{
    sealed partial class CategorizedList : Page
    {
        public static readonly string ID = typeof( Page ).Name;

        CategorizedSection CS;
        PopupList PopupParent;

        public CategorizedList()
        {
            this.InitializeComponent();
        }

        public CategorizedList( PopupList S )
            :this()
        {
            PopupParent = S;

            NavigationHandler.OnNavigatedBack += NavigationHandler_OnNavigatedBack;
            CS = new CategorizedSection();
            CS.PropertyChanged += CS_PropertyChanged;
            MainList.DataContext = CS;
            CS.Load( S.Item.Payload );
        }

        private void CS_PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
        {
            switch ( e.PropertyName )
            {
                case "Data":
                    PopupParent.Navigate( typeof( NavList ), CS );
                    break;
                case "NavListItem":
                    PopupParent.Navigate( typeof( NavList ), CS.NavListItem );
                    break;
            }
        }

        private void ListView_ItemClick( object sender, ItemClickEventArgs e )
        {
            CS.LoadSubSections( e.ClickedItem as ActiveItem );
        }

        private void Button_Tapped( object sender, TappedRoutedEventArgs e )
        {
            PopupParent.Close();
        }

        private void NavigationHandler_OnNavigatedBack( object sender, XBackRequestedEventArgs e )
        {
            NavigationHandler.OnNavigatedBack -= NavigationHandler_OnNavigatedBack;
            PopupParent.Close();
            e.Handled = true;
        }
    }
}