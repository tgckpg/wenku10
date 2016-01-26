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

using wenku8.Model.Section;
using wenku8.Model.ListItem;

namespace wenku10.Pages
{
    public sealed partial class History : Page
    {
        public static readonly string ID = typeof( History ).Name;
        private HistorySection HistoryContext;

        public History()
        {
            this.InitializeComponent();
            LoadHistory();
        }

        ~History() { Dispose(); }

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
        }

        private void History_ItemClick( object sender, ItemClickEventArgs e )
        {
            Frame.Navigate( typeof( BookInfoView ), ( e.ClickedItem as ActiveItem ).Payload );
        }

        private void LoadHistory()
        {
            HistoryContext = new HistorySection();
            HistoryView.DataContext = HistoryContext;
            HistoryContext.Load();
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            HistoryContext.SearchTerm = sender.Text.Trim();
        }
    }
}
