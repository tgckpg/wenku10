using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

namespace GR.MigrationOps
{
	using Data;
	using Database.Contexts;
	using Database.Models;
	using Model.Interfaces;
	using Settings;
	using Resources;

	sealed class M0003 : IMigrationOp
	{
		public Action<string> Mesg { get; set; }
		public Action<string> MesgR { get; set; }

		public bool ShouldMigrate { get; set; }

		private const string ROOT_SPIDER_VOL = "shared/transfers/SVolumes/";

		public M0003()
		{
			using ( DbContext Context = new BooksContext() )
			{
				ShouldMigrate = Context.Database.GetPendingMigrations().Contains( "20190211065932_SScript" );
			}
		}

		StringResources stx = StringResources.Load( "InitQuestions" );

		public async Task Up()
		{
			try
			{
				Mesg( stx.Text( "MigrateDatabase" ) );
				using ( DbContext Context = new BooksContext() )
				{
					Context.Database.Migrate();
				}

				Config.GRConfig.MasterExplorer.DefaultWidgets = false;

				Mesg( string.Format( stx.Text( "BooksType" ), "S" ) );
				await M0000_Books_TypeS();

				Mesg( stx.Text( "MigrationComplete" ) + " - M0003" );
			}
			catch ( Exception ex )
			{
				Mesg( ex.Message );
			}
		}

		private async Task M0000_Books_TypeS()
		{
			string SVolRoot = "shared/transfers/SVolumes";
			string ZSRoot = "ZoneSpiders";

			using ( BooksContext Db = new BooksContext() )
			{
				Dictionary<Guid, SScript> ZScripts = new Dictionary<Guid, SScript>();
				Shared.Storage.ListFiles( $"{ZSRoot}/" ).ExecEach( ZFile =>
				{
					string MetaLocation = $"{ZSRoot}/{ZFile}";
					XRegistry XReg = new XRegistry( "<a />", MetaLocation );

					SScript ZScript = new SScript()
					{
						Type = AppKeys.SS_ZS,
						OnlineId = Guid.Parse( XReg.Parameter( "Procedures" ).GetValue( "Guid" ) )
					};

					ZScript.Data.WriteStream( Shared.Storage.GetStream( MetaLocation ) );
					ZScripts[ ( Guid ) ZScript.OnlineId ] = ZScript;
				} );

				IEnumerable<Book> Books = Db.QueryBook( x => x.Type.HasFlag( BookType.S ) );
				foreach( Book Bk in Books )
				{
					string MetaLocation = $"{SVolRoot}/{Bk.ZoneId}/{Bk.ZItemId}.xml";
					if ( Shared.Storage.FileExists( MetaLocation ) )
					{
						XRegistry XReg = new XRegistry( "<a />", MetaLocation );
						XParameter ProcState = XReg.Parameter( "ProcessState" );
						if ( ProcState == null )
						{
							MesgR( $"Process state not found for {Bk.Title}" );
						}
						else
						{
							Bk.Info.Flags.Toggle( "SP_SUCCESS", ProcState.GetBool( "Success" ) );
							Bk.Info.Flags.Toggle( "SP_CHAKRA", ProcState.GetBool( "HasChakra" ) );
						}

						XParameter PPValues = XReg.Parameter( "PPValues" );
						if ( PPValues != null )
						{
							Bk.Meta[ AppKeys.XML_BMTA_PPVALUES ] = PPValues.AsBase64ZString();
						}

						if ( Guid.TryParse( Bk.ZoneId, out Guid ZId ) && ZScripts.TryGetValue( ZId, out SScript ZScript ) )
						{
							Bk.Script = ZScript;
						}
						else
						{
							SScript BoundScript = new SScript() { Type = AppKeys.SS_BS };
							XReg.RemoveParameter( "ProcessState" );
							using ( MemoryStream s = new MemoryStream() )
							{
								XReg.Save( s, SaveOptions.DisableFormatting );
								s.Position = 0;
								BoundScript.Data.WriteStream( s );
							}

							Bk.Script = BoundScript;
						}

						Db.Books.Update( Bk );
					}
				}

				MesgR( stx.Text( "SavingRecords" ) );
				await Db.SaveChangesAsync();
			}

			MesgR( stx.Text( "PurgingFiles" ) + ZSRoot );
			Shared.Storage.RemoveDir( ZSRoot );
			MesgR( stx.Text( "PurgingFiles" ) + SVolRoot );
			Shared.Storage.RemoveDir( SVolRoot );
		}


	}
}