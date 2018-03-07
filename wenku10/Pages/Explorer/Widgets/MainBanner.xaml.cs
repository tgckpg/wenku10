using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

using Net.Astropenguin.IO;

using GR.Converters;
using GR.Data;
using GR.Database.Models;
using GR.Model.Book;
using GR.Model.Pages;
using GR.Model.Section;

using static GR.Settings.Layout.BookInfoView;

namespace wenku10.Pages.Explorer.Widgets
{
	public sealed partial class MainBanner : UserControl
	{
		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
			"ItemsSource", typeof( object ), typeof( MainBanner )
			, new PropertyMetadata( null, OnUpdateItemsSource ) );

		public object ItemsSource
		{
			get { return ( object ) GetValue( ItemsSourceProperty ); }
			set { SetValue( ItemsSourceProperty, value ); }
		}

		public static readonly DependencyProperty RefSVProperty = DependencyProperty.Register(
			"RefSV", typeof( ScrollViewer ), typeof( MainBanner )
			, new PropertyMetadata( null, OnUpdateRefSV ) );

		public ScrollViewer RefSV
		{
			get { return ( ScrollViewer ) GetValue( RefSVProperty ); }
			set { SetValue( RefSVProperty, value ); }
		}

		public MainBanner()
		{
			this.InitializeComponent();
		}

		private static void OnUpdateItemsSource( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( MainBanner ) d ).SourceUpdate();
		}

		private IGRRow BindRow;

		private async void SourceUpdate()
		{
			Book Bk = null;
			if ( ItemsSource is IEnumerable<object> EnumSource )
			{
				object Item = EnumSource.FirstOrDefault();
				if ( Item == null )
				{
					if ( ItemsSource is ISupportIncrementalLoading IncrSource && IncrSource.HasMoreItems )
					{
						await IncrSource.LoadMoreItemsAsync( 1 );
					}

					Item = EnumSource.FirstOrDefault();

					if ( Item == null )
					{
						return;
					}
				}

				if ( Item is IGRRow GRow && GRow.CellData is BookDisplay BkDisplay )
				{
					Bk = BkDisplay.Entry;
					BindRow = GRow;
				}
			}

			if ( Bk == null )
				return;

			XRegistry XReg = new XRegistry( "<nop />", null );
			BgContext ItemContext = new BgContext( XReg, "CONTENT_READER" )
			{
				Book = ItemProcessor.GetBookItem( Bk )
			};

			ItemContext.SetBackground( "Preset" );

			InfoBgGrid.DataContext = ItemContext;
			TitleText.Text = Bk.Title;
		}

		private static void OnUpdateRefSV( DependencyObject d, DependencyPropertyChangedEventArgs e ) => ( ( MainBanner ) d ).RefSVUpdate();
		public void RefSVUpdate()
		{
			Binding VScroll = new Binding()
			{
				Source = RefSV,
				Path = new PropertyPath( "VerticalOffset" ),
				Converter = new ParallaxConverter(),
				ConverterParameter = 0.25
			};

			BindingOperations.SetBinding( BgGridTransform, TranslateTransform.YProperty, VScroll );
		}

		private void LayoutRoot_Tapped( object sender, TappedRoutedEventArgs e )
		{
			if( DataContext is WidgetView WV && BindRow != null )
			{
				WV.ViewSource.ItemAction( BindRow );
			}
		}
	}
}