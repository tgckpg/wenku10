using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using GStrings;
	using Model.Interfaces;
	using Model.ListItem;
	using Resources;
	using Settings;

	sealed class BookSpiderDisplayData : GRDataSource
	{
		private readonly string ID = typeof( BookSpiderDisplayData ).Name;

		public override string ConfigId => "NPSpider";

		private GRTable<IBookProcess> PsTable;
		public override IGRTable Table => PsTable;

		private ObservableCollection<GRRow<IBookProcess>> _Items = new ObservableCollection<GRRow<IBookProcess>>();

		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Name", Width = 335 },
			new ColumnConfig() { Name = "Zone", Width = 100 },
			new ColumnConfig() { Name = "Desc", Width = 355 },
		};

		public override string ColumnName( IGRCell CellProp ) => ColumnNameResolver.IBookProcess( CellProp.Property.Name );

		public override async void Reload()
		{
			lock ( this )
			{
				if ( IsLoading ) return;
				IsLoading = true;
			}

			StringResources stx = StringResources.Load( "LoadingMessage" );
			string LoadText = stx.Str( "ProgressIndicator_Message" );

			PsTable.Items = _Items;

			// Store existing items
			GRRow<IBookProcess>[] Items = _Items.ToArray();
			Worker.UIInvoke( () => _Items.Clear() );

			IEnumerable<string> ZoneIds = Shared.Storage.ListDirs( FileLinks.ROOT_SPIDER_VOL );

			foreach ( string ZoneId in ZoneIds )
			{
				Message = LoadText + ": " + ZoneId;

				IEnumerable<string> ZItemIds = Shared.Storage.ListFiles( FileLinks.ROOT_SPIDER_VOL + ZoneId + "/" ).Remap( x => x.Replace( ".xml", "" ) );

				foreach ( string ZItemId in ZItemIds )
				{
					if ( ZItemId == "METADATA" )
						continue;

					if ( FindRow( Items, ZoneId, ZItemId, out GRRow<IBookProcess> Row ) )
					{
						await AddRowAsync( Row );
					}
					else
					{
						SpiderBook LB = await SpiderBook.CreateSAsync( ZoneId, ZItemId, null );

						if ( LB.ProcessSuccess || LB.CanProcess )
						{
							ZoneNameResolver.Instance.Resolve( LB.ZoneId, x => LB.Zone = x );
							await AddRowAsync( new GRRow<IBookProcess>( PsTable ) { Source = LB } );
						}
						else
						{
							try
							{
								Logger.Log( ID, "Removing invalid script: " + ZItemId, LogType.INFO );
								Shared.Storage.DeleteFile( LB.MetaLocation );
							}
							catch ( Exception ex )
							{
								Logger.Log( ID, "Cannot remove invalid script: " + ex.Message, LogType.WARNING );
							}
						}
					}
				}
			}

			IsLoading = false;
		}

		public void ImportItem( SpiderBook Item )
		{
			if ( FindRow( _Items, Item.ZoneId, Item.ZItemId, out GRRow<IBookProcess> Existing ) )
			{
				// TODO: Ask to replace
			}
			else
			{
				AddRowBg( new GRRow<IBookProcess>( PsTable ) { Source = Item } );
			}
		}

		public void Delete( GRRow<IBookProcess> Row )
		{
			SpiderBook SBk = ( SpiderBook ) Row.Source;
			Shared.Storage.DeleteFile( SBk.MetaLocation );
			Shared.BooksDb.Delete( BookType.S, SBk.ZoneId, SBk.ZItemId );
			Worker.UIInvoke( () => _Items.Remove( Row ) );
		}

		public IGRRow FindRow( string ZoneId, string ZItemId )
		{
			FindRow( _Items, ZoneId, ZItemId, out GRRow<IBookProcess> Item );
			return Item;
		}

		private bool FindRow( IEnumerable<GRRow<IBookProcess>> Source, string ZoneId, string ZItemId, out GRRow<IBookProcess> Item )
		{
			foreach ( GRRow<IBookProcess> P in Source )
			{
				SpiderBook x = ( SpiderBook ) P.Source;

				if ( x.ZoneId == ZoneId && x.ZItemId == ZItemId )
				{
					Item = P;
					return true;
				}
			}

			Item = null;
			return false;
		}

		private void AddRowBg( GRRow<IBookProcess> Row ) => Worker.UIInvoke( () => _Items.Add( Row ) );
		private Task AddRowAsync( GRRow<IBookProcess> Row ) => Worker.RunUIAsync( () => _Items.Add( Row ) );

		public override void StructTable()
		{
			if ( PsTable != null )
				return;

			List<IGRCell> PsProps = new List<IGRCell>();

			Type StringType = typeof( string );

			PsProps.AddRange(
				typeof( IBookProcess ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<IBookProcess>( p ) )
			);

			PsTable = new GRTable<IBookProcess>( PsProps );
			PsTable.Cell = ( i, x ) => PsTable.ColEnabled( i ) ? ColumnName( PsTable.CellProps[ i ] ) : "";
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }
		protected override void ConfigureSort( string PropertyName, int Order ) { /* Not Supported */ }
	}
}