using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using Model.Book;
	using Resources;

	class BookDisplayData : GRDataSource
	{
		protected override string ConfigId => "Library";

		public override IGRTable Table => BkTable;

		protected GRTable<BookDisplay> BkTable;
		protected Func<IQueryable<Book>, IQueryable<Book>> QueryExp;

		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Title", Width = 355 },
			new ColumnConfig() { Name = "Author", Width = 100 },
			new ColumnConfig() { Name = "Zone", Width = 110 },
			new ColumnConfig() { Name = "Status", Width = 100 },
			new ColumnConfig() { Name = "LastUpdateDate", Width = 160, Order = -1 },
		};

		public override string ColumnName( IGRCell BkProp ) => BookItem.PropertyName( BkProp.Property );
		public override void Reload() => Reload( QueryExp );

		public override void ToggleSort( int ColIndex )
		{
			PropertyInfo SortCol = BkTable.Sortings[ ColIndex ];
			Sort( ColIndex, ( ( int ) SortCol.GetValue( BkTable ) == 1 ) ? -1 : 1 );
		}

		public override void Sort( int ColIndex, int Order )
		{
			SortExp( ColIndex, Order );
			Reload( QueryExp );
		}

		public override void StructTable()
		{
			if ( BkTable != null )
				return;

			BkTable = new GRTable<BookDisplay>( BookDisplay.GetHeaders() );
			BkTable.Cell = ( i, x ) => BkTable.ColEnabled( i ) ? ColumnName( BkTable.CellProps[ i ] ) : "";
		}

		virtual protected IQueryable<Book> QuerySet( IQueryable<Book> Context ) => Context.AsQueryable();

		public void Reload( Func<IQueryable<Book>, IQueryable<Book>> Filter )
		{
			IsLoading = true;

			StringResBg stx = new StringResBg( "AppResources" );
			Message = stx.Text( "Loading" );

			IQueryable<Book> Books = QuerySet( Shared.BooksDb.Books.AsQueryable() );

			if ( Filter != null )
			{
				Books = Filter( Books );
			}

			if ( !string.IsNullOrEmpty( Search ) )
			{
				Books = Books.Where( x => x.Title.Contains( Search ) );
			}

			Books = Books.Include( x => x.Info );

			BkTable.Items = Books.Remap( x => new GRRow<BookDisplay>( BkTable )
			{
				Source = new BookDisplay( x ),
			} );

			IsLoading = false;
		}

		virtual protected void SortExp( int ColIndex, int Order )
		{
			PropertyInfo Prop = BkTable.CellProps[ ColIndex ].Property;
			QueryExp = BookDisplay.QuerySort( Prop, Order );

			if ( QueryExp != null )
			{
				BkTable.SortCol( ColIndex, Order );
			}
		}

		protected override void ConfigureSort( string PropertyName, int Order )
		{
			SortExp( BkTable.CellProps.FindIndex( x => x.Property.Name == PropertyName ), Order );
		}

	}
}