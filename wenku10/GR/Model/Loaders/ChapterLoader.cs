using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using GFlow.Controls;
using GFlow.Models.Procedure;

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

		public async void Load( Chapter C, bool Cache = true )
		{
			if( C.Content == null )
			{
				await Task.Run( () => Shared.BooksDb.LoadRef( C, b => b.Content ) );
				await Task.Run( () => Shared.BooksDb.LoadRef( C, b => b.Image ) );
			}

			if ( Cache && C.Content != null )
			{
				OnComplete( C );
			}
			else if ( C.Book.Type == BookType.S )
			{
				LoadChapterInst( C );
			}
			else if( C.Book.Type.HasFlag( BookType.W ) )
			{
				if ( C.Book.Type.HasFlag( BookType.L ) )
				{
					IDeathblow Db = X.Instance<IDeathblow>( XProto.Deathblow, CurrentBook );
					MessageBus.SendUI( GetType(), AppKeys.EX_DEATHBLOW, Db );
				}
				else
				{
					IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );

					// Initiate download, precache should not be done internally.
					wCache.InitDownload(
						C.Id.ToString()
						, X.Call<XKey[]>( XProto.WRequest, "GetBookContent", CurrentBook.ZItemId, C.Meta[ AppKeys.GLOBAL_CID ] )
						, async ( DRequestCompletedEventArgs e, string path ) =>
						{
							await new ContentParser().ParseAsync( Shared.Conv.Chinese.Translate( e.ResponseString ), C );
							OnComplete( C );

							X.Instance<IDeathblow>( XProto.Deathblow, CurrentBook ).Check( e.ResponseBytes );
						}
						, ( string Request, string path, Exception ex ) =>
						{
							Logger.Log( ID, ex.Message, LogType.ERROR );
							GSystem.ActionCenter.Instance.ShowError( "Download" );
							OnComplete( C );
						}
						, false
					);
				}
			}
		}

		private async void LoadChapterInst( Chapter C )
		{
			BookInstruction BkInst = ( BookInstruction ) CurrentBook ?? new BookInstruction( C.Book );
			XRegistry Settings = SpiderBook.GetSettings( BkInst.ZoneId, BkInst.ZItemId );

			EpInstruction Inst = new EpInstruction( C, Settings );
			IEnumerable<ProcConvoy> Convoys = await Inst.Process();

			string ChapterText = "";

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
					ChapterText += await ( ( IStorageFile ) Convoy.Payload ).ReadString();
				}
				else if ( Convoy.Payload is IEnumerable<IStorageFile> )
				{
					foreach ( IStorageFile ISF in ( ( IEnumerable<IStorageFile> ) Convoy.Payload ) )
					{
						Shared.LoadMessage( "MergingContents", ISF.Name );
						ChapterText += ( await ISF.ReadString() ) + "\n";
					}
				}
			}

			await new ContentParser().ParseAsync( ChapterText, C );

			OnComplete( C );
		}

		private void OnComplete( Chapter C )
		{
			Worker.UIInvoke( () => CompleteHandler( C ) );
		}
	}
}