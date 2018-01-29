using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

using Net.Astropenguin.Linq;

using GR.Data;
using GR.DataSources;
using GR.Effects;

namespace wenku10.Pages.Explorer
{
	public sealed partial class GRTableView : UserControl
	{
		private List<MenuFlyoutItem> ColToggles;

		private volatile bool Locked = false;
		private volatile bool ColMisfire = false;

		private GRDataSource DataSource;
		private IGRTable Table => DataSource.Table;

		public GRTableView( GRDataSource DataSource )
		{
			this.DataSource = DataSource;
			DataSource.StructTable();
			Table.SetCol( 4, -1, false );

			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			MenuFlyout TableFlyout = new MenuFlyout();
			ColToggles = new List<MenuFlyoutItem>();

			for ( int i = 0, l = Table.CellProps.Count; i < l; i++ )
			{
				IGRCell CellProp = Table.CellProps[ i ];

				MenuFlyoutItem Item = new MenuFlyoutItem()
				{
					Icon = new SymbolIcon( Symbol.Accept ),
					Text = DataSource.ColumnName( CellProp ),
					Tag = CellProp
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
			Item.Icon.Opacity = Table.ToggleCol( ( IGRCell ) Item.Tag ) ? 1 : 0;
		}

		private void SortByColumn_Click( object sender, RoutedEventArgs e )
		{
			if ( ColMisfire ) return;

			Button ColBtn = ( Button ) sender;
			int ColIndex = int.Parse( ( string ) ColBtn.Tag );

			DataSource.Sort( ColIndex );
		}

		private void ItemList_ItemClick( object sender, ItemClickEventArgs e )
		{
			if ( Locked ) return;
			Locked = true;

			DataSource.ItemAction( ( GRRow<object> ) e.ClickedItem );

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

		private void Rezise_Enter( object sender, PointerRoutedEventArgs e ) => CursorResize();

		private void Rezise_Exit( object sender, PointerRoutedEventArgs e )
		{
			if ( ColResizeIndex == -1 )
			{
				CursorArrow();
			}
		}

		private void Rezise_DragStart( object sender, ManipulationStartedRoutedEventArgs e )
		{
			ColResizeIndex = int.Parse( ( string ) ( ( FrameworkElement ) sender ).Tag );
			CursorResize();
		}

		private void Resize_DragEnd( object sender, ManipulationCompletedRoutedEventArgs e )
		{
			ColResizeIndex = -1;
			CursorArrow();
		}

		private void Resize_Drag( object sender, ManipulationDeltaRoutedEventArgs e )
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