using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;

using wenku8.Model.Book;
using wenku8.Model.Section;
using wenku8.Model.Text;
using wenku8.Resources;

namespace wenku10.Pages.ContentReaderPane
{
    sealed partial class ReaderContent : Page
    {
        public static readonly string ID = typeof( ReaderContent ).Name;

        public ReaderView Reader { get; internal set; }
        public bool UserStartReading = false;

        private ContentReader Container;
        private BookItem CurrentBook { get { return Container.CurrentBook; } }
        private Chapter CurrentChapter { get { return Container.CurrentChapter; } }
        private Paragraph SelectedParagraph;

        public ReaderContent( ContentReader Container, int Anchor )
        {
            this.InitializeComponent();
            this.Container = Container;
            SetTemplate( Anchor );
        }

        internal void SetTemplate( int Anchor )
        {
            if ( Reader != null )
                Reader.PropertyChanged -= ScrollToParagraph;

            Reader = new ReaderView( CurrentBook, CurrentChapter );
            Reader.ApplyCustomAnchor( Anchor );

            MasterGrid.DataContext = Reader;
            Reader.PropertyChanged += ScrollToParagraph;
        }

        internal void Load( bool Reload = false )
        {
            Reader.Load( !Reload || CurrentBook.IsLocal );
        }

        internal void ContentGrid_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( Reader == null || UserStartReading ) return;
            UserStartReading = true;

            if ( 0 < e.AddedItems.Count )
            {
                ContentGrid.ScrollIntoView( e.AddedItems[ 0 ] );
            }

            Reader.AutoVolumeAnchor();
        }

        internal void Grid_RightTapped( object sender, RightTappedRoutedEventArgs e )
        {
            Grid ParaGrid = sender as Grid;
            if ( ParaGrid == null ) return;

            FlyoutBase.ShowAttachedFlyout( MainStage.Instance.IsPhone ? MasterGrid : ParaGrid );

            SelectedParagraph = ParaGrid.DataContext as Paragraph;
        }

        internal void ScrollMore( bool IsPage = false )
        {
            ScrollViewer SV = ContentGrid.ChildAt<ScrollViewer>( 1 );
            double d = 50;
            if ( Reader.Settings.IsHorizontal )
            {
                if ( IsPage ) d = global::wenku8.Resources.LayoutSettings.ScreenWidth;
                SV.ChangeView( SV.HorizontalOffset + d, null, null );
            }
            else
            {
                if ( IsPage ) d = global::wenku8.Resources.LayoutSettings.ScreenHeight;
                SV.ChangeView( null, SV.VerticalOffset + d, null );
            }
        }

        internal void ScrollLess( bool IsPage = false  )
        {
            ScrollViewer SV = ContentGrid.ChildAt<ScrollViewer>( 1 );
            double d = 50;
            if ( Reader.Settings.IsHorizontal )
            {
                if ( IsPage ) d = global::wenku8.Resources.LayoutSettings.ScreenWidth;
                SV.ChangeView( SV.HorizontalOffset - d, null, null );
            }
            else
            {
                if ( IsPage ) d = global::wenku8.Resources.LayoutSettings.ScreenHeight;
                SV.ChangeView( null, SV.VerticalOffset - d, null );
            }
        }

        internal void PrevPara()
        {
            if ( ContentGrid.SelectedIndex == 0 ) return;
            Reader.SelectIndex( Reader.SelectedIndex - 1 );
        }

        internal void NextPara()
        {
            if ( ContentGrid.Items.Count == ContentGrid.SelectedIndex + 1 ) return;
            Reader.SelectIndex( Reader.SelectedIndex + 1 );
        }

        internal void GoTop() { GotoIndex( 0 ); }
        internal void GoBottom() { GotoIndex( ContentGrid.Items.Count - 1 ); }

        internal void GotoIndex( int i )
        {
            if ( ContentGrid.ItemsSource == null ) return;
            int l = ContentGrid.Items.Count;
            if ( !( -1 < i && i < l ) ) return;

            ContentGrid.SelectedIndex = i;
            ContentGrid.ScrollIntoView( ContentGrid.SelectedItem, ScrollIntoViewAlignment.Leading );
            Reader.SelectIndex( i );
        }

