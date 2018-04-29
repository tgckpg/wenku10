using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using Model.ListItem;
	using Model.Loaders;
	using Resources;
	using Settings;

	sealed class ConvDisplayData : GRDataSource
	{
		public override string ConfigId => "Conv-" + TableName;
		public override IGRTable Table => ConvTable;

		public override bool Searchable => true;

		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Name", Width = 260 },
			new ColumnConfig() { Name = "Value", Width = 260 },
		};

		private GRTable<NameValue<string>> ConvTable;

		private string TableName;
		private List<NameValue<string>> SourceData;

		public ConvDisplayData( string TableName )
		{
			this.TableName = TableName;
		}

		public override void StructTable()
		{
			Type NVType = typeof( NameValue<string> );

			List<IGRCell> Headers = new List<IGRCell>();
			Headers.Add( new GRCell<NameValue<string>>( NVType.GetProperty( "Name" ) ) );
			Headers.Add( new GRCell<NameValue<string>>( NVType.GetProperty( "Value" ) ) );

			ConvTable = new GRTable<NameValue<string>>( Headers );
			ConvTable.Cell = ( i, x ) => ConvTable.ColEnabled( i ) ? ColumnName( ConvTable.CellProps[ i ] ) : "";
		}

		public override void Reload()
		{
			string Local = FileLinks.ROOT_WTEXT + "tr-" + TableName;
			if ( SourceData == null )
			{
				if ( Shared.Storage.FileExists( Local ) )
				{
					SourceData = Shared.Storage.GetString( Local ).Split( '\n' ).Select( x =>
					{
						string[] s = x.Split( ',' );
						return new NameValue<string>( s[ 0 ], s[ 1 ] );
					} ).ToList();
				}

				if ( SourceData == null )
				{
					return;
				}
			}

			LargeList<NameValue<string>> Results = null;
			if ( !string.IsNullOrEmpty( Search ) )
			{
				if ( Search[ 0 ] == '^' )
				{
					string HSearch = Search.Substring( 1 );
					if ( !string.IsNullOrEmpty( HSearch ) )
					{
						Results = new LargeList<NameValue<string>>( SourceData.Where( x => x.Name.IndexOf( HSearch ) == 0 || x.Value.IndexOf( HSearch ) == 0 ) );
					}
				}
				else if ( Search[ Search.Length - 1 ] == '$' )
				{
					string RSearch = Search.Substring( 0, Search.Length - 1 );
					if ( !string.IsNullOrEmpty( RSearch ) )
					{
						int RLen = RSearch.Length;
						Results = new LargeList<NameValue<string>>( SourceData.Where( x =>
						{
							int RIndex = x.Name.Length - RLen;
							if ( 0 < RIndex && x.Name.IndexOf( RSearch ) == RIndex )
							{
								return true;
							}

							RIndex = x.Value.Length - RLen;
							if ( 0 < RIndex && x.Value.IndexOf( RSearch ) == RIndex )
							{
								return true;
							}
							return false;
						} ) );
					}
				}
				else
				{
					Results = new LargeList<NameValue<string>>( SourceData.Where( x => x.Name.Contains( Search ) || x.Value.Contains( Search ) ) );
				}
			}
			else
			{
				Results = new LargeList<NameValue<string>>( SourceData );
			}

			Observables<NameValue<string>, GRRow<NameValue<string>>> ItemsObservable = new Observables<NameValue<string>, GRRow<NameValue<string>>>();

			if ( Results != null )
			{
				ItemsObservable.ConnectLoader( Results, x => x.Remap( ToGRRow ) );
			}

			ConvTable.Items = ItemsObservable;
		}

		private GRRow<NameValue<string>> ToGRRow( NameValue<string> x )
		{
			return new GRRow<NameValue<string>>( ConvTable ) { Source = x };
		}

		public override string ColumnName( IGRCell CellProp )
		{
			switch ( CellProp.Property.Name )
			{
				case "Name":
					return "Pattern";
				case "Value":
					return "Replace";
				default:
					return CellProp.Property.Name;
			}
		}

		protected override void ConfigureSort( string PropertyName, int Order ) { /* Not Supported */  }
		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }
	}
}