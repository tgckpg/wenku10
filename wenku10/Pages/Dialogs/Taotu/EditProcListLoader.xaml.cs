using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using libtaotu.Crawler;
using libtaotu.Controls;
using libtaotu.Models.Procedure;
using libtaotu.Pages;

using wenku8.Taotu;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using System.Threading.Tasks;

namespace wenku10.Pages.Dialogs.Taotu
{
    sealed partial class EditProcListLoader : ContentDialog, IDisposable
    {
        public static readonly string ID = typeof( EditProcListLoader ).Name;

        private WenkuListLoader EditTarget;

        private EditProcListLoader()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            StringResources stx = new StringResources( "Message" );
            PrimaryButtonText = stx.Str( "OK" );

            MessageBus.OnDelivery += MessageBus_OnDelivery;
        }

        public void Dispose()
        {
            MessageBus.OnDelivery -= MessageBus_OnDelivery;
        }

        ~EditProcListLoader() { Dispose(); }

        public EditProcListLoader( WenkuListLoader EditTarget )
            : this()
        {
            this.EditTarget = EditTarget;
            EditTarget.SubEditComplete();

            LayoutRoot.DataContext = EditTarget;
        }

        private void TestDef( object sender, RoutedEventArgs e )
        {
            if ( TestRunning.IsActive ) return;

            TestRunning.IsActive = true;
            MessageBus.SendUI( typeof( ProceduresPanel ), "RUN", EditTarget );
        }

        private void SetPattern( object sender, RoutedEventArgs e )
        {
            TextBox Input = sender as TextBox;
            EditTarget.ItemPattern = Input.Text;
        }

        private void SetFormat( object sender, RoutedEventArgs e )
        {
            TextBox Input = sender as TextBox;
            EditTarget.ItemParam = Input.Text;
        }

        private void SetBanner( object sender, RoutedEventArgs e )
        {
            TextBox Input = sender as TextBox;
            EditTarget.BannerPath = Input.Text;
        }

        private void Subprocess( object sender, RoutedEventArgs e )
        {
            EditTarget.SubEdit = WListSub.Process;
            Popups.CloseDialog();
        }

        private async void ImportBookSpider( object sender, RoutedEventArgs e )
        {
            IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
            if ( ISF == null ) return;

            try
            {
                EditTarget.ImportSpider( new XRegistry( await ISF.ReadString(), null, false ).Parameter( "Procedures" ) );
            }
            catch ( Exception ex )
            {
                ProcManager.PanelMessage( EditTarget, Res.SSTR( "InvalidXML", ex.Message ), LogType.ERROR );
            }
        }

        private void SpiderProcess( object sender, RoutedEventArgs e )
        {
            EditTarget.SubEdit = WListSub.Spider;
            Popups.CloseDialog();
        }

        private void MessageBus_OnDelivery( Message Mesg )
        {
            ProcConvoy Convoy = Mesg.Payload as ProcConvoy;
            if ( Mesg.Content == "RUN_RESULT"
                && Convoy != null
                && Convoy.Dispatcher == EditTarget )
            {
                TestRunning.IsActive = false;

                Convoy = ProcManager.TracePackage( Convoy, ( P, C ) => Convoy.Payload is IEnumerable<BookInstruction> );

                if ( Convoy == null )
                {
                    throw new Exception( "Unable to find the generated book convoy" );
                }
                else
                {
                    var j = ViewTestResult( ( IEnumerable<BookInstruction> ) Convoy.Payload );
                }
            }
        }

        private async Task ViewTestResult( IEnumerable<BookInstruction> Payload )
        {
            if ( Payload.Count() == 0 ) return;

            IStorageFile PreviewFile = await AppStorage.MkTemp();
            await PreviewFile.WriteString(
                string.Join( "\n<<<<<<<<<<<<<<\n", Payload.Remap( x => x.PlainTextInfo ) ) );

            var j = Dispatcher.RunIdleAsync(
                x => Frame.Navigate( typeof( DirectTextViewer ), PreviewFile )
            );
        }

    }
}