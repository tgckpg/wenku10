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

using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using GFlow.Controls;
using GFlow.Models.Procedure;

using GR.Database.Models;
using GR.Model.Book.Spider;
using GR.Settings;

namespace wenku10.Pages.Viewers
{
	using ContentReaderPane;

	public sealed partial class GRMarkerView : Page
	{
		private static readonly string ID = typeof( GRMarkerView ).Name;
		private BookInstruction TempInst;

		public GRMarkerView()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			if ( e.Parameter is ProcConvoy Convoy )
			{
				ViewConvoy( Convoy );
			}
		}

		private void ViewConvoy( ProcConvoy Convoy )
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

			ViewFrame.Navigate(
				typeof( TableOfContents )
				, new Tuple<Volume[], Action<Chapter>>( TempInst.GetVolInsts().Remap( x => x.ToVolume( TempInst.Entry ) ), ViewChapter )
			);

			ViewFrame.BackStack.Clear();
		}

		private async void ViewChapter( Chapter Ch )
		{
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

			StringResources stx = StringResources.Load( "LoadingMessage" );

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

			ViewFrame.Navigate( typeof( PlainTextView ), TempFile );
		}

		private void FrameGoBack() => ViewFrame.GoBack();

	}
}
