using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Logging;

using GR.Data;
using GR.Database.Models;
using GR.Effects;
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

		private GRTable<BookDisplay> Table { get; set; }

		private volatile bool ColMisfire = false;

		public Explorer()
		{
			_xSetTemplate();
			this.InitializeComponent();
			SetTemplate();
		}

		private List<MenuFlyoutItem> ColToggles;

		public void SoftOpen()
		{
			ReloadItems();
		}

		public void SoftClose() { }

		private void _xSetTemplate()
		{
			_xStructTable();

			Table.SetCol( 4, -1, false );
			ReloadItems();
		}

		private void SetTemplate()
		{
			MenuFlyout TableFlyout = new MenuFlyout();
			ColToggles = new List<MenuFlyoutItem>();

			for ( int i = 0, l = Table.CellProps.Count; i < l; i++ )
			{
				GRCell<BookDisplay> BkProp = Table.CellProps[ i ];

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
			GRCell<BookDisplay> BkProp = ( GRCell<BookDisplay> ) Item.Tag;
			Item.Icon.Opacity = Table.ToggleCol( BkProp ) ? 1 : 0;
		}

		private void SortByColumn_Click( object sender, RoutedEventArgs e )
		{
			if ( ColMisfire ) return;

			Button ColBtn = ( Button ) sender;
			int ColIndex = int.Parse( ( string ) ColBtn.Tag );

			ParameterExpression _x = Expression.Parameter( typeof( Book ), "x" );

			Expression OrderExp;

			PropertyInfo Prop = Table.CellProps[ ColIndex ].Property;
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

			if ( Filter != null )
			{
				Books = Filter( Books );
			}

			Table.Items = Books.Remap( x => new GRRow<BookDisplay>( Table )
			{
				Source = new BookDisplay( x ),
				Cell = ( _i, _x ) => Table.CellProps[ _i ].Value( ( BookDisplay ) _x ),
			} );
		}

		private void _xStructTable()
		{
			List<GRCell<BookDisplay>> BkProps = new List<GRCell<BookDisplay>>();

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

			Table = new GRTable<BookDisplay>( BkProps );
			Table.Cell = ( i, x ) => BookItem.PropertyName( Table.CellProps[ i ].Property );
		}

		private void ItemList_ItemClick( object sender, ItemClickEventArgs e )
		{
			if ( Locked ) return;
			Locked = true;

			BookDisplay Item = ( ( GRRow<BookDisplay> ) e.ClickedItem ).Source;

			Book Bk = Item.Entry;
			BookItem BkItem = ItemProcessor.GetBookItem( Bk );
			ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( BkItem ) );

			Locked = false;
		}

		private void TableSettings_Click( object sender, RoutedEventArgs e )
		{
			FlyoutBase.ShowAttachedFlyout( ( Button ) sender );
		}

		private void CursorResize()
		{
			Window.Current.CoreWindow.PointerCursor =
				new Windows.UI.Core.CoreCursor( Windows.UI.Core.CoreCursorType.SizeWestEast, 2 );
		}

		private void CursorArrow()
		{
			Window.Current.CoreWindow.PointerCursor =
				new Windows.UI.Core.CoreCursor( Windows.UI.Core.CoreCursorType.Arrow, 1 );
		}

		private int ColResizeIndex = -1;

		private void Handle_Enter( object sender, PointerRoutedEventArgs e ) => CursorResize();

		private void Handle_Exit( object sender, PointerRoutedEventArgs e )
		{
			if ( ColResizeIndex == -1 )
			{
				CursorArrow();
			}
		}

		private void Handle_DragStart( object sender, ManipulationStartedRoutedEventArgs e )
		{
			ColResizeIndex = int.Parse( ( string ) ( ( FrameworkElement ) sender ).Tag );
			CursorResize();
		}

		private void Handle_DragEnd( object sender, ManipulationCompletedRoutedEventArgs e )
		{
			ColResizeIndex = -1;
			CursorArrow();
		}

		private void Handle_Drag( object sender, ManipulationDeltaRoutedEventArgs e )
		{
			Table.ResizeCol( ColResizeIndex, e.Delta.Translation.X );
		}

		private Button ColReorder = null;
		private int ColZIndex = -1;
		private int NewColIndex = -1;
		private TranslateTransform DragColTrans = null;

		private Dictionary<int, Vector2> WayPoints;
		private Dictionary<int, Tuple<Storyboard, Storyboard>> ReorderStories;
		private Dictionary<int, TranslateTransform> ColTransforms;

		private Button[] AllCols;

		private double StartX = 0;

		private void Column_DragStart( object sender, ManipulationStartedRoutedEventArgs e )
		{
			ColReorder = ( Button ) sender;
			DragColTrans = new TranslateTransform();
			ColReorder.RenderTransform = DragColTrans;

			// Bring element to the front
			Grid ColContainer = ( Grid ) ColReorder.Parent;
			AllCols = ColContainer.Children.Where( x => x.GetType() == typeof( Button ) ).Cast<Button>().ToArray();

			int ColIndex = int.Parse( ( string ) ColReorder.Tag );
			GridLength GL = ( GridLength ) Table.Headers[ ColIndex ].GetValue( Table );

			ColTransforms = ColTransforms ?? new Dictionary<int, TranslateTransform>();
			ReorderStories = new Dictionary<int, Tuple<Storyboard, Storyboard>>();
			WayPoints = new Dictionary<int, Vector2>();

			StartX = 0;

			bool BeforeTarget = true;

			Vector2 LastPoint = Vector2.Zero;
			float SumWidth = 0;
			foreach ( Button Col in AllCols )
			{
				int i = int.Parse( ( string ) Col.Tag );
				if ( !Table.ColEnabled( i ) ) continue;

				double ColWidth = ( ( GridLength ) Table.Headers[ i ].GetValue( Table ) ).Value;
				SumWidth += ( float ) ColWidth;

				WayPoints[ i ] = new Vector2( LastPoint.Y, SumWidth );
				LastPoint = WayPoints[ i ];

				if ( Col == ColReorder )
				{
					StartX += e.Position.X;
					ReorderStories[ i ] = new Tuple<Storyboard, Storyboard>( null, null );
					BeforeTarget = false;
					continue;
				}

				Storyboard sb;

				if ( ReorderStories.ContainsKey( i ) )
				{
					sb = ReorderStories[ i ].Item1;
					ReorderStories[ i ].Item2.Children.Clear();
					sb.Children.Clear();
				}
				else
				{
					sb = new Storyboard { FillBehavior = FillBehavior.HoldEnd };
					ReorderStories[ i ] = new Tuple<Storyboard, Storyboard>( sb, new Storyboard { FillBehavior = FillBehavior.HoldEnd } );
				}

				TranslateTransform ColTransform;
				if ( ColTransforms.ContainsKey( i ) )
				{
					ColTransform = ColTransforms[ i ];
				}
				else
				{
					ColTransform = new TranslateTransform();
					ColTransforms[ i ] = ColTransform;
				}

				Col.RenderTransform = ColTransform;

				if ( BeforeTarget )
				{
					StartX += ColWidth;
					SimpleStory.DoubleAnimation( sb, ColTransform, "X", 0, GL.Value );
				}
				else
				{
					SimpleStory.DoubleAnimation( sb, ColTransform, "X", 0, -GL.Value );
				}
			}

			ColZIndex = ColContainer.Children.IndexOf( ColReorder );
			ColContainer.Children.Move( ( uint ) ColZIndex, ( uint ) ColContainer.Children.Count() - 1 );
		}

		private void Column_DragEnd( object sender, ManipulationCompletedRoutedEventArgs e )
		{
			Grid ColContainer = ( Grid ) ColReorder.Parent;
			ColContainer.Children.Move( ( uint ) ColContainer.Children.IndexOf( ColReorder ), ( uint ) ColZIndex );

			ColZIndex = -1;
			DragColTrans = null;

			ReorderStories.Values.ExecEach( x => { x.Item1?.Stop(); x.Item2?.Stop(); } );
			ColTransforms.ExecEach( x => x.Value.X = 0 );
			AllCols.ExecEach( x => x.RenderTransform = null );

			int OIndex = int.Parse( ColReorder.Tag.ToString() );
			if ( NewColIndex != -1 )
			{
				PreventColMisfire();
				Table.MoveColumn( OIndex, NewColIndex );
				NewColIndex = -1;
			}

			ColReorder = null;
		}

		private async void PreventColMisfire()
		{
			ColMisfire = true;
			await Task.Delay( 500 );
			ColMisfire = false;
		}

		private void Column_Drag( object sender, ManipulationDeltaRoutedEventArgs e )
		{
			DragColTrans.X += e.Delta.Translation.X;
			ColReorderAnima( StartX + e.Cumulative.Translation.X );
		}

		private void ColReorderAnima( double X )
		{
			bool BeforeTarget = true;
			foreach ( KeyValuePair<int, Vector2> WayPoint in WayPoints )
			{
				int i = WayPoint.Key;
				bool InRange = WayPoint.Value.X < X && X < WayPoint.Value.Y;

				if ( InRange ) NewColIndex = i;

				(Storyboard MoveSt, Storyboard RestoreSt) = ReorderStories[ i ];
				if ( MoveSt == null )
				{
					BeforeTarget = false;
					continue;
				}

				if ( InRange )
				{
					RestoreSt.Stop();

					if ( MoveSt.GetCurrentState() == ClockState.Stopped )
					{
						MoveSt.Begin();
					}
				}
				else if (
					!( BeforeTarget ? ( X < WayPoint.Value.Y ) : ( WayPoint.Value.X < X ) )
					&& RestoreSt.GetCurrentState() == ClockState.Stopped )
				{
					TranslateTransform ColTrans = ColTransforms[ i ];
					double tX = ( double ) ColTrans.GetValue( TranslateTransform.XProperty );

					if ( tX != 0 )
					{
						MoveSt.Stop();
						RestoreSt.Children.Clear();
						SimpleStory.DoubleAnimation( RestoreSt, ColTrans, "X", tX, 0 );
						RestoreSt.Begin();
					}
				}
			}
		}

	}
}