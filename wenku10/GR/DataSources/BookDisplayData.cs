using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Linq;

using GR.Data;
using GR.Database.Models;
using GR.Model.Book;
using GR.Resources;

namespace GR.DataSources
{
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

			List<IGRCell> BkProps = new List<IGRCell>();

			Type StringType = typeof( string );

			string[] BkExclude = new string[] { "ZoneId", "ZItemId", "Description" };
			string[] InfoExclude = new string[] { "LongDescription" };

			BkProps.AddRange(
				typeof( Book ).GetProperties()
					.Where(
						x => x.PropertyType == StringType
						&& !( x.Name.StartsWith( "Json_" ) || BkExclude.Contains( x.Name ) ) )
					.Remap( p => new GRCell<BookDisplay>( p ) { Path = x => x.Entry } )
			);

			BkProps.AddRange(
				typeof( BookDisplay ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<BookDisplay>( p ) )
			);

			BkProps.AddRange(
				typeof( BookInfo ).GetProperties()
					.Where( x => x.PropertyType == StringType
						&& !( x.Name.StartsWith( "Json_" ) || InfoExclude.Contains( x.Name ) ) )
					.Remap( p => new GRCell<BookDisplay>( p ) { Path = x => x.Entry.Info } )
			);

			BkTable = new GRTable<BookDisplay>( BkProps );
			BkTable.Cell = ( i, x ) => ColumnName( BkTable.CellProps[ i ] );
		}

		virtual protected IQueryable<Book> QuerySet( IQueryable<Book> Context )
			=> Context.Where( x => x.Fav || x.Type == BookType.S || x.Type == BookType.L );

		public void Reload( Func<IQueryable<Book>, IQueryable<Book>> Filter )
		{
			IQueryable<Book> Books = QuerySet( Shared.BooksDb.Books.AsQueryable() );

			if ( Filter != null )
			{
				Books = Filter( Books );
			}

			Books = Books.Include( x => x.Info );

			BkTable.Items = Books.Remap( x => new GRRow<BookDisplay>( BkTable )
			{
				Source = new BookDisplay( x ),
				Cell = ( _i, _x ) => BkTable.CellProps[ _i ].Value( ( BookDisplay ) _x ),
			} );
		}

		virtual protected void SortExp( int ColIndex, int Order )
		{
			ParameterExpression _x = Expression.Parameter( typeof( Book ), "x" );

			Expression OrderExp;

			PropertyInfo Prop = BkTable.CellProps[ ColIndex ].Property;
			if ( Prop.DeclaringType == typeof( Book ) )
			{
				OrderExp = Expression.PropertyOrField( _x, Prop.Name );
			}
			else if ( Prop.DeclaringType == typeof( BookInfo ) )
			{
				OrderExp = Expression.PropertyOrField( _x, "Info" );
				OrderExp = Expression.PropertyOrField( OrderExp, Prop.Name );
			}
			else if ( Prop.DeclaringType == typeof( BookDisplay ) )
			{
				// Special fields
				switch ( Prop.Name )
				{
					case "LastAccess":
						OrderExp = Expression.PropertyOrField( _x, Prop.Name );
						break;
					default:
						return;
				}
			}
			else
			{
				return;
			}

			BkTable.SortCol( ColIndex, Order );
			string OrderMethod = Order == 1 ? "OrderBy" : "OrderByDescending";

			QueryExp = ( x ) =>
			{
				Expression _Exp = Expression.Call(
							typeof( Queryable ), OrderMethod,
							new Type[] { x.ElementType, OrderExp.Type },
							x.Expression, Expression.Quote( Expression.Lambda( OrderExp, _x ) ) );
				return x.Provider.CreateQuery<Book>( _Exp );
			};
		}

		protected override void ConfigureSort( string PropertyName, int Order )
		{
			SortExp( BkTable.CellProps.FindIndex( x => x.Property.Name == PropertyName ), Order );
		}

	}
}