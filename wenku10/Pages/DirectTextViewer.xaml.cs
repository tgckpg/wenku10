using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku10.Pages
{
    public sealed partial class DirectTextViewer : Page
    {
        public static readonly string ID = typeof( DirectTextViewer ).Name;

        public DirectTextViewer()
        {
            this.InitializeComponent();
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

            ViewFile( e.Parameter as StorageFile );
        }

        private async void ViewFile( StorageFile file )
        {
            StorageFileStreamer SFS = new StorageFileStreamer( file );
            IList<string> FirstRead = await SFS.NextPage( 50 );

            Observables<string, string> OSF = new Observables<string, string>( FirstRead );
            OSF.ConnectLoader( SFS );

            TextContent.ItemsSource = OSF;
        }

    }
}