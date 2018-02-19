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
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using libtaotu.Controls;
using libtaotu.Models.Procedure;
using libtaotu.Pages;

using GR.Database.Models;
using GR.Model.Book.Spider;
using GR.Model.ListItem;
using GR.Settings;
using GR.Taotu;

using wenku10.Pages.ContentReaderPane;

namespace wenku10.Pages.Dialogs.Taotu
{
	sealed partial class EditProcMark : ContentDialog, IDisposable
	{
		public static readonly string ID = typeof( EditProcExtract ).Name;

		WenkuMarker EditTarget;

		private BookInstruction TempInst;

		private EditProcMark()
		{
			this.InitializeComponent();
			SetTemplate();

			MessageBus.Subscribe( this, MessageBus_OnDelivery );
		}

		public void Dispose()
		{
			MessageBus.Unsubscribe( this, MessageBus_OnDelivery );
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
				TempInst = Convoy.Payload as BookInstruction;

				ProcConvoy ProcCon = ProcManager.TracePackage( Convoy, ( P, C ) => P is ProcParameter );
				if ( ProcCon != null )
				{
					ProcParameter PPClone = new ProcParameter();
					PPClone.ReadParam( ProcCon.Dispatcher.ToXParam() );
					ProcCon = new ProcConvoy( PPClone, null );
				}

				TempInst.PackVolumes( ProcCon );

				Preview.Navigate(
					typeof( TableOfContents )
					, new Tuple<Volume[], SelectionChangedEventHandler>( TempInst.GetVolInsts().Remap( x => x.ToVolume( TempInst.Entry ) ), PreviewContent )
				);
				Preview.BackStack.Clear();
				TestRunning.IsActive = false;
			}
		}

		private async void PreviewContent( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count == 0 || TestRunning.IsActive ) return;
			TestRunning.IsActive = true;

			TOCItem Item = ( TOCItem ) e.AddedItems[ 0 ];
			Chapter Ch = Item.GetChapter();

			if ( Ch == null )
			{
				ProcManager.PanelMessage( ID, "Chapter is not available", LogType.INFO );
				return;
			}

			string VId = Ch.Volume.Meta[ AppKeys.GLOBAL_VID ];
			string CId = Ch.Meta[ AppKeys.GLOBAL_CID ];
			EpInstruction EpInst = TempInst.GetVolInsts().First( x => x.VId == VId ).EpInsts.Cast<EpInstruction>().First( x => x.CId == CId );
			IEnumerable<ProcConvoy> Convoys = await EpInst.Process();

			StorageFile TempFile = await AppStorage.MkTemp();

			StringResources stx = new StringResources( "LoadingMessage" );

			foreach ( ProcConvoy Konvoi in Convoys )
			{
				ProcConvoy Convoy = ProcManager.TracePackage(
					Konvoi
					, ( d, c ) =>
					c.Payload is IEnumerable<IStorageFile>
					|| c.Payload is IStorageFile
				);

				if ( Convoy == null ) continue;

				if ( Convoy.Payload is IStorageFile )
				{
					await TempFile.WriteFile( ( IStorageFile ) Convoy.Payload, true, new byte[] { ( byte ) '\n' } );
				}
				else if ( Convoy.Payload is IEnumerable<IStorageFile> )
				{
					foreach ( IStorageFile ISF in ( ( IEnumerable<IStorageFile> ) Convoy.Payload ) )
					{
						ProcManager.PanelMessage( ID, string.Format( stx.Str( "MergingContents" ), ISF.Name ), LogType.INFO );
						await TempFile.WriteFile( ISF, true, new byte[] { ( byte ) '\n' } );
					}
				}
			}

			ShowSource( TempFile );
			TestRunning.IsActive = false;
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