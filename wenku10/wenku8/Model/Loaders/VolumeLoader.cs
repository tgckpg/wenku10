using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

namespace wenku8.Model.Loaders
{
	using Ext;
	using Book;
	using Book.Spider;
	using Resources;
	using Text;
	using Settings;

	sealed class VolumeLoader
	{
		public static readonly string ID = typeof( VolumeLoader ).Name;

		public BookItem CurrentBook { get; private set; }

		private Action<BookItem> CompleteHandler;

		public VolumeLoader( Action<BookItem> CompleteHandler )
		{
			this.CompleteHandler = CompleteHandler;
		}

		public void Load( BookItem b, bool useCache = true )
		{
			// b is null when back button is pressed before BookLoader load
			if ( b == null ) return;

			Shared.LoadMessage( "LoadingVolumes" );
			CurrentBook = b;
			if ( b.IsLocal() || ( useCache && !b.NeedUpdate && Shared.Storage.FileExists( CurrentBook.TOCPath ) ) )
			{
				OnComplete( b );
			}
			else if ( b.IsSpider() )
			{
				LoadInst( ( BookInstruction ) b );
			}
			else // wenku8 Protocol
			{
				IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
				// This occurs when tapping pinned book but cache is cleared
				wCache.InitDownload(
					CurrentBook.Id
					, X.Call<XKey[]>( XProto.WRequest, "GetBookTOC", CurrentBook.Id )
					, ( DRequestCompletedEventArgs e, string id ) =>
					{
						Shared.Storage.WriteString( CurrentBook.TOCPath, Manipulation.PatchSyntax( Shared.TC.Translate( e.ResponseString ) ) );
						Shared.Storage.WriteString( CurrentBook.TOCDatePath, CurrentBook.RecentUpdateRaw );
						OnComplete( b );
					}
					, ( string RequestURI, string id, Exception ex ) =>
					{
						OnComplete( b );
					}
					, false
				);
			}
		}

		public async void LoadInst( BookInstruction b )
		{
			IEnumerable<SVolume> Vols = b.GetVolumes().Cast<SVolume>();
			foreach ( SVolume Vol in Vols )
			{
				Shared.LoadMessage( "SubProcessRun", Vol.VolumeTitle );
				// This should finalize the chapter info
				await Vol.SubProcRun( b );
			}

			if( Vols.Count() == 0 )
			{
				MessageBus.SendUI( GetType(), AppKeys.HS_NO_VOLDATA, b );
			}

			Shared.LoadMessage( "CompilingTOC", b.Title );
			await b.SaveTOC( Vols );
			Shared.Storage.WriteString( b.TOCDatePath, System.Utils.Md5( Shared.Storage.GetBytes( b.TOCPath ).AsBuffer() ) );
			OnComplete( b );
		}

		private void OnComplete( BookItem b )
		{
			Worker.UIInvoke( () => CompleteHandler( b ) );
		}
	}
}