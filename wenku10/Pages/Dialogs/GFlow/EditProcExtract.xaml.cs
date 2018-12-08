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

using GFlow.Crawler;
using GFlow.Models.Procedure;

using GR.GFlow;
using GR.Model.Book;
using GR.Model.Book.Spider;

namespace wenku10.Pages.Dialogs.GFlow
{
	sealed partial class EditProcExtract : Page
	{
		public static readonly string ID = typeof( EditProcExtract ).Name;

		private IStorageFile PreviewFile;
		private GrimoireExtractor EditTarget;
		private ProceduralSpider MCrawler = new ProceduralSpider( new Procedure[ 0 ] );

		public EditProcExtract()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			if ( e.Parameter is GrimoireExtractor EditTarget )
			{
				this.EditTarget = EditTarget;

				if ( EditTarget.ProcessNodes.Count == 0 )
				{
					EditTarget.ProcessNodes.Add( new GrimoireExtractor.PropExt( PropType.Title ) );
				}

				IncomingCheck.IsOn = EditTarget.Incoming;

				LayoutRoot.DataContext = EditTarget;

				if ( !string.IsNullOrEmpty( EditTarget.TargetUrl ) )
				{
					UrlInput.Text = EditTarget.TargetUrl;
				}
			}
		}

		private void AddPropDef( object sender, RoutedEventArgs e )
		{
			EditTarget.ProcessNodes.Add( new GrimoireExtractor.PropExt() );
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
			EditTarget.ProcessNodes.Remove( ( GrimoireExtractor.PropExt ) B.DataContext );
		}

		private void SetIncoming( object sender, RoutedEventArgs e )
		{
			EditTarget.Incoming = IncomingCheck.IsOn;
		}

		private void ChangeType( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count == 0 ) return;

			ComboBox Cb = sender as ComboBox;
			GenericData<PropType> NType = e.AddedItems[ 0 ] as GenericData<PropType>;

			GrimoireExtractor.PropExt Ext = ( GrimoireExtractor.PropExt ) Cb.DataContext;
			Ext.PType = NType.Data;

			MessageBus.Send( typeof( global::GFlow.Pages.GFEditor ), "REDRAW" );
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