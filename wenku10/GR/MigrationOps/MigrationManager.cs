using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

namespace GR.MigrationOps
{
	using CompositeElement;
	using GSystem;
	using Resources;

	class MigrationManager : ActiveData
	{
		Type[] Mops = new Type[] { typeof( M0000 ) };
		private string NextMop;

		public bool ShouldMigrate { get; set; }

		public string Mesg { get; set; }
		public string MesgR { get; set; }

		private bool _CanBackup = false;
		public bool CanBackup { get => _CanBackup; set { _CanBackup = value; NotifyChanged( "CanBackup" ); } }

		private bool _CanMigrate = false;
		public bool CanMigrate { get => _CanMigrate; set { _CanMigrate = value; NotifyChanged( "CanMigrate" ); } }

		private bool _CanRestore = false;
		public bool CanRestore { get => _CanRestore; set { _CanRestore = value; NotifyChanged( "CanRestore" ); } }

		private bool _IsLoading = false;
		public bool IsLoading { get => _IsLoading; set { _IsLoading = value; NotifyChanged( "IsLoading" ); } }

		BackupAndRestoreOp CurrBakOp;

		StringResources stx = new StringResBg( "InitQuestions", "Message", "Settings", "NavigationTitles" );
		DispatcherTimer DTimer;

		public MigrationManager()
		{
			NextMop = string.Format( "M{0:0000}", int.Parse( Mops.Last().Name.Substring( 1 ) ) + 1 );

			DTimer = new DispatcherTimer();
			DTimer.Interval = TimeSpan.FromSeconds( 2 );
			DTimer.Tick += DTimer_Tick;

			ShouldMigrate = false;

			foreach ( Type M in Mops )
			{
				IMigrationOp Mop = ( IMigrationOp ) Activator.CreateInstance( M );
				if ( Mop.ShouldMigrate )
				{
					ShouldMigrate = true;
					CanBackup = true;
					CanMigrate = true;
					CanRestore = true;

					CurrBakOp = new BackupAndRestoreOp( M.Name );
					break;
				}
			}

			if( CurrBakOp == null)
			{
				CurrBakOp = new BackupAndRestoreOp( NextMop );
				CanBackup = true;
				CanRestore = true;
			}
		}

		public async Task Backup()
		{
			if ( CurrBakOp.ZBackup != null )
			{
				goto SaveBackup;
			}

			CanBackup = CanMigrate = CanRestore = false;

			MWriteLine( stx.Text( "DataBackup" ) );
			Worker.UIInvoke( DTimer.Start );
			await CurrBakOp.Backup();
			Worker.UIInvoke( DTimer.Stop );

			CanBackup = true;

			SaveBackup:
			MWriteLine( stx.Text( "ExportBackup" ) );

			await Worker.RunUITaskAsync( async () =>
			{
				IStorageFile ISF = await AppStorage.SaveFileAsync( CurrBakOp.BackupType, new string[] { CurrBakOp.ExtType }, CurrBakOp.BackupName );
				if ( ISF != null )
				{
					await CurrBakOp.ZBackup.MoveAndReplaceAsync( ISF );
					MWriteLine( stx.Text( "BackupComplete" ) );
					CanBackup = false;
				}
			} );
		}

		public async Task Restore()
		{
			if ( !await CurrBakOp.PickRestoreFile() )
				return;

			CanBackup = CanMigrate = CanRestore = false;

			MWriteLine( stx.Text( "PurgingFiles" ) );
			Shared.Storage.PurgeContents( "./", false );

			Worker.UIInvoke( DTimer.Start );
			MWriteLine( stx.Text( "ExtractingFiles" ) );
			bool RestoreSuccess = await CurrBakOp.Restore();
			Worker.UIInvoke( DTimer.Stop );

			if( RestoreSuccess )
			{
				await Migrate();
			}
			else
			{
				MWriteLine( "Restore failed" );
			}
		}

		public async Task Migrate()
		{
			if ( CanBackup )
			{
				bool ContinueWithoutBackup = false;

				await Worker.RunUITaskAsync( () =>
				{
					return Popups.ShowDialog( UIAliases.CreateDialog(
						stx.Text( "NoBackupWarning" )
						, () => ContinueWithoutBackup = true
						, stx.Str( "Yes", "Message" ), stx.Str( "No", "Message" )
					) );
				} );

				if ( !ContinueWithoutBackup )
				{
					MWriteLine( stx.Text( "MigrationAborted" ) );
					return;
				}
			}

			CanBackup = CanMigrate = CanRestore = false;

			foreach( Type M in Mops )
			{
				IMigrationOp Mop = ( IMigrationOp ) Activator.CreateInstance( M );
				Mop.Mesg = MWriteLine;
				Mop.MesgR = MWrite;

				if( Mop.ShouldMigrate )
				{
					await Mop.Up();
				}
			}

			CanRestore = true;
		}

		private void MWriteLine( string Text )
		{
			Mesg += "\n" + Text;
			NotifyChanged( "Mesg" );

			MWrite( "" );
		}

		private void MWrite( string Text )
		{
			MesgR = Text;
			NotifyChanged( "MesgR" );
		}

		private void DTimer_Tick( object sender, object e )
		{
			MWrite( stx.Text( "MightTakeAWhile" ) + string.Format( "{0}/{1}: {2}", Utils.AutoByteUnit( CurrBakOp.BytesCopied ), CurrBakOp.BytesTotal, CurrBakOp.CFName ) );
		}
	}
}