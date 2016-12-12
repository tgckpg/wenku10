using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using wenku8.Config;
using wenku8.Effects;
using wenku8.Model.Book;
using wenku8.Model.Section;
using wenku8.Model.Text;
using wenku8.Resources;

namespace wenku10.Pages.ContentReaderPane
{
    sealed partial class ReaderContent : Page, IDisposable
    {
        public static readonly string ID = typeof( ReaderContent ).Name;

        public ReaderView Reader { get; internal set; }
        public bool UserStartReading = false;

        private ContentReader Container;
        private BookItem CurrentBook { get { return Container.CurrentBook; } }
        private Chapter CurrentChapter { get { return Container.CurrentChapter; } }
        private Paragraph SelectedParagraph;

        private volatile bool HoldOneMore = false;
        private volatile int UndoingJump = 0;

        private AHQueue AnchorHistory;

        ScrollBar VScrollBar;
        ScrollBar HScrollBar;

        public ReaderContent( ContentReader Container, int Anchor )
        {
            this.InitializeComponent();
            this.Container = Container;
            SetTemplate( Anchor );
        }

        public void Dispose()
        {
            try
            {
                AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
                Reader.PropertyChanged -= ScrollToParagraph;
                Reader.Dispose();
                Reader = null;

                Worker.UIInvoke( () =>
                {
                    MasterGrid.DataContext = null;
                } );

            }
            catch ( Exception ) { }
        }

        internal void SetTemplate( int Anchor )
        {
            if ( Reader != null )
                Reader.PropertyChanged -= ScrollToParagraph;

            Reader = new ReaderView( CurrentBook, CurrentChapter );
            Reader.ApplyCustomAnchor( Anchor );

            AnchorHistory = new AHQueue( 20 );
            HCount.DataContext = AnchorHistory;

            ContentGrid.ItemsPanel = ( ItemsPanelTemplate ) Resources[ Reader.Settings.IsHorizontal ? "HPanel" : "VPanel" ];

            MasterGrid.DataContext = Reader;
            Reader.PropertyChanged += ScrollToParagraph;
            AppSettings.PropertyChanged += AppSettings_PropertyChanged;
        }

        private void AppSettings_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            if( e.PropertyName == Parameters.APPEARANCE_CONTENTREADER_SCROLLBAR ) UpdateScrollBar();
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

        internal void ScrollLess( bool IsPage = false )
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
            Reader.SelectIndex( Reader.SelectedIndex - 1 );
        }

        internal void NextPara()
        {
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
            ShowUndoButton();
        }

        // This calls onLoaded
        private void SetBookAnchor( object sender, RoutedEventArgs e )
        {
            SetScrollBar();
            ToggleInertia();

            ContentGrid.IsSynchronizedWithCurrentItem = false;

            // Reader may not be available as ContentGrid.OnLoad is faster then SetTemplate
            if ( !( Reader == null || Reader.SelectedData == null ) )
                ContentGrid.ScrollIntoView( Reader.SelectedData, ScrollIntoViewAlignment.Leading );
        }

