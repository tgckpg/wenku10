using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

namespace GR.MigrationOps
{
	using CompositeElement;
	using GSystem;
	using Resources;

	class BackupAndRestoreOp
	{
		public string BytesTotal = "";
		public string CFName;
		public ulong BytesCopied = 0;

		private string BakFileName;
		private string SN;

		private byte OfsIV;

		public IStorageFile ZBackup { get; private set; }

		public string BackupType => "GR Backup " + SN;
		public string ExtType => "." + SN;
		public string BackupName => "Backup" + ExtType;

		public BackupAndRestoreOp( string SN )
		{
			this.SN = SN;
			BakFileName = "backup." + SN;

			switch ( SN )
			{
				case "M0000": OfsIV = 0b10101101; break;
				case "M0001": OfsIV = 0b01001001; break;
				case "M0002": OfsIV = 0b11011101; break;
				case "M0003": OfsIV = 0b11000110; break;

				default:
					throw new InvalidOperationException();
			}
		}

		public async Task Backup()
		{
			string ZM000 = Path.GetFullPath( Path.Combine( ApplicationData.Current.TemporaryFolder.Path, BakFileName ) );
			string MLocalState = Path.GetFullPath( ApplicationData.Current.LocalFolder.Path );

			FileInfo[] AllFiles = new DirectoryInfo( MLocalState )
				.GetFiles( "*", SearchOption.AllDirectories )
				.Where( x => !x.DirectoryName.Contains( "EBWin" ) )
				.ToArray();

			BytesTotal = Utils.AutoByteUnit( ( ulong ) AllFiles.Sum( x => x.Length ) );
			BytesCopied = 0;

			using ( Stream FileStream = File.OpenWrite( ZM000 ) )
			using ( Stream Ofs = new NaiveObfustream( FileStream, OfsIV ) )
			using ( ZipArchive ZArch = new ZipArchive( Ofs, ZipArchiveMode.Create ) )
			{
				foreach ( FileInfo F in AllFiles )
				{
					CFName = F.Name;
					ZipArchiveEntry ZEntry = ZArch.CreateEntry( F.FullName.Substring( MLocalState.Length + 1 ) );

					ZEntry.LastWriteTime = F.LastWriteTime;

					using ( Stream RStream = File.OpenRead( F.FullName ) )
					using ( Stream ZStream = ZEntry.Open() )
					{
						RStream.CopyTo( ZStream );
						BytesCopied += ( ulong ) F.Length;
					}
				}
			}

			ZBackup = await ApplicationData.Current.TemporaryFolder.GetFileAsync( BakFileName );
		}


		public async Task<bool> PickRestoreFile()
		{
			bool UseThisFile = false;

			await Worker.RunUITaskAsync( async () =>
			{
				ZBackup = await AppStorage.OpenFileAsync( "." + SN );
				if ( ZBackup != null )
				{
					StringResources stx = new StringResources( "InitQuestions", "Message" );
					await Popups.ShowDialog( UIAliases.CreateDialog(
						stx.Text( "WarnMigrateWithBackup" )
						, () => UseThisFile = true
						, stx.Str( "Yes", "Message" ), stx.Str( "No", "Message" )
					) );
				}
			} );

			return UseThisFile;
		}

		public async Task Restore()
		{
			if ( ZBackup == null )
				return;

			using ( Stream ZStream = await ZBackup.OpenStreamForReadAsync() )
			using ( Stream Ofs = new NaiveObfustream( ZStream, OfsIV ) )
			using ( ZipArchive ZArch = new ZipArchive( Ofs, ZipArchiveMode.Read ) )
			{
				BytesTotal = Utils.AutoByteUnit( ( ulong ) ZArch.Entries.Sum( n => n.Length ) );
				BytesCopied = 0;

				ZArch.Entries.ExecEach( Entry =>
				{
					Shared.Storage.CreateDirs( Path.GetDirectoryName( Entry.FullName ) );
					Entry.ExtractToFile( Path.Combine( ApplicationData.Current.LocalFolder.Path, Entry.FullName ) );
					BytesCopied += ( ulong ) Entry.Length;
					CFName = Entry.Name;
				} );
			}
		}
	}
}