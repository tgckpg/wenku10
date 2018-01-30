using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

using Net.Astropenguin.Linq;

using GR.Data;
using GR.Database.Contexts;
using GR.Database.Models;
using GR.Model.Book;
using GR.Model.Pages;
using GR.Resources;

using wenku10.Pages;

namespace GR.DataSources
{
	class BookDisplayData : GRDataSource
	{
		private GRTable<BookDisplay> BkTable;
		private Func<IQueryable<Book>, IQueryable<Book>> QueryExp;

		virtual public string Name => "Library";
		public override IGRTable Table => BkTable;

		public override void ItemAction( IGRRow Row )
		{
			BookItem BkItem = ItemProcessor.GetBookItem( ( ( GRRow<BookDisplay> ) Row ).Source.Entry );
			ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( BkItem ) );
		}

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

		public void Reload( Func<IQueryable<Book>, IQueryable<Book>> Filter )
		{
			IQueryable<Book> Books = Shared.BooksDb.Books
				.Where( x => x.Fav || x.Type == BookType.S || x.Type == BookType.L );

			if ( Filter != null )
			{
				Books = Filter( Books );
			}

			BkTable.Items = Books.Remap( x => new GRRow<BookDisplay>( BkTable )
			{
				Source = new BookDisplay( x ),
				Cell = ( _i, _x ) => BkTable.CellProps[ _i ].Value( ( BookDisplay ) _x ),
			} );
		}

		public override void StructTable()
		{
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
			BkTable.Cell = ( i, x ) => BookItem.PropertyName( BkTable.CellProps[ i ].Property );
		}

		public override async Task Configure()
		{
			using ( SettingsContext Settings = new SettingsContext() )
			{
				GRTableConfig Config = Settings.TableConfigs.Find( Name );

				// Set the default configs
				if ( Config == null )
				{
					Config = new GRTableConfig() { Id = Name };
					Config.Columns.AddRange(
						new string[] { "Title", "Author", "Zone", "LastUpdateDate", "Status" }
						.Remap( x => new ColumnConfig() { Name = x, Width = Table.H00.Value } )
					);

					Settings.TableConfigs.Add( Config );
					await Settings.SaveChangesAsync();
				}

				BkTable.Configure( Config );

				ColumnConfig SortingCol = Config.Columns.FirstOrDefault( x => x.Order != 0 && 0 < x.Width );
				if( SortingCol != null )
				{
					SortExp( BkTable.CellProps.FindIndex( x => x.Property.Name == SortingCol.Name ), SortingCol.Order );
				}
			}
		}

		public override async Task SaveConfig()
		{
			using ( SettingsContext Settings = new SettingsContext() )
			{
				GRTableConfig Config = Settings.TableConfigs.Find( Name );
				if ( Config == null )
				{
					Config = new GRTableConfig() { Id = Name };
					Settings.TableConfigs.Add( Config );
				}

				Config.Columns.Clear();
				Config.Columns.AddRange( BkTable.Headers.Remap( ( x, i ) => new ColumnConfig()
				{
					Name = BkTable.CellProps[ i ].Property.Name,
					Width = ( ( GridLength ) x.GetValue( BkTable ) ).Value,
					Order = ( int ) BkTable.Sortings[ i ].GetValue( BkTable )
				} ) );

				await Settings.SaveChangesAsync();
			}
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

	}
}