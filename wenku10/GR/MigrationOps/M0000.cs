using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

namespace GR.MigrationOps
{
	using Config;
	using Database.Contexts;
	using Database.Models;
	using Ext;
	using GSystem;
	using Model.Book.Spider;
	using Model.ListItem;
	using Model.Book;
	using Resources;
	using Settings;
	using Storage;

	sealed class M0000 : IMigrationOp
	{
		public Action<string> Mesg { get; set; }
		public Action<string> MesgR { get; set; }

		public bool ShouldMigrate { get; set; }

		StringResources stx = new StringResBg( "InitQuestions", "Message", "Settings", "NavigationTitles" );

		public M0000()
		{
			ShouldMigrate = !Shared.Storage.FileExists( "books.db" );
		}

		public async Task Up()
		{
			try
			{
				Mesg( stx.Text( "CreatingDatabase" ) );
				Database.ContextManager.Migrate();

				Mesg( stx.Text( "CompressCache" ) );
				await M0000_Caches();

				Mesg( stx.Text( "Appearance_Theme", "Settings" ) );
				await M0001_ContentReader_Theme();

				Mesg( string.Format( stx.Text( "BooksType" ), "S" ) );
				await M0002_Books_TypeS();

				Mesg( string.Format( stx.Text( "BooksType" ), "W" ) );
				await M0003_Books_TypeW();

				Mesg( string.Format( stx.Text( "BooksType" ), "L" ) );
				await M0004_Books_TypeL();

				Mesg( stx.Text( "History", "NavigationTitles" ) );
				M0005_ReadingHistory();

				Mesg( stx.Text( "FavRecords" ) );
				await M0006_LocalBookStorage();

				Mesg( stx.Text( "MigrationComplete" ) );
			}
			catch( Exception ex )
			{
				Mesg( ex.Message );
			}
		}

		private void Purge( string Location )
		{
			MesgR( stx.Text( "PurgingFiles" ) + Location );
			Shared.Storage.PurgeContents( Location, true );
		}

		private Task M0000_Caches()
		{
			string CRoot = "Cache/";

			if ( !Shared.Storage.DirExist( CRoot ) )
				return Task.Delay( 0 );

			string[] Caches = Shared.Storage.ListFiles( CRoot );

			Caches.ExecEach( x => Shared.ZCacheDb.Write( x, Shared.Storage.GetBytes( CRoot + x ) ) );

			return Task.Run( () => Purge( CRoot ) );
		}

		private Task M0001_ContentReader_Theme()
		{
			Type ParamType = typeof( Parameters );
			Type PropType = typeof( Properties );

			using ( SettingsContext Context = new SettingsContext() )
			{
				IEnumerable<FieldInfo> FInfo = ParamType.GetFields();
				foreach ( FieldInfo Info in FInfo )
				{
					string ParamName = Info.Name;
					string ParamValue = ( string ) Info.GetValue( null );
					(string DValue, GSDataType DType) = ValueType( PropType.GetProperty( ParamName ).GetValue( null ) );

					if ( ParamName.Contains( "CONTENTREADER" ) )
					{
						ParamValue = ParamValue.Replace( "Appearance_", "" ).Replace( "ContentReader_", "" );
						InsertOrUpdate( Context.ContentReader, new ContentReader() { Key = ParamValue, Type = DType, Value = DValue } );
					}
					else if( ParamName.Contains( "THEME" ) )
					{
						ParamValue = ParamValue.Replace( "Appearance_", "" ).Replace( "Theme_", "" ).Replace( "Appearence_", "" );
						InsertOrUpdate( Context.Theme, new Theme() { Key = ParamValue, Type = DType, Value = DValue } );
					}
				}

				return Context.SaveChangesAsync();
			}

		}

