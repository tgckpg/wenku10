using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;

using GR.Config;
using GR.Database.Contexts;
using GR.Database.Models;
using GR.Ext;
using GR.GSystem;
using GR.Storage;
using GR.Model.Book.Spider;
using GR.Model.ListItem;
using GR.Model.Loaders;
using GR.Model.Book;
using GR.Resources;
using GR.Settings;

namespace wenku10.Pages.Settings.Migrations
{
	public sealed partial class M0000 : Page
	{
		public M0000()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			var j = Task.Run( MBackup );
		}

		private string MesgLog0 = "";
		private string MesgLog1 = "";

		private void UpdateProgress()
		{
			var j = Dispatcher.RunIdleAsync( x => ProgressText.Text = MesgLog0 + MesgLog1 );
		}

		private void Mesg( string Text )
		{
			MesgLog0 += Text + "\n";
			MesgLog1 = "";
			UpdateProgress();
		}

		private void MesgR( string Text )
		{
			MesgLog1 = Text;
			UpdateProgress();
		}

		private async Task MBackup()
		{
			Mesg( "Removing Caches ..." );
			Shared.Storage.PurgeContents( "Cache/", true );

			string ZPath = "";
			try
			{
				IStorageFile ZM000 = await ApplicationData.Current.TemporaryFolder.GetFileAsync( "M000.zip" );
				ZPath = ZM000.Path;
				await ZM000.DeleteAsync();
			}
			catch ( FileNotFoundException )
			{
				ZPath = Path.Combine( ApplicationData.Current.TemporaryFolder.Path, "M000.zip" );
			}

			Mesg( "Backing up your files, this might take a while." );
			// await Task.Run( () => ZipFile.CreateFromDirectory( ApplicationData.Current.LocalFolder.Path, ZPath ) );

			Mesg( "Creating Database Contexts" );
			GR.Database.ContextManager.Migrate();

			Mesg( "Theme - ContentReader" );
			await M0001_ContentReader_Theme();

			Mesg( "Books - Type S" );
			await M0002_Books_TypeS();

			Mesg( "Books - Type L" );
			await M0003_Books_TypeL();

			Mesg( "Books - Type W" );
			await M0004_Books_TypeW();

			Mesg( "Reading History" );
			M0005_ReadingHistory();

			Mesg( "Storage - Favs" );
			await M0006_LocalBookStorage();
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

			await Dirs.ExecEach( async ( Id, i ) =>
			{
				MesgR( string.Format( "Processing ... ( {1} / {2} ): {0}", Id, i + 1, l ) );
				Book Entry = null;
				string SRoot = SVolRoot + Id + "/";

				if ( Id[ 0 ] == 'Z' )
				{

				}
				else
				{
					BookInstruction BkInst = new BookInstruction( AppKeys.ZLOCAL, Id );

					XRegistry MetaDefs = new XRegistry( "<metadata />", SRoot + "METADATA.xml" );
					XParameter MetaParam = MetaDefs.Parameter( "METADATA" );

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

					Entry = BkInst.Entry;
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
								ChapterContent ChCont = new ChapterContent()
								{
									Chapter = Ch,
									Text = Shared.Storage.GetString( ChLocation )
								};

								Shared.BooksDb.ChapterContents.Add( ChCont );
							}

							ChParam = VolParam.Parameter( "EpInst" + ( ++ei ) );
						}

						Entry.Volumes.Add( Vol );

						VolParam = TOCDefs.Parameter( "VolInst" + ( ++vi ) );
					}

					SpiderBook SBk = await SpiderBook.CreateSAsync( AppKeys.ZLOCAL, Id, MetaDefs.Parameter( "Procedures" ) );
					SBk.PSettings.Save();
				}

				if ( Entry != null )
				{
					Entries.Add( Entry );
				}
			} );

			MesgR( "Saving records" );
			Shared.BooksDb.SaveBooks( Entries.ToArray() );
		}

		private async Task M0003_Books_TypeL() { }

		private async Task M0004_Books_TypeW()
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
				MesgR( string.Format( "Processing ... ( {1} / {2} ): {0}", Id, i + 1, l ) );
				BookItem Item = X.Instance<BookItem>( XProto.BookItemEx, Id );

				if ( Shared.Storage.FileExists( IntroRoot + Id + ".txt" ) )
				{
					Item.Info.LongDescription = Shared.Storage.GetString( IntroRoot + Id + ".txt" );
				}

				await Item.XCallAsync<bool>( "Migrate", VRoot + Id );
				Entries.Add( Item.Entry );
			} );

			MesgR( "Saving records" );
			Shared.BooksDb.SaveBooks( Entries.ToArray() );

			Mesg( "Purging IntroRoot" );
			Shared.Storage.PurgeContents( IntroRoot, true );

			Mesg( "Purging Volume Root" );
			Shared.Storage.PurgeContents( VRoot, true );
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
			foreach ( FavItem Item in BkStore.GetList<FavItem>() )
			{
				BookItem Bk = X.Instance<BookItem>( XProto.BookItemEx, Item.Payload );
				Bk.Entry.Fav = true;
				Bk.Title = Item.Name;

				Bk.Info.LatestSection = Item.Desc.Replace( BookItem.PropertyName( PropType.LatestSection ) + ": ", "" );
				Bk.Info.LastUpdateDate = Item.Desc2;

				Books.Add( Bk.Entry );
			}

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
	}
}
