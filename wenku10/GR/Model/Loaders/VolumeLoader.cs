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
				b.Entry.Volumes = await Shared.BooksDb.Entry( b.Entry ).Collection( x => x.Volumes ).Query().OrderBy( x => x.Index ).ToListAsync();
			}

			if ( b.IsLocal() || ( useCache && !b.NeedUpdate && b.Volumes.Any() ) )
			{
				foreach ( Volume Vol in b.Volumes )
				{
					if ( Vol.Chapters == null )
					{
						Vol.Chapters = await Shared.BooksDb.Entry( Vol ).Collection( x => x.Chapters ).Query().OrderBy( x => x.Index ).ToListAsync();
					}
				}

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
					b.ZItemId
					, X.Call<XKey[]>( XProto.WRequest, "GetBookTOC", b.ZItemId )
					, ( DRequestCompletedEventArgs e, string id ) =>
					{
						b.XCall( "ParseVolume", Manipulation.PatchSyntax( Shared.TC.Translate( e.ResponseString ) ) );
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
				b.Entry.Volumes.Clear();
				b.Entry.Volumes.AddRange( Vols );
				b.SaveInfo();
			}
			else
			{
				MessageBus.SendUI( GetType(), AppKeys.HS_NO_VOLDATA, b );
			}

			OnComplete( b );
		}

		private void OnComplete( BookItem b )
		{
			Worker.UIInvoke( () => CompleteHandler( b ) );
		}
	}
}