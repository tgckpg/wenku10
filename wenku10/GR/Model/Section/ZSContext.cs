using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

namespace GR.Model.Section
{
	using Resources;
	using Settings;

	sealed class ZSContext : ActiveData
	{
		public static readonly string ID = typeof( ZSContext ).Name;

		public Action<ZoneSpider> ZoneEntry = x => { };

		public ZSContext() { }

		public void ScanZones()
		{
			var j = Task.Run( () =>
			{
				string[] StoredZones = Shared.Storage.ListFiles( FileLinks.ROOT_ZSPIDER );
				foreach ( string Zone in StoredZones )
				{
					try
					{
						ReadZone( Shared.Storage.GetString( FileLinks.ROOT_ZSPIDER + Zone ), true );
					}
					catch ( Exception ex )
					{
						Logger.Log( ID, "Removing faulty zone: " + Zone, LogType.WARNING );
						Logger.Log( ID, ex.Message, LogType.DEBUG );
					}
				}
			} );
		}

		public async void OpenFile()
		{
			IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
			if ( ISF == null ) return;
			var j = OpenFile( ISF );
		}

		public async Task<bool> OpenFile( IStorageFile ISF )
		{
			try
			{
				ReadZone( await ISF.ReadString() );
				return true;
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.WARNING );
			}

			return false;
		}

		private void ReadZone( string ZData, bool Init = false )
		{
			ZoneSpider ZS = new ZoneSpider();
			XRegistry ZDef = new XRegistry( ZData, null, false );

			if ( ZS.Open( ZDef ) )
			{
				ZoneEntry( ZS );
				if ( !Init )
				{
					Worker.ReisterBackgroundWork( () => { Shared.Storage.WriteString( ZS.MetaLocation, ZData ); } );
				}
			}
		}

		public void RemoveZone( ZoneSpider ZS )
		{
			if ( ZS == null ) return;

			try
			{
				Shared.Storage.DeleteFile( ZS.MetaLocation );
			}
			catch ( Exception ) { }
		}
	}
}