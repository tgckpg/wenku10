using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Logging;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using Model.Interfaces;
	using Model.Section;
	using Resources;
	using Settings;

	sealed class ZSMDisplayData : GRDataSource
	{
		public readonly string ID = typeof( ZSMDisplayData ).Name;

		public GRTable<IMetaSpider> ZSTable { get; private set; }
		public override IGRTable Table => ZSTable;

		public override string ConfigId => "ZSManager";
		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Name", Width = 355 },
			new ColumnConfig() { Name = "ZoneId", Width = 200 },
		};

		public delegate void ZSEvent( object sender, IMetaSpider MetaSpider );

		public event ZSEvent ZoneOpened;
		public event ZSEvent ZoneRemoved;

		private ObservableCollection<GRRow<IMetaSpider>> MetaSpiders = new ObservableCollection<GRRow<IMetaSpider>>();

		public override string ColumnName( IGRCell CellProp ) => GStrings.ColumnNameResolver.IBookProcess( CellProp.Property.Name );

		public override void Reload()
		{
			if ( ZSTable.Items == null )
			{
				ZSTable.Items = MetaSpiders;
			}

			string[] StoredZones = Shared.Storage.ListFiles( FileLinks.ROOT_ZSPIDER );
			foreach ( string Zone in StoredZones )
			{
				try
				{
					var j = AddZone( Shared.Storage.GetString( FileLinks.ROOT_ZSPIDER + Zone ) );
				}
				catch ( Exception ex )
				{
					Logger.Log( ID, "Removing faulty zone: " + Zone, LogType.WARNING );
					Logger.Log( ID, ex.Message, LogType.DEBUG );
				}
			}
		}

		public override void StructTable()
		{
			if ( ZSTable != null )
				return;

			List<IGRCell> ZSProps = new List<IGRCell>();

			Type StringType = typeof( string );

			ZSProps.AddRange(
				typeof( IMetaSpider ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<IMetaSpider>( p ) )
			);

			ZSTable = new GRTable<IMetaSpider>( ZSProps );
			ZSTable.Cell = ( i, x ) => ZSTable.ColEnabled( i ) ? ColumnName( ZSTable.CellProps[ i ] ) : "";
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ } 
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }
		protected override void ConfigureSort( string PropertyName, int Order ) { /* Not Supported */ }

		public async Task<bool> OpenFile( IStorageFile ISF )
		{
			try
			{
				IMetaSpider ZS = await AddZone( await ISF.ReadString() );

				if ( ZS != null )
				{
					Worker.Register( () => { var j = Shared.Storage.WriteFileAsync( ZS.MetaLocation, ISF ); } );
					return true;
				}
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.WARNING );
			}

			return false;
		}

		public void RemoveZone( GRRow<IMetaSpider> ZS )
		{
			try
			{
				Shared.Storage.DeleteFile( ZS.Source.MetaLocation );
				ZoneRemoved?.Invoke( this, ZS.Source );
				Worker.UIInvoke( () => MetaSpiders.Remove( ZS ) );
			}
			catch ( Exception ) { }
		}

		private async Task<IMetaSpider> AddZone( string ZData )
		{
			ZoneSpider ZS = new ZoneSpider();
			XRegistry ZDef = new XRegistry( ZData, null, false );

			if ( await Task.Run( () => ZS.Open( ZDef ) ) )
			{
				GRRow<IMetaSpider> Existing = MetaSpiders.FirstOrDefault( x => x.Source.MetaLocation == ZS.MetaLocation );
				if ( Existing != null )
				{
					return Existing.Source;
				}

				ZoneOpened?.Invoke( this, ZS );
				await AddZone( ZS );
				return ZS;
			}

			return null;
		}

		private Task AddZone( ZoneSpider ZS )
		{
			return Worker.RunUIAsync( () => MetaSpiders.Add( new GRRow<IMetaSpider>( ZSTable ) { Source = ZS } ) );
		}

	}
}