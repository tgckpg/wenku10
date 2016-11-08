using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Pages;
using wenku8.Storage;

using TokenManager = wenku8.System.TokenManager;
using WComments = wenku10.Pages.BookInfoControls.Comments;

namespace wenku10.Pages
{
    using Sharers;

    sealed partial class BookInfoView : Page, ICmdControls, IAnimaPage
    {
        private static readonly string ID = typeof( BookInfoView ).Name;

        private Grid InfoBgGrid;
        private Grid PushGrid;

        private AppBarButton FavBtn;
        private AppBarButton BrowserBtn;
        private AppBarButton TOCBtn;
        private AppBarButton CommentBtn;
        private AppBarButton AuthorBtn;

        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get ; private set; }

        private global::wenku8.Settings.Layout.BookInfoView LayoutSettings;

        private BookInfoView()
        {
            this.InitializeComponent();
            LayoutSettings = new global::wenku8.Settings.Layout.BookInfoView();
            SetTemplate();
        }

        private BookItem ThisBook;

        public BookInfoView( HubScriptItem HSI )
            :this()
        {
            OpenSpider( HSI );
        }

        public BookInfoView( BookItem Book )
            :this()
        {
            OpenBook( Book );
        }

        private void SetTemplate()
        {
            HeaderPanel.RenderTransform = new TranslateTransform();
            StatusPanel.RenderTransform = new TranslateTransform();
            IntroText.RenderTransform = new TranslateTransform();

            InitAppBar();
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "AppBar", "ContextMenu", "AppResources" );

            // Major Controls
            FavBtn = UIAliases.CreateAppBarBtn( Symbol.UnFavorite, "" );
            FavBtn.Click += AddOrRemoveFav;

            AuthorBtn = UIAliases.CreateAppBarBtn( Symbol.ContactPresence, stx.Str( "Author" ) );
            AuthorBtn.Click += SearchAuthor;

            CommentBtn = UIAliases.CreateAppBarBtn( Symbol.Comment, stx.Text( "Comments", "AppResources" ) );
            CommentBtn.Click += OpenComments;

            TOCBtn = UIAliases.CreateAppBarBtn( Symbol.OpenWith, stx.Text( "TOC" ) );
            TOCBtn.Click += OpenTOC;

            // Minor Controls
            AppBarButton ThemeBtn = UIAliases.CreateAppBarBtn( Symbol.Caption, stx.Text( "CustomBackground", "ContextMenu" ) );
            ThemeBtn.Click += ( s, e ) => { FlyoutBase.ShowAttachedFlyout( ThemeBtn ); };

            FlyoutBase.SetAttachedFlyout( ThemeBtn, ( MenuFlyout ) Resources[ "ThemeFlyout" ] );

            BrowserBtn = UIAliases.CreateAppBarBtn( Symbol.Globe, stx.Str( "OpenInBrowser" ) );
            BrowserBtn.Click += OpenInBrowser;

