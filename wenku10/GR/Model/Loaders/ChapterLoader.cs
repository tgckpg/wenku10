using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using libtaotu.Controls;
using libtaotu.Models.Procedure;

using wenku10;

namespace GR.Model.Loaders
{
	using Book.Spider;
	using Ext;
	using Database.Models;
	using ListItem;
	using Resources;
	using Settings;

	sealed class ChapterLoader
	{
		public static readonly string ID = typeof( ChapterLoader ).Name;

		public Model.Book.BookItem CurrentBook { get; private set; }

		private Action<Chapter> CompleteHandler;

		public bool ProtoMode { get; private set; }

		public ChapterLoader( Model.Book.BookItem b, Action<Chapter> CompleteHandler )
		{
			ProtoMode = true;
			CurrentBook = b;
			this.CompleteHandler = CompleteHandler;
		}

		public ChapterLoader( Action<Chapter> CompleteHandler = null )
		{
			ProtoMode = false;
			if( CompleteHandler == null )
			{
				this.CompleteHandler = x => { };
			}
			else
			{
				this.CompleteHandler = CompleteHandler;
			}
		}

		public async Task LoadAsync( Chapter C, bool Cache = true )
		{
			if( C.Content == null )
			{
				await Shared.BooksDb.Entry( C ).Reference( b => b.Content ).LoadAsync();
			}

			if ( Cache && C.Content != null )
			{
				OnComplete( C );
			}
			else if ( C.Book.Type == BookType.S )
			{
				LoadChapterInst( C, ( BookInstruction ) CurrentBook );
			}
			else
			{
				if ( !ProtoMode )
					throw new InvalidOperationException( "ChapterLoader is in Bare mode" );

				IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );

				// Cancel thread if there is same job downloading
				App.RuntimeTransfer.CancelThread( C.Id.ToString() );

				// Initiate download, precache should not be done internally.
				wCache.InitDownload(
					C.Id.ToString()
					, X.Call<XKey[]>( XProto.WRequest, "GetBookContent", CurrentBook.ZItemId, C.Meta[ AppKeys.GLOBAL_CID ] )
					, async ( DRequestCompletedEventArgs e, string path ) =>
					{
						await new ContentParser().ParseAsync( Shared.TC.Translate( e.ResponseString ), C );
						OnComplete( C );

						X.Instance<IDeathblow>( XProto.Deathblow, CurrentBook ).Check( e.ResponseBytes );
					}
					, ( string Request, string path, Exception ex ) =>
					{
						Logger.Log( ID, ex.Message, LogType.ERROR );
						GSystem.ActionCenter.Instance.ShowError( "Download" );
						// OnComplete( C );
					}
					, false
				);
			}
		}

		public async void Load( Chapter C, bool Cache = true )
		{
			await LoadAsync( C, Cache );
		}

		private async void LoadChapterInst( Chapter C, BookInstruction BkInst )
		{
			XRegistry Settings = SpiderBook.GetSettings( BkInst.ZoneId, BkInst.ZItemId );

			EpInstruction Inst = new EpInstruction( C, Settings );
			IEnumerable<ProcConvoy> Convoys = await Inst.Process();

			C.Content = new ChapterContent { Text = "" };

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
					C.Content.Text += await ( ( IStorageFile ) Convoy.Payload ).ReadString();
				}
				else if ( Convoy.Payload is IEnumerable<IStorageFile> )
				{
					foreach ( IStorageFile ISF in ( ( IEnumerable<IStorageFile> ) Convoy.Payload ) )
					{
						Shared.LoadMessage( "MergingContents", ISF.Name );
						C.Content.Text += ( await ISF.ReadString() ) + "\n";
					}
				}
			}

			Shared.BooksDb.Chapters.Update( C );
			await Shared.BooksDb.SaveChangesAsync();

			OnComplete( C );
		}

		private void OnComplete( Chapter C )
		{
			Worker.UIInvoke( () => CompleteHandler( C ) );
		}
	}
}