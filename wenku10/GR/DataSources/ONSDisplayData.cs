using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Linq;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using Model.ListItem.Sharers;

	sealed class ONSDisplayData : GRDataSource
	{
		public override IGRTable Table => HSTable;

		protected override string ConfigId => "ONS";
		protected override ColumnConfig[] DefaultColumns => throw new NotImplementedException();

		private GRTable<HSDisplay> HSTable;

		public override string ColumnName( IGRCell CellProp )
		{
			throw new NotImplementedException();
		}

		public override void Reload()
		{
			throw new NotImplementedException();
		}

		public override void StructTable()
		{
			if ( HSTable != null )
				return;

			List<IGRCell> HSProps = new List<IGRCell>();

			Type StringType = typeof( string );

			HSProps.AddRange(
				typeof( HSDisplay ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<HSDisplay>( p ) )
			);

			HSTable = new GRTable<HSDisplay>( HSProps );
			HSTable.Cell = ( i, x ) => HSTable.ColEnabled( i ) ? HSDisplay.PropertyName( HSTable.CellProps[ i ].Property ) : "";
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }

		protected override void ConfigureSort( string Name, int Order ) { /* Not Supported */ }
	}
}