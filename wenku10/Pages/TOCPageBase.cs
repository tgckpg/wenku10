using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.UI.Icons;

using wenku8.Config;
using wenku8.CompositeElement;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Model.Loaders;
using wenku8.Model.Section;
using wenku8.Storage;
using wenku8.Resources;

namespace wenku10.Pages
{
    abstract class TOCPageBase : Page, ICmdControls
    {
        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; protected set; }
        public IList<ICommandBarElement> Major2ndControls { get; protected set; }
        public IList<ICommandBarElement> MinorControls { get; protected set; }

        protected AppBarButton JumpMarkBtn;

        protected TOCSection TOCData;
        protected global::wenku8.Settings.Layout.BookInfoView LayoutSettings;

        protected BookItem ThisBook;
        protected Volume RightClickedVolume;

        protected void Init( BookItem Book )
        {
            ThisBook = Book;
            new BookLoader( ( b ) =>
            {
                new VolumeLoader( SetTOC ).Load( b );
            } ).Load( Book, true );
        }

        virtual protected void SetTemplate()
        {
            LayoutSettings = new global::wenku8.Settings.Layout.BookInfoView();
            InitAppBar();
        }

        protected void InitAppBar()
        {
            StringResources stx = new StringResources( "AppBar" );
            List<ICommandBarElement> Btns = new List<ICommandBarElement>();

            if ( Properties.ENABLE_ONEDRIVE )
            {
                AppBarButtonEx OneDriveBtn = UIAliases.CreateAppBarBtnEx( SegoeMDL2.Cloud, stx.Text( "SyncBookmarks" ) );
                ButtonOperation SyncOp = new ButtonOperation( OneDriveBtn );

                SyncOp.SetOp( OneDriveRsync );
                Btns.Add( OneDriveBtn );
            }

            JumpMarkBtn = UIAliases.CreateAppBarBtn( Symbol.Tag, stx.Text( "JumpToAnchor" ) );
            JumpMarkBtn.Click += JumpToBookmark;

            CRDirToggle ReaderDirBtn = new CRDirToggle();
            ReaderDirBtn.Label = stx.Str( "ContentDirection" );
            ReaderDirBtn.Foreground = UIAliases.ContextColor;

            Btns.Add( ReaderDirBtn );
            Btns.Add( JumpMarkBtn );

            MajorControls = Btns.ToArray();
        }

        protected void VolumeChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count() < 1 ) return;
            TOCData.SelectVolume( ( Volume ) e.AddedItems[ 0 ] );
        }

        protected void ChapterSelected( object sender, ItemClickEventArgs e )
        {
            ControlFrame.Instance.BackStack.Remove( PageId.CONTENT_READER );
            ControlFrame.Instance.NavigateTo( PageId.CONTENT_READER, () => new ContentReader( ( Chapter ) e.ClickedItem ) );
        }

        protected async Task OneDriveRsync()
        {
            if ( ThisBook == null ) return;

            await new AutoAnchor( ThisBook ).SyncSettings();
            TOCData?.SetAutoAnchor();
        }

        protected void JumpToBookmark( object sender, RoutedEventArgs e )
        {
            if ( TOCData == null ) return;
            ControlFrame.Instance.BackStack.Remove( PageId.CONTENT_READER );
            ControlFrame.Instance.NavigateTo( PageId.CONTENT_READER, () => new ContentReader( TOCData.AutoAnchor ) );
        }

        protected void TOCShowVolumeAction( object sender, RightTappedRoutedEventArgs e )
        {
            FrameworkElement Elem = sender as FrameworkElement;
            FlyoutBase.ShowAttachedFlyout( Elem );

            RightClickedVolume = Elem.DataContext as Volume;
            if ( RightClickedVolume == null )
            {
                RightClickedVolume = ( Elem.DataContext as TOCSection.ChapterGroup ).Vol;
            }
        }

        protected async void DownloadVolume( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Message", "ContextMenu" );

            bool Confirmed = false;

            await Popups.ShowDialog(
                UIAliases.CreateDialog(
                    RightClickedVolume.VolumeTitle, stx.Text( "AutoUpdate", "ContextMenu" )
                    , () => Confirmed = true
                    , stx.Str( "Yes" ), stx.Str( "No" )
            ) );

            if ( !Confirmed ) return;

            AutoCache.DownloadVolume( ThisBook, RightClickedVolume );
        }

        virtual protected void SetTOC( BookItem b )
        {
            TOCData = new TOCSection( b );
            JumpMarkBtn.SetBinding( IsEnabledProperty, new Binding() { Source = TOCData, Path = new PropertyPath( "AnchorAvailable" ) } );
        }

    }
}