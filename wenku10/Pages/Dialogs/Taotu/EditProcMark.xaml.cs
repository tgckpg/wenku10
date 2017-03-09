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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using libtaotu.Controls;
using libtaotu.Models.Procedure;
using libtaotu.Pages;

using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Taotu;

using wenku10.Pages.ContentReaderPane;

namespace wenku10.Pages.Dialogs.Taotu
{
	sealed partial class EditProcMark : ContentDialog, IDisposable
	{
		public static readonly string ID = typeof( EditProcExtract ).Name;

		WenkuMarker EditTarget;

		private EditProcMark()
		{
			this.InitializeComponent();
			SetTemplate();

			MessageBus.OnDelivery += MessageBus_OnDelivery;
		}

		~EditProcMark() { Dispose(); }

		public void Dispose()
		{
			MessageBus.OnDelivery -= MessageBus_OnDelivery;
		}

		private void SetTemplate()
		{
			StringResources stx = new StringResources( "Message" );
			PrimaryButtonText = stx.Str( "OK" );
		}

		public EditProcMark( WenkuMarker P )
			: this()
		{
			EditTarget = P;
			EditTarget.SubEditComplete();
			LayoutRoot.DataContext = P;
		}

		private void SetProp( object sender, RoutedEventArgs e )
		{
			TextBox Input = sender as TextBox;
			EditTarget.SetProp( Input.Tag as string, Input.Text.Trim() );
		}

		private void RunTilHere( object sender, RoutedEventArgs e )
		{
			TestRunning.IsActive = true;
			MessageBus.SendUI( typeof( ProceduresPanel ), "RUN", EditTarget );
		}

		private void MessageBus_OnDelivery( Message Mesg )
		{
			ProcConvoy Convoy = Mesg.Payload as ProcConvoy;
			if ( Mesg.Content == "RUN_RESULT"
				&& Convoy != null
				&& Convoy.Dispatcher == EditTarget )
			{
				BookInstruction TInst = Convoy.Payload as BookInstruction;

				ProcConvoy ProcCon = ProcManager.TracePackage( Convoy, ( P, C ) => P is ProcParameter );
				if ( ProcCon != null )
				{
					ProcParameter PPClone = new ProcParameter();
					PPClone.ReadParam( ProcCon.Dispatcher.ToXParam() );
					ProcCon = new ProcConvoy( PPClone, null );
				}

				TInst.PackVolumes( ProcCon );

				Preview.Navigate(
					typeof( TableOfContents )
					, new Tuple<Volume[], SelectionChangedEventHandler>( TInst.GetVolumes(), PreviewContent )
				);
				Preview.BackStack.Clear();
				TestRunning.IsActive = false;
			}
		}

		private void PreviewContent( object sender, SelectionChangedEventArgs e )
		{
			if ( 0 == e.AddedItems.Count ) return;

			TestRunning.IsActive = true;

			Chapter Ch = ( e.AddedItems[ 0 ] as TOCItem ).GetChapter();

			if( Ch == null )
			{
				ProcManager.PanelMessage( ID, "Chapter is not available", LogType.INFO );
				return;
			}

			new ChapterLoader(
				C => {
					ShowSource( ( C as SChapter ).TempFile );
					TestRunning.IsActive = false;
				}
			).Load( Ch );
		}

		private void ShowSource( StorageFile SF )
		{
			Preview.Navigate( typeof( DirectTextViewer ), SF );
		}

		private void SubVolume( object sender, RoutedEventArgs e )
		{
			EditTarget.SubEdit = WMarkerSub.Volume;
			Popups.CloseDialog();
		}

		private void SubChapter( object sender, RoutedEventArgs e )
		{
			EditTarget.SubEdit = WMarkerSub.Chapter;
			Popups.CloseDialog();
		}

		private void ToggleVAsync( object sender, RoutedEventArgs e ) { EditTarget.VolAsync = !EditTarget.VolAsync; }
		private void ToggleEAsync( object sender, RoutedEventArgs e ) { EditTarget.EpAsync = !EditTarget.EpAsync; }

		private void FrameGoBack( object sender, RoutedEventArgs e )
		{
			if( Preview.CanGoBack ) Preview.GoBack();
		}
	}
}