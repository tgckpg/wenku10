using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public GRTable Table { get; set; }

        public Explorer()
        {
			xSetTemplate();
            this.InitializeComponent();
        }

		private List<PropFunc<BookDisplay>> BkProps;

		public void SoftOpen()
		{
			ReloadItems();
		}

		public void SoftClose() {}

		private void xSetTemplate()
		{
			xBuildColumns();

			Table = new GRTable()
			{
				Cell = ( i, x ) => BookItem.PropertyName( BkProps[ i ].Property )
			};

			ReloadItems();
		}

		private void ReloadItems()
		{
			Table.Items = Shared.BooksDb.Books
				.Where( x => x.Fav || x.Type == BookType.S || x.Type == BookType.L )
				.Remap( x => new GRRow()
				{
					Source = new BookDisplay( x ),
					Cell = ( _i, _x ) => BkProps[ _i ].Value( ( BookDisplay ) _x )
				} );
		}

		private void xBuildColumns()
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

		private class PropFunc<T>
		{
			public PropertyInfo Property;
			public Func<T, object> Path = x => x;
			public string Value( T x ) => ( string ) Property.GetValue( Path( x ) );

			public PropFunc( PropertyInfo Property )
			{
				this.Property = Property;
			}
		}
	}

	public class GRRow : ActiveData
	{
		public GridLength W00 = new GridLength( 100, GridUnitType.Star );
		public GridLength W01 = new GridLength( 100, GridUnitType.Star );
		public GridLength W02 = new GridLength( 100, GridUnitType.Star );
		public GridLength W03 = new GridLength( 100, GridUnitType.Star );
		public GridLength W04 = new GridLength( 100, GridUnitType.Star );
		public GridLength W05 = new GridLength( 100, GridUnitType.Star );
		public GridLength W06 = new GridLength( 100, GridUnitType.Star );
		public GridLength W07 = new GridLength( 100, GridUnitType.Star );
		public GridLength W08 = new GridLength( 100, GridUnitType.Star );
		public GridLength W09 = new GridLength( 100, GridUnitType.Star );

		public string C00 => Cell( 0, Source );
		public string C01 => Cell( 1, Source );
		public string C02 => Cell( 2, Source );
		public string C03 => Cell( 3, Source );
		public string C04 => Cell( 4, Source );
		public string C05 => Cell( 5, Source );
		public string C06 => Cell( 6, Source );
		public string C07 => Cell( 7, Source );
		public string C08 => Cell( 8, Source );
		public string C09 => Cell( 9, Source );

		public object Source { get; set; }
		public Func<int, object, string> Cell = ( i, x ) => "";
	}

	public class GRTable : GRRow
	{
		public IEnumerable<GRRow> _Items;
		public IEnumerable<GRRow> Items
		{
			get => _Items;
			set
			{
				_Items = value;
				NotifyChanged( "Items" );
			}
		}
	}
}
