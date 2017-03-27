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
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
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

			MessageBus.OnDelivery += MessageBus_OnDelivery;
		}

		public void Dispose()
		{
			MessageBus.OnDelivery -= MessageBus_OnDelivery;
			if ( PreviewFile != null )
			{
				try
				{
					var j = PreviewFile.DeleteAsync();
				}
				catch( Exception ) { }
			}
		}

		~EditProcExtract() { Dispose(); }

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
			if ( TestRunning.IsActive ) return;

			string Url = UrlInput.Text.Trim();
			TestRunning.IsActive = true;

			if ( string.IsNullOrEmpty( Url ) )
			{
				MessageBus.SendUI( typeof( ProceduresPanel ), "RUN", EditTarget );
				return;
			}

			try
			{
				if ( PreviewFile == null )
					PreviewFile = await ProceduralSpider.DownloadSource( Url );

				// The resulting convoy may not be the book instruction originally created
				ProcConvoy Convoy = await new ProceduralSpider( new Procedure[] { EditTarget } )
					.Crawl( new ProcConvoy( new ProcDummy(), PreviewFile ) );

				// So we trackback the Book Convoy
				Convoy = ProcManager.TracePackage( Convoy, ( D, C ) => C.Payload is BookInstruction );

				if ( Convoy == null )
				{
					throw new Exception( "Unable to find the generated book convoy" );
				}

				await ViewTestResult( ( BookInstruction ) Convoy.Payload );
			}
			catch ( Exception ex )
			{
				ProcManager.PanelMessage( ID, ex.Message, LogType.INFO );
			}

			TestRunning.IsActive = false;
		}

		private void AddPropDef( object sender, RoutedEventArgs e )
		{
			EditTarget.PropDefs.Add( new WenkuExtractor.PropExt() );
		}

		private void SetPattern( object sender, RoutedEventArgs e )
		{
			TextBox Input = ( TextBox ) sender;
			ProcFind.RegItem Item = ( ProcFind.RegItem ) Input.DataContext;
			Item.Pattern = Input.Text;

			Item.Validate( FindMode.MATCH );
		}

		private void SetFormat( object sender, RoutedEventArgs e )
		{
			TextBox Input = ( TextBox ) sender;
			ProcFind.RegItem Item = ( ProcFind.RegItem ) Input.DataContext;
			Item.Format = Input.Text;

			Item.Validate( FindMode.MATCH );
		}

		private void SetUrl( object sender, RoutedEventArgs e )
		{
			TextBox Input = ( TextBox ) sender;
			EditTarget.TargetUrl = Input.Text;
		}

		private void RemovePropDef( object sender, RoutedEventArgs e )
		{
			Button B = ( Button ) sender;
			EditTarget.PropDefs.Remove( ( GrimoireExtractor.PropExt ) B.DataContext );
		}

		private void SetIncoming( object sender, RoutedEventArgs e )
		{
			EditTarget.Incoming = ( bool ) IncomingCheck.IsChecked;
		}

		private void Subprocess( object sender, RoutedEventArgs e )
		{
			GrimoireExtractor.PropExt PropDef = ( GrimoireExtractor.PropExt ) ( ( Button ) sender ).DataContext;
			EditTarget.SubEdit = PropDef;
			Popups.CloseDialog();
		}

		private void ChangeType( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count == 0 ) return;

			ComboBox Cb = sender as ComboBox;
			GenericData<BookInfo> NType = e.AddedItems[ 0 ] as GenericData<BookInfo>;

			GrimoireExtractor.PropExt Ext = ( GrimoireExtractor.PropExt ) Cb.DataContext;
			Ext.PType = NType.Data;
		}

		private void MessageBus_OnDelivery( Message Mesg )
		{
			ProcConvoy Convoy = Mesg.Payload as ProcConvoy;
			if ( Mesg.Content == "RUN_RESULT"
				&& Convoy != null
				&& Convoy.Dispatcher == EditTarget )
			{
				TestRunning.IsActive = false;

				BookInstruction BookInst = Convoy.Payload as BookInstruction;
				if( BookInst != null )
				{
					var j = ViewTestResult( BookInst );
				}
			}
		}

		private async Task ViewTestResult( BookInstruction Payload )
		{
			if ( PreviewFile == null )
				PreviewFile = await AppStorage.MkTemp();

			await PreviewFile.WriteString( Payload.PlainTextInfo );

			var j = Dispatcher.RunIdleAsync(
				x => Frame.Navigate( typeof( DirectTextViewer ), PreviewFile )
			);
		}

	}
}