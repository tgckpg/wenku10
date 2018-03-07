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
	using Database.Contexts;
	using Database.Models;
	using GStrings;
	using Model.Book;
	using Resources;

	/// <summary>
	/// Full Text Search display data
	/// </summary>
	sealed class FTSDisplayData : GRDataSource
	{
		private readonly string ID = typeof( FTSDisplayData ).Name;

		public override string ConfigId => "FTS";

		private GRTable<FTSResult> MatchTable;
		private FTSResult TableHeaderSource;

		public override IGRTable Table => MatchTable;

		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Title", Width = 200 },
			new ColumnConfig() { Name = "VolTitle", Width = 100 },
			new ColumnConfig() { Name = "EpTitle", Width = 100 },
			new ColumnConfig() { Name = "Result", Width = 400 },
		};

		public override string ColumnName( IGRCell CellProp )
		{
			if( CellProp.Property.Name == "Result" )
			{
				return TableHeaderSource.Result;
			}

			return ColumnNameResolver.FTSColumns( CellProp.Property.Name );
		}

		public bool IsBuilt => Database.ContextManager.ContextExists( typeof( FTSDataContext ) );

		public override void Reload()
		{
			if ( string.IsNullOrEmpty( Search ) )
				throw new EmptySearchQueryException();

			if ( !IsBuilt )
				return;

			lock ( this )
			{
				if ( IsLoading ) return;
				IsLoading = true;
			}

			StringResources stx = new StringResBg( "LoadingMessage", "AppResources" );
			Message = stx.Str( "ProgressIndicator_Message" );

			using ( var FTSD = new FTSDataContext() )
			{
				MatchTable.Items = FTSD.Search( Search ).Select( x => new GRRow<FTSResult>( MatchTable ) { Source = new FTSResult( x.ChapterId, x.Text ) } ).ToArray();
			}

			TableHeaderSource.Result = string.Format( stx.Text( "Search_N_Result", "AppResources" ), MatchTable.Items.Count() );
			IsLoading = false;
		}

		public async Task Rebuild()
		{
			lock ( this )
			{
				if ( IsLoading ) return;
				IsLoading = true;
			}

			MatchTable.Items = null;

			StringResources stx = new StringResBg( "LoadingMessage" );
			Message = stx.Str( "BuildingIndexes" );

			Database.ContextManager.CreateFTSContext();

			await Task.Run( () =>
			{
				using ( var FTSD = new FTSDataContext() )
				{
					FTSD.FTSChapters.AddRange(
						Shared.BooksDb.ChapterContents
						.Select( x => new FTSChapter() { ChapterId = x.ChapterId, Text = x.Data.StringValue } )
					);

					FTSD.SaveChanges();
				}
			} );

			IsLoading = false;

			try
			{
				Reload();
			}
			catch ( EmptySearchQueryException ) { }
		}

		public override void StructTable()
		{
			if ( MatchTable != null )
				return;

			List<IGRCell> PsProps = new List<IGRCell>();

			Type StringType = typeof( string );

			PsProps.AddRange(
				typeof( FTSResult ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<FTSResult>( p ) )
			);

			MatchTable = new GRTable<FTSResult>( PsProps );
			TableHeaderSource = new FTSResult( -1, ColumnNameResolver.FTSColumns( "Result" ) );

			MatchTable.Source = TableHeaderSource;
			MatchTable.Cell = ( i, x ) => MatchTable.ColEnabled( i ) ? ColumnName( MatchTable.CellProps[ i ] ) : "";
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }
		protected override void ConfigureSort( string PropertyName, int Order ) { /* Not Supported */ }
	}
}