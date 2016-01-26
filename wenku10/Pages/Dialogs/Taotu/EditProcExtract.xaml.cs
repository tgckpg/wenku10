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
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using libtaotu.Crawler;
using libtaotu.Controls;
using libtaotu.Models.Procedure;

using wenku8.Taotu;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;

namespace wenku10.Pages.Dialogs.Taotu
{
    sealed partial class EditProcExtract : ContentDialog, IDisposable
    {
        public static readonly string ID = typeof( EditProcExtract ).Name;

        private IStorageFile PreviewFile;
        private WenkuExtractor EditTarget;

        private EditProcExtract()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            StringResources stx = new StringResources( "Message" );
            PrimaryButtonText = stx.Str( "OK" );
        }

        public void Dispose()
        {
            if ( PreviewFile != null )
            {
                var j = PreviewFile.DeleteAsync();
            }
        }

        public EditProcExtract( WenkuExtractor EditTarget )
            : this()
        {
            this.EditTarget = EditTarget;
            EditTarget.SubEditComplete();

            if( EditTarget.PropDefs.Count == 0 )
            {
                EditTarget.PropDefs.Add( new WenkuExtractor.PropExt( BookInfo.Title ) );
            }

            IncomingCheck.IsChecked = EditTarget.Incoming;

            LayoutRoot.DataContext = EditTarget;

            if ( !string.IsNullOrEmpty( EditTarget.TargetUrl ) )
            {
                UrlInput.Text = EditTarget.TargetUrl;
            }
        }

        private async void TestDef( object sender, RoutedEventArgs e )
        {
            string Url = UrlInput.Text.Trim();
            if ( string.IsNullOrEmpty( Url ) ) return;

            Button B = sender as Button;
            ProgressRing Pring = B.FindName( "TestRunning" ) as ProgressRing;

            Pring.IsActive = true;
            try
            {
                if ( PreviewFile == null ) PreviewFile = await AppStorage.MkTemp();

                IStorageFile ISF = await ProceduralSpider.DownloadSource( Url );
                string Content = await ISF.ReadString();

                // The resulting convoy may not be the book instruction originally created
                ProcConvoy Convoy = await new ProceduralSpider( new Procedure[] { EditTarget } )
                    .Crawl( new ProcConvoy( new ProcDummy(), PreviewFile ) );

                // So we trackback the Book Convoy
                Convoy = ProcManager.TracePackage( Convoy, ( D, C ) => C.Payload is BookInstruction );

                if( Convoy == null )
                {
                    throw new Exception( "Unable to find the generated book convoy" );
                }

                await PreviewFile.WriteString( ( Convoy.Payload as BookInstruction ).PlainTextInfo );

                var j = Dispatcher.RunIdleAsync(
                    x => Frame.Navigate( typeof( DirectTextViewer ), PreviewFile )
                );
            }
            catch( Exception ex )
            {
                ProcManager.PanelMessage( ID, ex.Message, LogType.INFO );
            }

            Pring.IsActive = false;
        }

        private void AddPropDef( object sender, RoutedEventArgs e )
        {
            EditTarget.PropDefs.Add( new WenkuExtractor.PropExt() );
        }

        private void SetPattern( object sender, RoutedEventArgs e )
        {
            TextBox Input = sender as TextBox;
            ProcFind.RegItem Item = Input.DataContext as ProcFind.RegItem;
            Item.Pattern = Input.Text;

            Item.Validate( FindMode.MATCH );
        }

        private void SetFormat( object sender, RoutedEventArgs e )
        {
            TextBox Input = sender as TextBox;
            ProcFind.RegItem Item = Input.DataContext as ProcFind.RegItem;
            Item.Format = Input.Text;

            Item.Validate( FindMode.MATCH );
        }

        private void SetUrl( object sender, RoutedEventArgs e )
        {
            TextBox Input = sender as TextBox;
            EditTarget.TargetUrl = Input.Text;
        }

        private void RemovePropDef( object sender, RoutedEventArgs e )
        {
            Button B = sender as Button;
            EditTarget.PropDefs.Remove( B.DataContext as WenkuExtractor.PropExt );
        }

        private void SetIncoming( object sender, RoutedEventArgs e )
        {
            EditTarget.Incoming = ( bool ) IncomingCheck.IsChecked;
        }

        private void Subprocess( object sender, RoutedEventArgs e )
        {
            WenkuExtractor.PropExt PropDef = ( sender as Button ).DataContext as WenkuExtractor.PropExt;
            EditTarget.SubEdit = PropDef;
            Popups.CloseDialog();
        }

        private void ChangeType( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count == 0 ) return;

            ComboBox Cb = sender as ComboBox;
            GenericData<BookInfo> NType = e.AddedItems[ 0 ] as GenericData<BookInfo>;

            WenkuExtractor.PropExt Ext = Cb.DataContext as WenkuExtractor.PropExt;
            Ext.PType = NType.Data;
        }
    }
}