		private async Task M0002_Books_TypeS()
		{
			string SVolRoot = "shared/transfers/SVolumes/";
			string[] Dirs = Shared.Storage.ListDirs( SVolRoot );

			List<Book> Entries = new List<Book>();
			int l = Dirs.Length;

			await Dirs.Where( x => x != AppKeys.ZLOCAL ).ExecEach( async ( ZZId, i ) =>
			{
				Book Entry = null;

				string SRoot = SVolRoot + ZZId + "/";

				if ( ZZId[ 0 ] == 'Z' )
				{
					// ZZId is ZoneId
					string ZCoverRoot = FileLinks.ROOT_COVER + ZZId;
					if ( Shared.Storage.DirExist( ZCoverRoot ) )
						Shared.Storage.MoveDir( ZCoverRoot, FileLinks.ROOT_COVER + ZZId.Substring( 1 ) );

					ZZId = ZZId.Substring( 1 );

					string[] ZItemIds = Shared.Storage.ListDirs( SRoot );
					int m = ZItemIds.Length;

					await ZItemIds.ExecEach( async ( ZItemId, j ) =>
					{
						MesgR( stx.Text( "MightTakeAWhile" ) + string.Format( "{0}/{1} ( {3} )", ZItemId, j + 1, m, "ZMode" ) );
						MoveCover( ZZId, ZZId, ZItemId );

						string ZSRoot = SRoot + ZItemId + "/";

						Entry = await MigrateBookSpider( ZSRoot, ZZId, ZItemId );
						if( Entry != null )
						{
							Entries.Add( Entry );
						}
					} );
				}
				else
				{
					// ZZId is ZItemId
					MesgR( stx.Text( "MightTakeAWhile" ) + string.Format( "{1}/{2} ( {0} )", ZZId, i + 1, l ) );
					MoveCover( "", AppKeys.ZLOCAL, ZZId );

					Entry = await MigrateBookSpider( SRoot, AppKeys.ZLOCAL, ZZId );

					if ( Entry != null )
						Entries.Add( Entry );
				}
				Purge( SRoot );
			} );

			MesgR( stx.Text( "SavingRecords" ) );
			Shared.BooksDb.SaveBooks( Entries.ToArray() );
		}

		private void MoveCover( string OZoneId, string ZoneId, string ZItemId )
		{
			string CoverPath = FileLinks.ROOT_COVER + OZoneId + "/" + ZItemId + ".jpg";
			if ( Shared.Storage.FileExists( CoverPath ) )
			{
				string NCoverPath = FileLinks.ROOT_COVER + ZoneId + "/" + ZItemId + ".cvr";
				Shared.Storage.MoveFile( CoverPath, NCoverPath );
			}
		}

		private async Task M0003_Books_TypeW()
		{
			if ( !X.Exists )
				return;

			string VRoot = "shared/transfers/Volumes/";
			if ( !Shared.Storage.DirExist( VRoot ) )
				return;

			string IntroRoot = "intro/";

			List<Book> Entries = new List<Book>();
			string[] Dirs = Shared.Storage.ListDirs( VRoot );
			int l = Dirs.Length;

			await Dirs.ExecEach( async ( Id, i ) =>
			{
				MesgR( stx.Text( "MightTakeAWhile" ) + string.Format( "{1}/{2} ( {0} )", Id, i + 1, l ) );
				BookItem Item = X.Instance<BookItem>( XProto.BookItemEx, Id );

				if ( Shared.Storage.FileExists( IntroRoot + Id + ".txt" ) )
				{
					Item.Info.LongDescription = Shared.Storage.GetString( IntroRoot + Id + ".txt" );
				}

				await Item.XCallAsync<bool>( "Migrate", VRoot + Id );
				Entries.Add( Item.Entry );
			} );

			MesgR( stx.Text( "SavingRecords" ) );
			Shared.BooksDb.SaveBooks( Entries.ToArray() );

			Purge( IntroRoot );
			Purge( VRoot );
		}

