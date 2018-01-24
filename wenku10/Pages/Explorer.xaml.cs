﻿using System;
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

		private GRTable Table { get; set; }

        public Explorer()
        {
			_xSetTemplate();
            this.InitializeComponent();
			SetTemplate();
        }

		private List<PropFunc<BookDisplay>> BkProps;

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

			Table.SetCol( 5, -1, false );

			ReloadItems();
		}

		private void SetTemplate()
		{
			MenuFlyout TableFlyout = new MenuFlyout();
			foreach ( PropFunc<BookDisplay> BkProp in BkProps )
			{
				MenuFlyoutItem Item = new MenuFlyoutItem()
				{
					Icon = new SymbolIcon( Symbol.Accept ),
					Text = BookItem.PropertyName( BkProp.Property ),
					Tag = BkProp
				};

				Item.Click += ToggleCol_Click;
				TableFlyout.Items.Add( Item );
			}

			FlyoutBase.SetAttachedFlyout( TableSettings, TableFlyout );
		}

		private void ToggleCol_Click( object sender, RoutedEventArgs e )
		{
			MenuFlyoutItem Item = ( MenuFlyoutItem ) sender;
			PropFunc<BookDisplay> BkProp = ( PropFunc<BookDisplay> ) Item.Tag;

			if( Item.Icon.Opacity == 0 )
			{
				Item.Icon.Opacity = 1;
			}
			else
			{
				Item.Icon.Opacity = 0;

			}

		}

		private void ReloadItems()
		{
			Table.Items = Shared.BooksDb.Books
				.Where( x => x.Fav || x.Type == BookType.S || x.Type == BookType.L )
				.Remap( x => new GRRow( Table )
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
			public Func<T, object> Path = x => x;
			public string Value( T x ) => ( string ) Property.GetValue( Path( x ) );

			public PropFunc( PropertyInfo Property )
			{
				this.Property = Property;
			}
		}
	}

	public class _GRCells : ActiveData
	{
		public static readonly Type _GRCellType = typeof( _GRCells );
		public string C00 => _Cell( 0 );
		public string C01 => _Cell( 1 );
		public string C02 => _Cell( 2 );
		public string C03 => _Cell( 3 );
		public string C04 => _Cell( 4 );
		public string C05 => _Cell( 5 );
		public string C06 => _Cell( 6 );
		public string C07 => _Cell( 7 );
		public string C08 => _Cell( 8 );
		public string C09 => _Cell( 9 );

		public object Source { get; set; }
		public Func<int, object, string> Cell = ( i, x ) => "";

		private static IReadOnlyList<PropertyInfo> _CellProps;
		public IReadOnlyList<PropertyInfo> Cells
		{
			get
			{
				if ( _CellProps == null )
				{
					List<PropertyInfo> _Cells = new List<PropertyInfo>();

					for ( int i = 0; ; i++ )
					{
						PropertyInfo PropInfo = _GRCellType.GetProperty( string.Format( "C{0:00}", i ) );

						if ( PropInfo == null )
							break;

						_Cells.Add( PropInfo );
					}

					_CellProps = _Cells.AsReadOnly();
				}

				return _CellProps;
			}
		}

		protected string[] _CellNames;
		public IReadOnlyList<string> CellNames => _CellNames ?? ( _CellNames = Cells.Remap( x => x.Name ) );

		virtual protected string _Cell( int ColIndex )
		{
			return Cell( ColIndex, Source );
		}
	}

	public class GRRow : _GRCells
	{
		public GRTable Table { get; set; }

		public GRRow( GRTable Table )
		{
			this.Table = Table;
		}

		protected override string _Cell( int ColIndex )
		{
			return Table.ColEnabled( ColIndex ) ? base._Cell( ColIndex ) : "";
		}
	}

	public class GRTable : _GRCells
	{
		public static readonly Type GRTableType = typeof( GRTable );

		public GridLength H00 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H01 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H02 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H03 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H04 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H05 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H06 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H07 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H08 { get; set; } = new GridLength( 100, GridUnitType.Star );
		public GridLength H09 { get; set; } = new GridLength( 100, GridUnitType.Star );

		public GridLength HSP { get; set; } = new GridLength( 0, GridUnitType.Star );

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

		private static IReadOnlyList<PropertyInfo> _Headers;
		public IReadOnlyList<PropertyInfo> Headers
			=> _Headers ?? (
				_Headers = Cells.Remap( x => GRTableType.GetProperty( x.Name.Replace( 'C', 'H' ) ) ).ToList().AsReadOnly()
			);

		private string[] _HeaderNames;
		public IReadOnlyList<string> HeaderNames => _HeaderNames ?? ( _HeaderNames = Headers.Remap( x => x.Name ) );

		public bool ColEnabled( int ColIndex )
		{
			return 0 < ( ( GridLength ) Headers[ ColIndex ].GetValue( this ) ).Value;
		}

		public void Toggle( int ColIndex )
		{
			GridLength GL = ( GridLength ) Headers[ ColIndex ].GetValue( this );

			if ( GL.Value == 0 )
			{
				Headers[ ColIndex ].SetValue( this, new GridLength( 100, GL.GridUnitType ) );
			}
			else
			{
				Headers[ ColIndex ].SetValue( this, new GridLength( 0, GL.GridUnitType ) );
			}

			NotifyChanged( _HeaderNames );
			NotifyChanged( _CellNames );
		}

		public void SetCol( int FromCol, int ToCol, bool Enable )
		{
			IEnumerable<PropertyInfo> Cols = Headers;
			FromCol = FromCol - 1;

			if ( 0 < FromCol )
				Cols = Cols.Skip( FromCol );

			if ( FromCol < ToCol )
				Cols = Cols.Take( ToCol - FromCol );

			if ( Enable )
			{
				foreach ( PropertyInfo GLInfo in Cols )
				{
					GridLength GL = ( GridLength ) GLInfo.GetValue( this );
					GLInfo.SetValue( this, new GridLength( 100, GL.GridUnitType ) );
				}
			}
			else
			{
				foreach ( PropertyInfo GLInfo in Cols )
				{
					GridLength GL = ( GridLength ) GLInfo.GetValue( this );
					GLInfo.SetValue( this, new GridLength( 0, GL.GridUnitType ) );
				}
			}
		}
	}
}
