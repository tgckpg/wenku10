using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
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

			if ( b.Volumes == null )
			{
				b.Entry.Volumes = await Shared.BooksDb.LoadCollectionAsync( b.Entry, x => x.Volumes, x => x.Index );
			}

			if ( b.IsLocal() || ( useCache && !b.NeedUpdate && b.Volumes.Any() ) )
			{
				foreach ( Volume Vol in b.Volumes )
				{
					if ( Vol.Chapters == null )
					{
						Vol.Chapters = await Shared.BooksDb.LoadCollectionAsync( Vol, x => x.Chapters, x => x.Index );
					}
				}

				OnComplete( b );
			}
			else if ( b.IsSpider() )
			{
				var j = Task.Run( () => LoadInst( ( BookInstruction ) b ) );
			}
			else if ( b.IsEx() )
			{
				IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
				// This occurs when tapping pinned book but cache is cleared
				wCache.InitDownload(
					b.ZItemId
					, X.Call<XKey[]>( XProto.WRequest, "GetBookTOC", b.ZItemId )
					, ( DRequestCompletedEventArgs e, string id ) =>
					{
						b.XCall( "ParseVolume", Shared.Conv.Chinese.Translate( e.ResponseString ) );
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

		private async void LoadInst( BookInstruction b )
		{
			if ( !BookInstruction.OpLocks.AcquireLock( b.GID, out AsyncLocks<string, bool>.QueueToken QT ) )
			{
				await QT.Task;
			}

			foreach ( VolInstruction VInst in b.GetVolInsts() )
			{
				Shared.LoadMessage( "SubProcessRun", VInst.Title );
				// This should finalize the chapter info
				var Convoy = await VInst.Process( b );
			}

			Shared.LoadMessage( "CompilingTOC", b.Title );

			IEnumerable<Volume> Vols = b.GetVolInsts().Remap( x => x.ToVolume( b.Entry ) );

			if ( Vols.Any() )
			{
				List<Volume> NewVolumes = new List<Volume>();
				Vols.ExecEach( Vol =>
				{
					string VID = Vol.Meta[ AppKeys.GLOBAL_VID ];
					Volume NVol = b.Entry.Volumes.FirstOrDefault( x => x.Meta[ AppKeys.GLOBAL_VID ] == VID ) ?? Vol;
					if ( NVol != Vol )
					{
						NVol.Title = Vol.Title;
						NVol.Index = Vol.Index;
						NVol.Json_Meta = Vol.Json_Meta;
					}

					Shared.BooksDb.LoadCollection( NVol, x => x.Chapters, x => x.Index );

					List<Chapter> NewChapters = new List<Chapter>();
					Vol.Chapters.ExecEach( Ch =>
					{
						string CID = Ch.Meta[ AppKeys.GLOBAL_CID ];
						Chapter NCh = NVol.Chapters.FirstOrDefault( x => x.Meta[ AppKeys.GLOBAL_CID ] == CID ) ?? Ch;
						if ( NCh != Ch )
						{
							NCh.Title = Ch.Title;
							NCh.Index = Ch.Index;
							NCh.Json_Meta = Ch.Json_Meta;
						}
						NewChapters.Add( NCh );
					} );

					NewVolumes.Add( NVol );
				} );

				b.Entry.Volumes = NewVolumes;
				b.SaveInfo();
			}
			else
			{
				MessageBus.SendUI( GetType(), AppKeys.HS_NO_VOLDATA, b );
			}

			QT.TrySetResult( true );
			OnComplete( b );
		}

		private void OnComplete( BookItem b )
		{
			Worker.UIInvoke( () => CompleteHandler( b ) );
		}
	}
}