		private async Task M0004_Books_TypeL()
		{
			string LRoot = "shared/transfers/LVolumes/";

			if ( !Shared.Storage.DirExist( LRoot ) )
				return;

			string[] Ids = Shared.Storage.ListDirs( LRoot );

			int l = Ids.Length;
			List<Book> Entries = new List<Book>();
			await Ids.ExecEach( async ( Id, i ) =>
			{
				MesgR( stx.Text( "MightTakeAWhile" ) + string.Format( "{1}/{2} ( {0} )", Id, i + 1, l ) );

				Book Entry = null;
				if ( int.TryParse( Id, out int k ) && X.Exists )
				{
					BookItem Item = X.Instance<BookItem>( XProto.BookItemEx, Id );
					Entry = Item.Entry;
					Entry.Type = Entry.Type | BookType.L;
				}
				else
				{
					BookItem Item = new LocalTextDocument( Id );
					Entry = Item.Entry;
				}

				await Shared.BooksDb.LoadCollection( Entry, x => x.Volumes, x => x.Index );
				Entry.Volumes.Clear();

				string BRoot = LRoot + Id + "/";
				string MetaLoc = BRoot + "METADATA.xml";

				XRegistry XMeta = new XRegistry( "<Meta />", MetaLoc );

				XParameter[] VolDefs = XMeta.Parameters( "vid" );

				VolDefs.ExecEach( ( VolDef, vi ) =>
				{
					Volume Vol = new Volume()
					{
						Book = Entry,
						Title = VolDef.GetValue( "name" ),
						Chapters = new List<Chapter>(),
						Index = vi
					};

					Vol.Meta[ AppKeys.GLOBAL_VID ] = VolDef.Id;
					XRegistry ChReg = new XRegistry( "<ch />", BRoot + VolDef.Id + ".vol" );
					XParameter[] ChDefs = ChReg.Parameters( "cid" );

					ChDefs.ExecEach( ( ChDef, ei ) =>
					{
						Chapter Ch = new Chapter()
						{
							Book = Entry,
							Volume = Vol,
							Title = ChDef.GetValue( "name" ),
							Index = ei
						};

						Ch.Meta[ AppKeys.GLOBAL_CID ] = ChDef.Id;

						string ChLocation = BRoot + VolDef.Id + "/" + ChDef.Id + ".txt";
						if ( Shared.Storage.FileExists( ChLocation ) )
						{
							ChapterContent ChCont = new ChapterContent() { Chapter = Ch, };
							ChCont.Data.BytesValue = Shared.Storage.GetBytes( ChLocation );
							Shared.BooksDb.ChapterContents.Add( ChCont );
						}

						Vol.Chapters.Add( Ch );
					} );

					Entry.Volumes.Add( Vol );
				} );

				Entries.Add( Entry );
			} );

			MesgR( stx.Text( "SavingRecords" ) );
			Shared.BooksDb.SaveBooks( Entries.ToArray() );

			Purge( LRoot );
		}

		private void M0005_ReadingHistory()
		{
			XRegistry XReg = new XRegistry( "<n/>", FileLinks.ROOT_SETTING + "ReadingHistory.xml" );
			XParameter[] XParams = XReg.Parameters();
			foreach( XParameter XParam  in XParams )
			{
				string Id = XParam.Id;
				string Name = XParam.GetValue( "name" );
				string Date = XParam.GetValue( "date" );

				if ( int.TryParse( Id, out int aid ) )
				{
					BookItem Bk = X.Instance<BookItem>( XProto.BookItemEx, Id );
					Bk.Title = Name;

					if ( DateTime.TryParse( Date, out DateTime dt ) )
					{
						Bk.Entry.LastAccess = dt;
					}
				}
			}

			Shared.BooksDb.SaveChanges();
		}

		private async Task M0006_LocalBookStorage()
		{
			BookStorage BkStore = new BookStorage();
			await BkStore.SyncSettings();

			List<Book> Books = new List<Book>();
			foreach ( string[] Item in BkStore.GetList() )
			{
				string Id = Item[ 0 ];

				BookItem Bk = null;
				if( int.TryParse( Id, out int NOP ) )
				{
					Bk = X.Instance<BookItem>( XProto.BookItemEx, Id );
				}
				else
				{
					if( Id[0] == 'Z' )
					{
						string[] sId = Id.Split( '/' );
						Bk = new BookInstruction( sId[ 0 ].Substring( 1 ), sId[ 1 ] );
					}
					else
					{
						Bk = new BookInstruction( AppKeys.ZLOCAL, Id );
					}
				}

				Bk.Entry.Fav = true;
				Bk.Title = Item[ 1 ];

				Bk.Info.LastUpdateDate = Item[ 2 ];
				Bk.Info.LatestSection = Item[ 3 ].Replace( BookItem.PropertyName( PropType.LatestSection ) + ": ", "" );

				Books.Add( Bk.Entry );
			}

			MesgR( stx.Text( "SavingRecords" ) );
			Shared.BooksDb.SaveBooks( Books );
		}

		private ( string, GSDataType ) ValueType( object Val )
		{
			if( Val is bool )
			{
				return (( bool ) Val ? "1" : "0", GSDataType.BOOL);
			}
			else if( Val is int )
			{
				return (Val.ToString(), GSDataType.INT);
			}
			else if ( Val is Color )
			{
				return (Val.ToString(), GSDataType.COLOR);
			}
			else if( Val is FontWeight )
			{
				return (( ( FontWeight ) Val ).Weight.ToString(), GSDataType.INT);
			}

			return (Val?.ToString(), GSDataType.STRING);
		}

		private void InsertOrUpdate<T>( DbSet<T> Table, T Entry ) where T : GenericSettings
		{
			T OEntry = Table.FirstOrDefault( e => e.Key == Entry.Key );
			if ( OEntry == null )
			{
				Table.Add( Entry );
			}
			else
			{
				OEntry.Type = Entry.Type;
				OEntry.Value = Entry.Value;
			}
		}