        // This calls onLoaded
        internal void SetBookAnchor( object sender, RoutedEventArgs e )
        {
            ToggleInertia();

            ContentGrid.IsSynchronizedWithCurrentItem = false;

            if ( Reader.SelectedData != null )
                ContentGrid.ScrollIntoView( Reader.SelectedData, ScrollIntoViewAlignment.Leading );
        }

        internal void ToggleInertia()
        {
            ScrollViewer SV = ContentGrid.ChildAt<ScrollViewer>( 1 );
            if ( SV != null )
            {
                SV.HorizontalSnapPointsType = SnapPointsType.None;
                SV.VerticalSnapPointsType = SnapPointsType.None;
                SV.IsScrollInertiaEnabled = Container.UseInertia;
            }
        }

        internal async void ScrollToParagraph( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
        {
            switch ( e.PropertyName )
            {
                case "SelectedIndex":
                    if( !UserStartReading )
                        ContentGrid.SelectedItem = Reader.SelectedData;
                    break;
                case "Data":
                    Shared.LoadMessage( "PleaseWaitSecondsForUI", "2" );
                    await Task.Delay( 2000 );

                    Shared.LoadMessage( "WaitingForUI" );
                    var NOP = ContentGrid.Dispatcher.RunIdleAsync( new IdleDispatchedHandler( Container.RenderComplete ) );
                    break;
            }
        }

        internal void ViewHorizontal( object sender, RoutedEventArgs e )
        {
            if ( SelectedParagraph == null ) return;
            FlyoutBase.ShowAttachedFlyout( ContentGrid );

            TextBlock tb = new TextBlock();
            tb.TextWrapping = TextWrapping.Wrap;
            tb.Text = SelectedParagraph.Text;
            ContentFlyout.Content = tb;
        }

        internal void ContextCopyClicked( object sender, RoutedEventArgs e )
        {
            if ( SelectedParagraph == null ) return;
            DataPackage Data = new DataPackage();

            Data.SetText( SelectedParagraph.Text );
            Clipboard.SetContent( Data );
        }

        internal void MarkParagraph( object sender, RoutedEventArgs e )
        {
            if ( SelectedParagraph == null ) return;
            SetCustomAnchor( SelectedParagraph );
        }

        internal async void SearchWords( object sender, RoutedEventArgs e )
        {
            if ( SelectedParagraph == null ) return;
            Dialogs.EBDictSearch DictDialog = new Dialogs.EBDictSearch( SelectedParagraph );
            await Popups.ShowDialog( DictDialog );
        }

        public async void SetCustomAnchor( Paragraph P, string BookmarkName = null )
        {
            Dialogs.NewBookmarkInput BookmarkIn = new Dialogs.NewBookmarkInput( P );
            if ( BookmarkName != null ) BookmarkIn.SetName( BookmarkName );

            await Popups.ShowDialog( BookmarkIn );
            if ( BookmarkIn.Canceled ) return;

            Reader.SetCustomAnchor( BookmarkIn.AnchorName, P );
        }

        private void MasterGrid_Tapped( object sender, TappedRoutedEventArgs e )
        {
            Container.ClosePane();
            if ( Reader == null ) return;
            if( Reader.UsePageClick )
            {
                Point P = e.GetPosition( MasterGrid );
                if( Reader.Settings.IsHorizontal )
                {
                    double HW = 0.5 * global::wenku8.Resources.LayoutSettings.ScreenWidth;
                    if ( Reader.Settings.IsRightToLeft )
                        if( P.X < HW ) ScrollMore( true ); else ScrollLess( true );
                    else
                        if( HW < P.X ) ScrollMore( true ); else ScrollLess( true );
                }
                else
                {
                    double HS = 0.5 * global::wenku8.Resources.LayoutSettings.ScreenHeight;
                    if ( P.Y < HS ) ScrollLess( true ); else ScrollMore( true );
                }
            }
        }

        private void ContentGrid_ItemClick( object sender, ItemClickEventArgs e )
        {
            Paragraph P = e.ClickedItem as Paragraph;
            if ( P == SelectedParagraph ) return;
            Reader.SelectAndAnchor( SelectedParagraph = P );
        }
    }
}
