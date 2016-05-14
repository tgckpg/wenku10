using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.UI.Icons;

using libtaotu.Pages;

using wenku8.Config;
using wenku8.Model.Book;
using wenku8.Model.ListItem;
using wenku8.Model.Section;
using wenku8.Storage;
using wenku8.Model.Book.Spider;

namespace wenku10.Pages
{
    public sealed partial class LocalModeTxtList : Page
    {
        private static readonly string ID = typeof( LocalModeTxtList ).Name;

        private LocalFileList FileListContext;
        private LocalBook SelectedBook;

        public LocalModeTxtList()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private async void SetTemplate()
        {
            FileListContext = new LocalFileList();
            FileListView.DataContext = FileListContext;
            ProcessButton.DataContext = FileListContext;

            if( Properties.ENABLE_ONEDRIVE && OneDriveSync.Instance == null )
            {
                OneDriveSync.Instance = new OneDriveSync();
                await OneDriveSync.Instance.Authenticate();
            }

            BookStorage BS = new BookStorage();
            BSOneDrive.SetSync( BS.SyncSettings );
        }

        private void LoadFiles( object sender, RoutedEventArgs e )
        {
            FileListContext.Load();
        }

        private async void LoadUrl( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "AdvDM" );

            DownloadBookContext UrlC = new DownloadBookContext();
            Dialogs.Rename UrlBox = new Dialogs.Rename( UrlC, stx.Text( "Download_Location" ) );
            UrlBox.Placeholder = "http://example.com/NN. XXXX.txt";

            await Popups.ShowDialog( UrlBox );

            if ( UrlBox.Canceled ) return;

            FileListContext.LoadUrl( UrlC );
        }

        public class DownloadBookContext : INamable
        {
            private Uri _url;

            public Regex Re = new Regex( @"https?://.+/(\d+)\.([\w\W]+\.)?txt$" );

            public Uri Url { get { return _url; } }

            public string Id { get; private set; }
            public string Title { get; private set; }

            public string Name
            {
                get { return _url == null ? "" : _url.ToString(); }
                set
                {
                    Match m = Re.Match( value );
                    if ( !m.Success )
                    {
                        throw new Exception( "Invalid Url" );
                    }
                    else
                    {
                        Id = m.Groups[ 1 ].Value;
                        Title = m.Groups[ 2 ].Value;
                    }

                    _url = new Uri( value );
                }
            }
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        private void FileList_ItemClick( object sender, ItemClickEventArgs e )
        {
            LocalBook Item = e.ClickedItem as LocalBook;

            // Prevent double processing on the already processed item
            if ( !Item.ProcessSuccess )
            {
                ProcessItem( Item );
            }

            if ( Item.ProcessSuccess )
            {
                if( Item is SpiderBook )
                {
                    BackMask.HandleForward(
                        Frame, () => Frame.Navigate( typeof( BookInfoView ), ( Item as SpiderBook ).GetBook() )
                    );
                }
                else
                {
                    BackMask.HandleForward(
                        Frame, () => Frame.Navigate( typeof( BookInfoView ), new LocalTextDocument( Item.aid ) )
                    );
                }
            }
            else if ( !Item.Processing && Item.File != null )
            {
                Frame.Navigate( typeof( DirectTextViewer ), Item.File );
            }
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            FileListContext.SearchTerm = sender.Text.Trim();
        }

        private void ProcessAll( object sender, RoutedEventArgs e )
        {
            FileListContext.Terminate = !FileListContext.Terminate;
            FileListContext.ProcessAll();
        }

        private void FavMode( object sender, RoutedEventArgs e )
        {
            FileListContext.ToggleFavs();

            IconBase b = ( sender as Button ).ChildAt<IconBase>( 0, 0, 0 );
            if ( FileListContext.FavOnly )
            {
                b.Foreground = new SolidColorBrush( Properties.APPEARENCE_THEME_MAJOR_COLOR );
            }
            else
            {
                b.Foreground = new SolidColorBrush( Properties.APPEARENCE_THEME_RELATIVE_SHADES_COLOR );
            }
        }

        private async void GotoSettings( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Settings" );
            StringResources stapp = new StringResources( "AppBar" );
            StringResources stm = new StringResources( "Message" );

            bool Go = false;
            Windows.UI.Popups.MessageDialog Msg = new Windows.UI.Popups.MessageDialog( stx.Text( "Preface" ), stapp.Text( "Settings" ) );

            Msg.Commands.Add(
                new Windows.UI.Popups.UICommand(
                    stm.Str( "Yes" )
                    , ( c ) => Go = true
                )
            );

            Msg.Commands.Add(
                new Windows.UI.Popups.UICommand( stm.Str( "No" ) )
            );

            await Popups.ShowDialog( Msg );

            if ( Go )
            {
                Frame.Navigate( typeof( Settings.MainSettings ) );
            }
        }

        private void ShowBookAction( object sender, RightTappedRoutedEventArgs e )
        {
            Grid G = sender as Grid;
            FlyoutBase.ShowAttachedFlyout( G );

            SelectedBook = G.DataContext as LocalBook;
        }

        private void ToggleFav( object sender, RoutedEventArgs e )
        {
            SelectedBook.ToggleFav();
        }

        private void RemoveSource( object sender, RoutedEventArgs e )
        {
            SelectedBook.RemoveSource();
            FileListContext.CleanUp();
        }

        private void ViewRaw( object sender, RoutedEventArgs e )
        {
            if ( SelectedBook.File != null )
            {
                Frame.Navigate( typeof( DirectTextViewer ), SelectedBook.File );
            }
        }

        private void Reanalyze( object sender, RoutedEventArgs e )
        {
            ProcessItem( SelectedBook );
        }

        private async void ProcessItem( LocalBook LB )
        {
            await LB.Process();
            if( LB is SpiderBook )
            {
                BookInstruction BS = ( LB as SpiderBook ).GetBook();
                if( BS.Packable )
                {
                    BS.PackVolumes();
                }
            }
        }

        private void GotoGrabberPage( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( ProceduresPanel ) );
        }

        private void ImportSpider( object sender, RoutedEventArgs e )
        {
            FileListContext.OpenSpider();
        }
    }
}
