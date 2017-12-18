using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

using wenku8.Effects;
using wenku8.Model.Interfaces;
using wenku8.Model.Book;

namespace wenku10.Pages
{
	sealed partial class ContentReaderHorz : ContentReaderBase, IAnimaPage, INavPage
	{
		public static readonly string ID = typeof( ContentReaderHorz ).Name;

		private ContentReaderHorz()
		{
			this.InitializeComponent();
		}

		public ContentReaderHorz( BookItem Book, Chapter C )
			: this()
		{
			SetElements();
			SetTemplate();

			CurrentBook = Book;
			OpenBook( C );
		}

		private void SetElements()
		{
			_CGTransform = CGTransform;

			_BookTitle = BookTitle;
			_VolTitle = VolTitle;
			_EpTitleStepper = EpTitleStepper;
			_HistoryThubms = HistoryThumbs;

			_Clock = RClock;
			_Month = MonthText;
			_DayofMonth = DayofMonthText;
			_DayofWeek = DayofWeekText;

			_ContentBg = ContentBg;
			_ContentFrame = ContentFrame;
			_ContentRestore = ContentRestore;
			_ContentSlideBack = ContentSlideBack;
			_ContentSlideDown = ContentSlideDown;
			_ContentSlideUp = ContentSlideUp;
			_FocusHelper = FocusHelper;
			_LayoutRoot = LayoutRoot;
			_LowerBack = LowerBack;
			_MainSplitView = MainSplitView;
			_Overlay = Overlay;
			_OverlayFrame = OverlayFrame;
			_PaneGrid = PaneGrid;
			_RenderMask = RenderMask;
			_UpperBack = UpperBack;
			_VESwipe = VESwipe;
		}

		protected override void ContentBeginAway( bool Next )
		{
			if ( CurrManiState == ManiState.NORMAL ) return;

			ContentAway = new Storyboard();

			if ( Next )
			{
				SimpleStory.DoubleAnimation(
					ContentAway, _CGTransform, "TranslateX"
					, _CGTransform.TranslateX
					, -_MainSplitView.ActualWidth );

				StepNextTitle();
			}
			else
			{
				SimpleStory.DoubleAnimation(
					ContentAway, _CGTransform, "TranslateX"
					, _CGTransform.TranslateX
					, _MainSplitView.ActualWidth );

				StepPrevTitle();
			}

			ContentAway.Completed += ( s, e ) =>
			{
				ContentAway.Stop();
				_CGTransform.TranslateX = -( double ) _CGTransform.GetValue( CompositeTransform.TranslateXProperty );
				_CGTransform.TranslateY = 0;
				ReaderSlideBack();
			};
			ContentAway.Begin();
		}

		protected override void ManiZoomBackDown( object sender, ManipulationDeltaRoutedEventArgs e )
		{
			_CGTransform.TranslateX += e.Delta.Translation.X;
			VEZoomBackDown( e.Delta.Translation.Y );
		}

		protected override void ManiZoomBackUp( object sender, ManipulationDeltaRoutedEventArgs e )
		{
			_CGTransform.TranslateX += e.Delta.Translation.X;
			VEZoomBackUp( e.Delta.Translation.Y );
		}

		protected override void ManiZoomEnd( object sender, ManipulationCompletedRoutedEventArgs e )
		{
			double dv = e.Cumulative.Translation.X.Clamp( MinVT, MaxVT );
			ContentAway?.Stop();
			if ( VT < dv )
			{
				ContentBeginAway( false );
			}
			else if ( dv < -VT )
			{
				ContentBeginAway( true );
			}
			else
			{
				_ContentRestore.Begin();
			}
		}

	}
}