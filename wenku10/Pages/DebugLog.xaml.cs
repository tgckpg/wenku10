using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging.Handler;

using wenku8.Config;
using wenku8.Model.Text;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace wenku10.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DebugLog : Page
    {
        public DebugLog()
        {
            this.InitializeComponent();
            if( Properties.ENABLE_SYSTEM_LOG )
            {
                SetTemplate();
            }
        }

        private void SetTemplate()
        {
			IsolatedStorageFile isf = new AppStorage().GetISOStorage();
            if ( !isf.FileExists( "debug.log" ) ) return;

            FileSystemLog FSL = global::wenku8.System.Bootstrap.LogInstance;
            FSL.Stop();

            StreamReader Reader = new StreamReader( FSL.GetStream() );

            List<LogLine> Logs = new List<LogLine>();
            while( !Reader.EndOfStream )
            {
                Logs.Add( new LogLine( Reader.ReadLine() ) );
            }

            LogList.ItemsSource = Logs;

            Reader.Dispose();
            FSL.Start();
        }

        private async void LogList_ItemClick( object sender, ItemClickEventArgs e )
        {
            LogLine L = e.ClickedItem as LogLine;
            MessageDialog Mesg = new MessageDialog( L.Message, L.Tag );
            await Popups.ShowDialog( Mesg );
        }

    }
}
