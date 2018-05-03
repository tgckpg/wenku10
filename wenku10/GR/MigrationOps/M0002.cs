using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace GR.MigrationOps
{
	using Database.Contexts;
	using Model.Loaders;
	using Model.Interfaces;

	sealed class M0002 : IMigrationOp
	{
		public Action<string> Mesg { get; set; }
		public Action<string> MesgR { get; set; }

		public bool ShouldMigrate { get; set; }

		public M0002()
		{
			using ( DbContext Context = new BooksContext() )
			{
				ShouldMigrate = Context.Database.GetPendingMigrations().Contains( "20180429152855_CustomConv" );
			}
		}

		private IPropertySet Settings => ApplicationData.Current.LocalSettings.Values;

		StringResources stx = StringResources.Load( "InitQuestions", "AdvDM" );

		public async Task Up()
		{
			try
			{
				Mesg( stx.Text( "MigrateDatabase" ) );
				using ( DbContext Context = new BooksContext() )
				{
					Context.Database.Migrate();
				}

				TRTable Table = new TRTable();

				Mesg( stx.Text( "Active", "AdvDM" ) + " ntw_ws2t" );
				if ( !( await Table.Get( "ntw_ws2t" ) ).Any() )
				{
					Mesg( stx.Text( "Failure_NTW" ) );
				}

				Mesg( stx.Text( "Active", "AdvDM" ) + " ntw_ps2t" );
				if ( !( await Table.Get( "ntw_ps2t" ) ).Any() )
				{
					Mesg( stx.Text( "Failure_NTW" ) );
				}

				Mesg( stx.Text( "Active", "AdvDM" ) + " vertical" );
				if ( !( await Table.Get( "vertical" ) ).Any() )
				{
					Mesg( stx.Text( "Failure_Vertical" ) );
				}

				Mesg( stx.Text( "Active", "AdvDM" ) + " synpatch" );
				if ( !( await Table.Get( "synpatch" ) ).Any() )
				{
					Mesg( stx.Text( "Failure_Synpatch" ) );
				}

				Mesg( stx.Text( "MigrationComplete" ) + " - M0002" );
			}
			catch ( Exception ex )
			{
				Mesg( ex.Message );
			}
		}

	}
}