        private void SetScrollBar()
        {
            VScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 1 );
            HScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 2 );

            UpdateScrollBar();
        }

        private void UpdateScrollBar()
        {
            VScrollBar.Foreground
               = HScrollBar.Foreground
               = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_SCROLLBAR );
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

        internal async void ScrollToParagraph( object sender, PropertyChangedEventArgs e )
        {
            switch ( e.PropertyName )
            {
                case "SelectedIndex":
                    if ( !UserStartReading )
                        ContentGrid.SelectedItem = Reader.SelectedData;
                    RecordUndo( Reader.SelectedIndex );
                    break;
                case "Data":
                    Shared.LoadMessage( "PleaseWaitSecondsForUI", "2" );
                    await Task.Delay( 2000 );

                    Shared.LoadMessage( "WaitingForUI" );

                    ShowUndoButton();
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
            if ( Reader.UsePageClick )
            {
                Point P = e.GetPosition( MasterGrid );
                if ( Reader.Settings.IsHorizontal )
                {
                    double HW = 0.5 * global::wenku8.Resources.LayoutSettings.ScreenWidth;
                    if ( Reader.Settings.IsRightToLeft )
                        if ( P.X < HW ) ScrollMore( true ); else ScrollLess( true );
                    else
                        if ( HW < P.X ) ScrollMore( true ); else ScrollLess( true );
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

            RecordUndo( ContentGrid.SelectedIndex );
            Reader.SelectAndAnchor( SelectedParagraph = P );

            if( P is IllusPara )
            {
                IllusPara S = ( IllusPara ) P;

                if ( !S.EmbedIllus )
                {
                    Container.OverNavigate( typeof( ImageView ), S );
                }
            }
        }

        private void UndoAnchorJump( object sender, RoutedEventArgs e )
        {
            if ( TransitionDisplay.GetState( UndoButton ) == TransitionState.Active )
            {
                UndoJump();
            }
            else
            {
                ShowUndoButton();
            }
        }

        internal void UndoJump()
        {
            while ( 0 < AnchorHistory.Count && AnchorHistory.Peek() == Reader.SelectedIndex )
                AnchorHistory.Pop();

            if ( AnchorHistory.Count == 0 ) return;

            UndoingJump++;
            GotoIndex( AnchorHistory.Pop() );
        }

        private void ShowUndoButton( object sender, PointerRoutedEventArgs e )
        {
            if ( !MainStage.Instance.IsPhone )
            {
                ShowUndoButton();
            }
        }

        private async void ShowUndoButton()
        {
            HoldOneMore = true;

            if ( TransitionDisplay.GetState( UndoButton ) == TransitionState.Active )
                return;

            TransitionDisplay.SetState( UndoButton, TransitionState.Active );
            while ( HoldOneMore )
            {
                HoldOneMore = false;
                await Task.Delay( 3000 );
            }

            TransitionDisplay.SetState( UndoButton, TransitionState.Inactive );
        }

        private void RecordUndo( int Index )
        {
            if ( 0 < UndoingJump )
            {
                UndoingJump--;
                return;
            }

            AnchorHistory.Push( Index );
            AnchorHistory.TrimExcess();
        }

        private class AHQueue : Stack<int>, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public AHQueue( int Capacity ) : base( Capacity ) { }

            new public void Push( int i )
            {
                if ( 0 < Count && Peek() == i ) return;

                base.Push( i );
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( "Count" ) );
            }

            new public int Pop()
            {
                int i = base.Pop();
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( "Count" ) );
                return i;
            }
        }

        private double ZoomTrigger = 0;

        private void ManipulationDeltaX( object sender, ManipulationDeltaRoutedEventArgs e ) { TriggerZoom( e.Delta.Translation.X ); }
        private void ManipulationDeltaY( object sender, ManipulationDeltaRoutedEventArgs e ) { TriggerZoom( e.Delta.Translation.Y ); }

        private void TriggerZoom( double dv )
        {
            ZoomTrigger += dv;

            if ( 100 < ZoomTrigger )
            {
                ZoomTrigger = 0;
                CRSlide( ContentReader.ManiState.DOWN );
            }
            else if ( ZoomTrigger < -100 )
            {
                ZoomTrigger = 0;
                CRSlide( ContentReader.ManiState.UP );
            }
            else if ( ZoomTrigger == 0 )
            {
                CRSlide( ContentReader.ManiState.NORMAL );
            }
        }

        private void CRSlide( ContentReader.ManiState State )
        {
            if ( State == Container.CurrManiState ) return;

            switch ( State )
            {
                case ContentReader.ManiState.NORMAL:
                    Container.ReaderSlideBack();
                    break;
                case ContentReader.ManiState.UP:
                    if ( Container.CurrManiState == ContentReader.ManiState.DOWN )
                        goto case ContentReader.ManiState.NORMAL;

                    Container.ReaderSlideUp();
                    break;
                case ContentReader.ManiState.DOWN:
                    if ( Container.CurrManiState == ContentReader.ManiState.UP )
                        goto case ContentReader.ManiState.NORMAL;

                    Container.ReaderSlideDown();
                    break;
            }

            Container.CurrManiState = State;
        }

    }
}