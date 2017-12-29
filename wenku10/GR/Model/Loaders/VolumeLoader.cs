using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

namespace GR.Model.Loaders
{
	using Ext;
	using Book;
	using Book.Spider;
	using Database.Models;
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

		public async void Load( BookItem b, bool useCache = true )
		{
			// b is null when back button is pressed before BookLoader load
			if ( b == null ) return;

			Shared.LoadMessage( "LoadingVolumes" );
			CurrentBook = b;

			if( CurrentBook.Volumes == null )
			{
				await Shared.BooksDb.Entry( b.Entry ).Collection( x => x.Volumes ).LoadAsync();
			}

			if ( b.IsLocal() || ( useCache && !b.NeedUpdate && CurrentBook.Volumes.Any() ) )
			{
				foreach( Volume Vol in CurrentBook.Volumes )
					await Shared.BooksDb.Entry( Vol ).Collection( x => x.Chapters ).LoadAsync();

				OnComplete( b );
			}
			else if ( b.IsSpider() )
			{
				LoadInst( ( BookInstruction ) b );
			}
			else if ( b.IsEx() )
			{
				IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
				// This occurs when tapping pinned book but cache is cleared
				wCache.InitDownload(
					CurrentBook.ZItemId
					, X.Call<XKey[]>( XProto.WRequest, "GetBookTOC", CurrentBook.ZItemId )
					, ( DRequestCompletedEventArgs e, string id ) =>
					{
						CurrentBook.ParseVolumeData( Manipulation.PatchSyntax( Shared.TC.Translate( e.ResponseString ) ) );
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
			throw new NotImplementedException();
			Volume[] Vols = b.GetVolumes();
			foreach ( Volume Vol in Vols )
			{
				Shared.LoadMessage( "SubProcessRun", Vol.Title );
				// This should finalize the chapter info
				// await Vol.SubProcRun( b );
			}

			if( Vols.Count() == 0 )
			{
				MessageBus.SendUI( GetType(), AppKeys.HS_NO_VOLDATA, b );
			}

			Shared.LoadMessage( "CompilingTOC", b.Title );
			// await b.SaveTOC( Vols );
			Shared.Storage.WriteString( b.TOCDatePath, GSystem.Utils.Md5( Shared.Storage.GetBytes( b.TOCPath ).AsBuffer() ) );
			OnComplete( b );
		}

		private void OnComplete( BookItem b )
		{
			Worker.UIInvoke( () => CompleteHandler( b ) );
		}
	}
}