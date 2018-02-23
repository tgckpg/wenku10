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

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using GR.Model.Book;
	using Resources;

	/// <summary>
	/// Full Text Search display data
	/// </summary>
	sealed class FTSDisplayData : GRDataSource
	{
		private readonly string ID = typeof( FTSDisplayData ).Name;

		protected override string ConfigId => "FTS";

		private GRTable<BookDisplay> MatchTable;
		public override IGRTable Table => MatchTable;

		private ObservableCollection<GRRow<BookDisplay>> _Items = new ObservableCollection<GRRow<BookDisplay>>();

		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Name", Width = 200 },
			new ColumnConfig() { Name = "Volume", Width = 100 },
			new ColumnConfig() { Name = "Chapter", Width = 100 },
			new ColumnConfig() { Name = "Match", Width = 300 },
		};

		public override string ColumnName( IGRCell CellProp ) => "NULL";

		public override void Reload()
		{
			lock ( this )
			{
				if ( IsLoading ) return;
				IsLoading = true;
			}

			StringResources stx = new StringResBg( "LoadingMessage" );
			Message = stx.Str( "ProgressIndicator_Message" );

			MatchTable.Items = _Items;
			_Items.Clear();

			IsLoading = false;
		}
		
		public async void OpenDirectory()
		{
			IsLoading = true;

			await Shared.Storage.GetLocalText( async ( x, i, l ) =>
			{
				if ( i % 20 == 0 )
				{
					await Task.Delay( 15 );
				}

				Message = string.Format( "{0}/{1}", i, l );
			} );

			IsLoading = false;
		}

		public override void StructTable()
		{
			if ( MatchTable != null )
				return;

			List<IGRCell> PsProps = new List<IGRCell>();

			Type StringType = typeof( string );

			PsProps.AddRange(
				typeof( BookDisplay ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<BookDisplay>( p ) )
			);

			MatchTable = new GRTable<BookDisplay>( PsProps );
			MatchTable.Cell = ( i, x ) => MatchTable.ColEnabled( i ) ? ColumnName( MatchTable.CellProps[ i ] ) : "";
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }
		protected override void ConfigureSort( string PropertyName, int Order ) { /* Not Supported */ }
	}
}