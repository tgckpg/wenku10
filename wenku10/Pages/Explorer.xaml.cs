using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Messaging;

using GR.Data;
using GR.Database.Models;
using GR.Model.Book;
using GR.Model.Pages;
using GR.Model.Interfaces;
using GR.Resources;

namespace wenku10.Pages
{
    public sealed partial class Explorer : Page, ICmdControls, INavPage
    {
		private volatile bool Locked = false;

		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private GRTable Table { get; set; }

        public Explorer()
        {
			_xSetTemplate();
            this.InitializeComponent();
			SetTemplate();
        }

		private List<PropFunc<BookDisplay>> BkProps;
		private Dictionary<PropFunc<BookDisplay>, int> PropIndexes;
		private List<MenuFlyoutItem> ColToggles;

		public void SoftOpen()
		{
			ReloadItems();
		}

		public void SoftClose() {}

		private void _xSetTemplate()
		{
			_xBuildColumns();

			Table = new GRTable()
			{
				Cell = ( i, x ) => BookItem.PropertyName( BkProps[ i ].Property ),
			};

			Table.SetCol( 4, -1, false );

			ReloadItems();
		}

		private void SetTemplate()
		{
			MenuFlyout TableFlyout = new MenuFlyout();
			ColToggles = new List<MenuFlyoutItem>();

			for ( int i = 0, l = BkProps.Count; i < l; i ++ )
			{
				PropFunc<BookDisplay> BkProp = BkProps[ i ];

				MenuFlyoutItem Item = new MenuFlyoutItem()
				{
					Icon = new SymbolIcon( Symbol.Accept ),
					Text = BookItem.PropertyName( BkProp.Property ),
					Tag = BkProp
				};

				Item.Icon.Opacity = Table.ColEnabled( i ) ? 1 : 0;
				Item.Click += ToggleCol_Click;

				ColToggles.Add( Item );
				TableFlyout.Items.Add( Item );
			}

			FlyoutBase.SetAttachedFlyout( TableSettings, TableFlyout );
		}

		private void ToggleCol_Click( object sender, RoutedEventArgs e )
		{
			MenuFlyoutItem Item = ( MenuFlyoutItem ) sender;
			PropFunc<BookDisplay> BkProp = ( PropFunc<BookDisplay> ) Item.Tag;

			int ActiveCols = ColToggles.Where( x => 0 < x.Icon.Opacity ).Count();

			if ( Item.Icon.Opacity == 0 )
			{
				Item.Icon.Opacity = 1;
				if ( ActiveCols < PropIndexes[ BkProp ] )
				{
					PropIndexes[ BkProp ] = ActiveCols;
				}
				MoveColumn( BkProp, PropIndexes[ BkProp ] );
				ActiveCols++;
			}
			else
			{
				Item.Icon.Opacity = 0;
				PropIndexes[ BkProp ] = BkProps.IndexOf( BkProp );
				MoveColumn( BkProp, BkProps.Count - 1 );
				ActiveCols--;
			}

			Table.SetCol( PropIndexes[ BkProp ], ActiveCols - 1, true );
			Table.SetCol( ActiveCols, -1, false );
		}

		private void MoveColumn( PropFunc<BookDisplay> BkProp, int Index )
		{
			int k = BkProps.IndexOf( BkProp );

			if ( k < Index )
			{
				for ( int i = k; i < Index; i++ )
				{
					BkProps[ i ] = BkProps[ i + 1 ];
				}
			}
			else
			{
				for ( int i = k; Index < i; i-- )
				{
					BkProps[ i ] = BkProps[ i - 1 ];
				}
			}

			BkProps[ Index ] = BkProp;
		}

		private void SortByColumn_Click( object sender, RoutedEventArgs e )
		{
			Button ColBtn = ( Button ) sender;
			int ColIndex = int.Parse( ( string ) ColBtn.Tag );

			ParameterExpression _x = Expression.Parameter( typeof( Book ), "x" );

			Expression OrderExp;

			PropertyInfo Prop = BkProps[ ColIndex ].Property;
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

			PropertyInfo SortCol = Table.Sortings[ ColIndex ];
			int _Sort = ( ( int ) SortCol.GetValue( Table ) == 1 ) ? -1 : 1;

			Table.SortCol( ColIndex, _Sort );

			string OrderMethod = _Sort == 1 ? "OrderBy" : "OrderByDescending";

			ReloadItems( x =>
			{
				Expression _Exp = Expression.Call(
							typeof( Queryable ), OrderMethod,
							new Type[] { x.ElementType, OrderExp.Type },
							x.Expression, Expression.Quote( Expression.Lambda( OrderExp, _x ) ) );
				return x.Provider.CreateQuery<Book>( _Exp );
			} );
		}

		private void ReloadItems( Func<IQueryable<Book>, IQueryable<Book>> Filter = null )
		{
			IQueryable<Book> Books = Shared.BooksDb.Books
				.Where( x => x.Fav || x.Type == BookType.S || x.Type == BookType.L );

			if( Filter != null )
			{
				Books = Filter( Books );
			}

			Table.Items = Books.Remap( x => new GRRow( Table )
			{
				Source = new BookDisplay( x ),
				Cell = ( _i, _x ) => BkProps[ _i ].Value( ( BookDisplay ) _x ),
			} );
		}

		private void _xBuildColumns()
		{
			BkProps = new List<PropFunc<BookDisplay>>();

			Type StringType = typeof( string );

			string[] BkExclude = new string[] { "ZoneId", "ZItemId", "Description" };
			string[] InfoExclude = new string[] { "LongDescription" };

			BkProps.AddRange(
				typeof( Book ).GetProperties()
					.Where(
						x => x.PropertyType == StringType
						&& !( x.Name.StartsWith( "Json_" ) || BkExclude.Contains( x.Name ) ) )
					.Remap( p => new PropFunc<BookDisplay>( p ) { Path = x => x.Entry } )
			);

			BkProps.AddRange(
				typeof( BookDisplay ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new PropFunc<BookDisplay>( p ) )
			);

			BkProps.AddRange(
				typeof( BookInfo ).GetProperties()
					.Where( x => x.PropertyType == StringType
						&& !( x.Name.StartsWith( "Json_" ) || InfoExclude.Contains( x.Name ) ) )
					.Remap( p => new PropFunc<BookDisplay>( p ) { Path = x => x.Entry.Info } )
			);

			PropIndexes = new Dictionary<PropFunc<BookDisplay>, int>();

			BkProps.ExecEach( ( x, i ) => { PropIndexes[ x ] = i; } );
		}

		private void ItemList_ItemClick( object sender, ItemClickEventArgs e )
		{
			if ( Locked ) return;
			Locked = true;

			BookDisplay Item = ( BookDisplay ) ( ( GRRow ) e.ClickedItem ).Source;

			Book Bk = Item.Entry;
			BookItem BkItem = ItemProcessor.GetBookItem( Bk );
			ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( BkItem ) );

			Locked = false;
		}

		private void TableSettings_Click( object sender, RoutedEventArgs e )
		{
			FlyoutBase.ShowAttachedFlyout( ( Button ) sender );
		}

		private class PropFunc<T>
		{
			public PropertyInfo Property;
			public bool LeadOrderAsc = true;
			public Func<T, object> Path = x => x;
			public string Value( T x ) => ( string ) Property.GetValue( Path( x ) );

			public PropFunc( PropertyInfo Property )
			{
				this.Property = Property;
			}
		}

		private void Grid_DoubleTapped( object sender, DoubleTappedRoutedEventArgs e )
		{
			System.Diagnostics.Debugger.Break();
		}
	}

}