		private async Task<Book> MigrateBookSpider( string SRoot, string ZoneId, string ZItemId )
		{
			if ( !Shared.Storage.FileExists( SRoot + "METADATA.xml" ) )
				return null;

			XRegistry MetaDefs = new XRegistry( "<metadata />", SRoot + "METADATA.xml" );
			XParameter MetaParam = MetaDefs.Parameter( "METADATA" );

			BookInstruction BkInst = new BookInstruction( ZoneId, ZItemId );
			BkInst.Title = MetaParam.GetValue( "Title" );
			BkInst.Info.Author = MetaParam.GetValue( "Author" );
			BkInst.Info.Press = MetaParam.GetValue( "Press" );
			BkInst.Info.LastUpdateDate = MetaParam.GetValue( "LastUpdateDate" );
			BkInst.Info.TotalHitCount = MetaParam.GetValue( "TotalHitCount" );
			BkInst.Info.DailyHitCount = MetaParam.GetValue( "TodayHitCount" );
			BkInst.Info.PushCount = MetaParam.GetValue( "PushCount" );
			BkInst.Info.FavCount = MetaParam.GetValue( "FavCount" );
			BkInst.Info.Length = MetaParam.GetValue( "Length" );
			BkInst.Info.LatestSection = MetaParam.GetValue( "LatestSection" );
			BkInst.Info.LongDescription = MetaParam.GetValue( "Intro" );

			Book Entry = BkInst.Entry;
			await Shared.BooksDb.LoadCollection( Entry, x => x.Volumes, x => x.Index );
			Entry.Volumes.Clear();

			XRegistry TOCDefs = new XRegistry( "<metadata />", SRoot + "/" + "toc.txt" );

			int vi = 0;
			XParameter VolParam = TOCDefs.Parameter( "VolInst" + vi );
			while ( VolParam != null )
			{
				Volume Vol = new Volume()
				{
					Book = Entry,
					Title = VolParam.GetValue( "Title" ),
					Index = VolParam.GetSaveInt( "Index" ),
					Chapters = new List<Chapter>()
				};

				Vol.Meta[ "ProcId" ] = VolParam.GetValue( "ProcId" );
				Vol.Meta[ AppKeys.GLOBAL_VID ] = Utils.Md5( Vol.Title );

				XParameter PParam = VolParam.Parameter( "0" );
				for ( int p = 1; PParam != null; p++ )
				{
					Vol.Meta[ "P" + PParam.Id ] = PParam.GetValue( "Value" );
					PParam = VolParam.Parameter( p.ToString() );
				}

				string MVolHash = Utils.Md5( Vol.Title );

				int ei = 0;
				XParameter ChParam = VolParam.Parameter( "EpInst" + ei );
				while ( ChParam != null )
				{
					Chapter Ch = new Chapter()
					{
						Book = Entry,
						Volume = Vol,
						Title = ChParam.GetValue( "Title" ),
						Index = ChParam.GetSaveInt( "Index" )
					};

					Ch.Meta[ "ProcId" ] = ChParam.GetValue( "ProcId" );
					Ch.Meta[ AppKeys.GLOBAL_CID ] = Utils.Md5( Ch.Title );

					string MChHash = Utils.Md5( Ch.Title );

					PParam = ChParam.Parameter( "0" );
					for ( int p = 1; PParam != null; p++ )
					{
						Ch.Meta[ "P" + PParam.Id ] = PParam.GetValue( "Value" );
						PParam = ChParam.Parameter( p.ToString() );
					}

					Vol.Chapters.Add( Ch );

					string ChLocation = SRoot + MVolHash + "/" + MChHash + ".txt";
					if ( Shared.Storage.FileExists( ChLocation ) )
					{
						ChapterContent ChCont = new ChapterContent() { Chapter = Ch, };
						ChCont.Data.BytesValue = Shared.Storage.GetBytes( ChLocation );
						Shared.BooksDb.ChapterContents.Add( ChCont );
					}

					ChParam = VolParam.Parameter( "EpInst" + ( ++ei ) );
				}

				Entry.Volumes.Add( Vol );

				VolParam = TOCDefs.Parameter( "VolInst" + ( ++vi ) );
			}

			SpiderBook SBk = await SpiderBook.CreateSAsync( ZoneId, ZItemId, MetaDefs.Parameter( "Procedures" ) );
			SBk.PSettings.Save();
			return Entry;
		}

	}
}