            MajorControls = new ICommandBarElement[] { FavBtn, AuthorBtn, CommentBtn, TOCBtn };
            MinorControls = new ICommandBarElement[] { ThemeBtn, BrowserBtn };
        }

        private void OpenBook( BookItem Book )
        {
            ThisBook = Book;
            BookLoader BL = new BookLoader( ( NOP ) => { } );
            BL.Load( Book, true );
            BL.LoadIntro( Book, true );
            SetContext();
        }

        private async void OpenSpider( HubScriptItem HSI )
        {
            BookItem Book = null;
            try
            {
                SpiderBook SBook = await SpiderBook.ImportFile( await HSI.ScriptFile.ReadString(), true );
                if ( SBook.CanProcess && !SBook.Processed )
                {
                    await ItemProcessor.ProcessLocal( SBook );
                    Book = SBook.GetBook();
                }
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.WARNING );
            }

            // Should be outside of try..catch block
            OpenBook( Book );
        }

        private void SetContext()
        {
#if DEBUG
            if ( ThisBook == null ) ThisBook = BookItem.DummyBook();
#endif
            if( ThisBook == null )
            {
                // Set Book Unavailable View
                BrowserBtn.IsEnabled
                    = TOCBtn.IsEnabled
                    = CommentBtn.IsEnabled
                    = false;
            }
            else
            {
                CommentBtn.IsEnabled = !ThisBook.IsLocal;
                BrowserBtn.IsEnabled = !string.IsNullOrEmpty( ThisBook.OriginalUrl );
                LayoutRoot.DataContext = ThisBook;
            }

            ToggleFav();
            ToggleButtons();
        }

        private void InfoBgLoaded( object sender, RoutedEventArgs e )
        {
            InfoBgGrid = ( Grid ) sender;
            InfoBgGrid.DataContext = LayoutSettings.GetBgContext( "INFO_VIEW" );
        }

        private async void ChangeBackground( object sender, RoutedEventArgs e )
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            string[] Argv = item.Tag.ToString().Split( ',' );

            if ( Argv[ 0 ] == "Preset" )
            {
                bool No = true;

                StringResources stx = new StringResources( "Message", "ContextMenu" );

                MessageDialog MsgBox = new MessageDialog( stx.Str( "BInfoView_PresetBg_Mesg" ), stx.Text( "PresetBackground", "ContextMenu" ) );
                MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { No = false; } ) );
                MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

                await Popups.ShowDialog( MsgBox );

                if ( No ) return;

            }

            LayoutSettings.GetBgContext( Argv[ 1 ] ).SetBackground( Argv[ 0 ] );
        }

        private void OpenInBrowser( object sender, RoutedEventArgs e )
        {
            var j = Windows.System.Launcher.LaunchUriAsync( new Uri( ThisBook.OriginalUrl ) );
        }

        private void OpenTOC( object sender, RoutedEventArgs e )
        {
            ControlFrame.Instance.SubNavigateTo( this, () => LayoutSettings.HorizontalTOC ? new TOCViewHorz( ThisBook ) : ( Page ) new TOCViewVert( ThisBook ) );
        }

        private async void OpenComments( object sender, RoutedEventArgs e )
        {
            CommentBtn.IsEnabled = false;

            if ( ThisBook.XTest( XProto.BookItemEx ) )
            {
                ControlFrame.Instance.SubNavigateTo( this, () => new WComments( ThisBook ) );
            }
            else if ( ThisBook is BookInstruction )
            {
                string Token = ( string ) new TokenManager().GetAuthById( ThisBook.Id )?.Value;
                HubScriptItem HSI = await ItemProcessor.GetScriptFromHub( ThisBook.Id, Token );

                if ( HSI == null )
                {
                    // Suggest Upload
                    ControlFrame.Instance.SubNavigateTo( this, () => new ScriptUpload( ThisBook, OpenHSComment ) );
                }
                else
                {
                    OpenHSComment( HSI );
                }
            }

            CommentBtn.IsEnabled = true;
        }

        private void OpenHSComment( HubScriptItem HSI )
        {
            ControlFrame.Instance.NavigateTo(
                PageId.SCRIPT_DETAILS
                , () => new ScriptDetails( HSI )
                , View => ( ( ScriptDetails ) View ).OpenComment() );
        }

        private async void OpenHSComment( string Id, string AccessToken )
        {
            await ControlFrame.Instance.CloseSubView();
            HubScriptItem HSI = await ItemProcessor.GetScriptFromHub( Id, AccessToken );

            if ( ThisBook.Id != Id )
            {
                ThisBook.Update( await ItemProcessor.GetBookFromId( Id ) );
            }

            if ( HSI != null ) OpenHSComment( HSI );
        }

        private void SearchAuthor( object sender, RoutedEventArgs e )
        {
            ControlFrame.Instance.NavigateTo( PageId.W_SEARCH, () => new WSearch( ThisBook.AuthorRaw ) );
        }

        private void AddOrRemoveFav( object sender, RoutedEventArgs e )
        {
            BookStorage BS = new BookStorage();
            if ( ThisBook.IsFav )
            {
                BS.RemoveBook( ThisBook.Id );
                ThisBook.IsFav = false;
            }
            else
            {
                BS.SaveBook( ThisBook.Id, ThisBook.Title, ThisBook.RecentUpdateRaw, ThisBook.LatestSection );
                ThisBook.IsFav = true;
            }

            ToggleFav();
        }

        private void ToggleButtons()
        {
            if ( ThisBook.XTest( XProto.BookItemEx ) )
            {
                AuthorBtn.IsEnabled = true;
                VoteButton.Visibility = Visibility.Visible;
            }
            else
            {
                AuthorBtn.IsEnabled = false;
            }
        }

        private void ToggleFav()
        {
            StringResources stx = new StringResources( "AppBar" );
            if( ThisBook == null )
            {
                FavBtn.IsEnabled = false;
                FavBtn.Label = stx.Str( "FavOut" );
                return;
            }

            if( ThisBook.IsFav )
            {
                ( ( SymbolIcon ) FavBtn.Icon ).Symbol = Symbol.Favorite;
                FavBtn.Label = stx.Str( "FavIn" );
            }
            else
            {
                ( ( SymbolIcon ) FavBtn.Icon ).Symbol = Symbol.UnFavorite;
                FavBtn.Label = stx.Str( "FavOut" );
            }
        }

        private void PushCountGridLoaded( object sender, RoutedEventArgs e )
        {
            PushGrid = sender as Grid;
        }

        private async void VoteButton_Click( object sender, RoutedEventArgs e )
        {
            bool LoggedIn = await ControlFrame.Instance.CommandMgr.WAuthenticate();
            if ( !LoggedIn ) return;

            bool Voted = await ThisBook.XCall<Task<bool>>( "Vote" );
            if( Voted )
            {
                ( PushGrid.Resources[ "DataUpdate" ] as Storyboard )?.Begin();
            }
        }

        #region Anima
        Storyboard AnimaStory = new Storyboard();

        public async Task EnterAnima()
        {
            SplashCover.SplashIn();

            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel, "Opacity", 0, 1, 350, 100 );
            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel.RenderTransform, "Y", 30, 0, 350, 100 );

            SimpleStory.DoubleAnimation( AnimaStory, StatusPanel, "Opacity", 0, 1, 350, 200 );
            SimpleStory.DoubleAnimation( AnimaStory, StatusPanel.RenderTransform, "Y", 30, 0, 350, 200 );

            SimpleStory.DoubleAnimation( AnimaStory, IntroText, "Opacity", 0, 1, 350, 300 );
            SimpleStory.DoubleAnimation( AnimaStory, IntroText.RenderTransform, "Y", 30, 0, 350, 300 );

            AnimaStory.Begin();
            await Task.Delay( 1000 );
        }

        public async Task ExitAnima()
        {
            SplashCover.SplashOut();

            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel, "Opacity", 1, 0, 350, 300 );
            SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel.RenderTransform, "Y", 0, 30, 350, 300 );

            SimpleStory.DoubleAnimation( AnimaStory, StatusPanel, "Opacity", 1, 0, 350, 200 );
            SimpleStory.DoubleAnimation( AnimaStory, StatusPanel.RenderTransform, "Y", 0, 30, 350, 200 );

            SimpleStory.DoubleAnimation( AnimaStory, IntroText, "Opacity", 1, 0, 350, 100 );
            SimpleStory.DoubleAnimation( AnimaStory, IntroText.RenderTransform, "Y", 0, 30, 350, 100 );

            AnimaStory.Begin();
            await Task.Delay( 1000 );
        }
        #endregion

    }
}