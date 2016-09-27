using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku8.AdvDM;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Resources;
using wenku8.Settings;
using Net.Astropenguin.Controls;

namespace wenku10.Pages.ContentReaderPane
{
    sealed partial class ImageList : Page
    {
        private ContentReader ReaderPage;
        public ImageList( ContentReader R )
        {
            this.InitializeComponent();

            ReaderPage = R;
            SetTemplate();
        }

        private async void SetTemplate()
        {
            Chapter C = ReaderPage.CurrentChapter;
            if ( !C.HasIllustrations )
            {
                AsyncTryOut<Chapter> ASC;
                if ( ASC = await TryFoundIllustration() )
                {
                    C = ASC.Out;
                }
                else
                {
                    ChapterList.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            ChapterList.Visibility = Visibility.Collapsed;

            string[] ImagePaths = Shared.Storage.GetString( C.IllustrationPath )
                .Split( new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries );

            int l = ImagePaths.Length;

            WBackgroundTransfer Transfer = new WBackgroundTransfer();

            List<ImageThumb> ImgThumbs = new List<ImageThumb>();

            Transfer.OnThreadComplete += Transfer_OnThreadComplete;
            for ( int i = 0; i < l; i++ )
            {
                // Retrive URL
                string url = ImagePaths[ i ];

                // Use filename as <id>.<format> since format maybe <id>.png or <id>.jpg
                string fileName = url.Substring( url.LastIndexOf( '/' ) + 1 );
                string imageLocation = FileLinks.ROOT_IMAGE + fileName;

                ImageThumb Img = new ImageThumb( imageLocation, 200, null );
                Img.Reference = url;
                ImgThumbs.Add( Img );
            }

            MainView.ItemsSource = ImgThumbs;

            foreach( ImageThumb Thumb in ImgThumbs )
            {
                await Thumb.Set();
                if( Thumb.IsDownloadNeeded )
                {
                    Guid G = await Transfer.RegisterImage( Thumb.Reference, Thumb.Location );
                    Thumb.Id = G;
                }
            }
        }

        private void Transfer_OnThreadComplete( DTheradCompleteArgs DArgs )
        {
            ImageThumb Img = ( MainView.ItemsSource as List<ImageThumb> ).First( x => x.Id.Equals( DArgs.Id ) );
            var j = Img.Set();
        }

        private void MainView_ItemClick( object sender, ItemClickEventArgs e )
        {
            ImageThumb Img = e.ClickedItem as ImageThumb;
            if ( Img.IsDownloadNeeded ) return;

            ReaderPage.ClosePane();

            EventHandler<XBackRequestedEventArgs> ViewImage = null;
            ViewImage = ( sender2, e2 ) =>
            {
                NavigationHandler.OnNavigatedBack -= ViewImage;
                ReaderPage.RollOutLeftPane();
            };

            NavigationHandler.InsertHandlerOnNavigatedBack( ViewImage );

            ReaderPage.OverNavigate( typeof( ImageView ), Img );
        }

        private async Task<AsyncTryOut<Chapter>> TryFoundIllustration()
        {
            VolumesInfo VF = new VolumesInfo( ReaderPage.CurrentBook );
            EpisodeStepper ES = new EpisodeStepper( VF );

            ES.SetCurrentPosition( ReaderPage.CurrentChapter, true );

            List<Chapter> Chs = new List<Chapter>();

            bool NeedDownload = false;

            string Vid = ReaderPage.CurrentChapter.vid;
            while ( ES.Vid == Vid )
            {
                Chapter Ch = ES.Chapter;
                Chs.Add( Ch );

                if ( !Ch.IsCached ) NeedDownload = true;
                if( Ch.HasIllustrations )
                {
                    return new AsyncTryOut<Chapter>( true, Ch );
                }
                if ( !ES.StepNext() ) break;
            }

            if ( !NeedDownload )
            {
                Message.Text = "No Image for this volume";
                return new AsyncTryOut<Chapter>();
            }

            NeedDownload = false;

            StringResources stm = new StringResources( "Message" );
            MessageDialog Msg = new MessageDialog( "Not enough information to see if there were any illustrations within this volume. Download this volume?" );

            Msg.Commands.Add(
                new UICommand( stm.Str( "Yes" ), ( x ) => NeedDownload = true )
            );

            Msg.Commands.Add( new UICommand( stm.Str( "No" ) ) );

            await Popups.ShowDialog( Msg );

            if ( !NeedDownload )
            {
                Message.Text = "Not enough information for finding illustrations. Consider downloading a specific chapter";
                return new AsyncTryOut<Chapter>();
            }

            // Really, this desperate?
            TaskCompletionSource<AsyncTryOut<Chapter>> TCSChapter = new TaskCompletionSource<AsyncTryOut<Chapter>>();
            Volume V = ReaderPage.CurrentBook.GetVolumes().First( x => x.vid == ReaderPage.CurrentChapter.vid );
            ChapterList.ItemsSource = V.ChapterList;

            WRuntimeTransfer.DCycleCompleteHandler CycleComp = null;

            CycleComp = delegate ( object sender, DCycleCompleteArgs e )
            {
                App.RuntimeTransfer.OnCycleComplete -= CycleComp;
                bool AllSet = V.ChapterList.All( x => x.IsCached );

                Chapter C = V.ChapterList.FirstOrDefault( x => x.HasIllustrations );

                if ( C == null )
                {
                    if ( AllSet ) Worker.UIInvoke( () => Message.Text = "No Illustration available" );
                    TCSChapter.TrySetResult( new AsyncTryOut<Chapter>() );
                    return;
                }

                TCSChapter.TrySetResult( new AsyncTryOut<Chapter>( true, C ) );
            };

            if ( ReaderPage.CurrentBook is BookInstruction )
            {
                foreach( SChapter C in V.ChapterList.Cast<SChapter>() )
                {
                    await new ChapterLoader().LoadAsync( C );
                    C.UpdateStatus();
                }

                // Fire the event myself
                CycleComp( this, new DCycleCompleteArgs() );
            }
            else
            {
                App.RuntimeTransfer.OnCycleComplete += CycleComp;
                AutoCache.DownloadVolume( ReaderPage.CurrentBook, V );
            }

            return await TCSChapter.Task;
        }
    }
}