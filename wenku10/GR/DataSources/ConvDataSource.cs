using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using Model.ListItem;
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
		private List<GRRow<NameValue<string>>> SourceData;

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
						return new GRRow<NameValue<string>>( ConvTable ) { Source = new NameValue<string>( s[ 0 ], s[ 1 ] ) };
					} ).ToList();
				}

				if ( SourceData == null )
				{
					return;
				}
			}

			if ( !string.IsNullOrEmpty( Search ) )
			{
				if ( Search[ 0 ] == '^' )
				{
					string HSearch = Search.Substring( 1 );
					if ( !string.IsNullOrEmpty( HSearch ) )
					{
						ConvTable.Items = SourceData.Where( x => x.Source.Name.IndexOf( HSearch ) == 0 || x.Source.Value.IndexOf( HSearch ) == 0 );
					}
				}
				else if ( Search[ Search.Length - 1 ] == '$' )
				{
					string RSearch = Search.Substring( 0, Search.Length - 1 );
					if ( !string.IsNullOrEmpty( RSearch ) )
					{
						int RLen = RSearch.Length;
						ConvTable.Items = SourceData.Where( x =>
						{
							int RIndex = x.Source.Name.Length - RLen;
							if ( 0 < RIndex && x.Source.Name.IndexOf( RSearch ) == RIndex )
							{
								return true;
							}

							RIndex = x.Source.Value.Length - RLen;
							if ( 0 < RIndex && x.Source.Value.IndexOf( RSearch ) == RIndex )
							{
								return true;
							}
							return false;
						} );
					}
				}
				else
				{
					ConvTable.Items = SourceData.Where( x => x.Source.Name.Contains( Search ) || x.Source.Value.Contains( Search ) );
				}
			}
			else
			{
				ConvTable.Items = SourceData;
			}
		}

		public override string ColumnName( IGRCell CellProp )
		{
			switch( CellProp.Property.Name )
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