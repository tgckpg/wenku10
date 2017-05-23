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

using wenku8.AdvDM;
using wenku8.CompositeElement;
using wenku8.Config;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Pages;
using wenku8.Model.Section;
using wenku8.Resources;
using wenku8.Settings;
using wenku8.Storage;

using TokenManager = wenku8.System.TokenManager;
using WComments = wenku10.Pages.BookInfoControls.Comments;

namespace wenku10.Pages
{
	using Dialogs;
	using Sharers;

	sealed partial class BookInfoView : Page, ICmdControls, IAnimaPage, INavPage
	{
		private static readonly string ID = typeof( BookInfoView ).Name;

		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get ; private set; }

		private global::wenku8.Settings.Layout.BookInfoView LayoutSettings;

		AppBarButton FavBtn;
		AppBarButton BrowserBtn;
		AppBarButton TOCBtn;
		AppBarButton HSBtn;
		AppBarButton CommentBtn;
		AppBarButton AuthorBtn;

		Storyboard CacheStateStory;

		private volatile bool BookLoading = false;

		private BookInfoView()
		{
			this.InitializeComponent();
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

		public void SoftOpen()
		{
			LayoutSettings.GetBgContext( "INFO_VIEW" ).ApplyBackgrounds();
			SyncAnchors();
		}

		public void SoftClose() { }

		private void SetTemplate()
		{
			LayoutSettings = new global::wenku8.Settings.Layout.BookInfoView();

			Indicators.RenderTransform = new TranslateTransform();
			HeaderPanel.RenderTransform = new TranslateTransform();
			StatusPanel.RenderTransform = new TranslateTransform();
			IntroText.RenderTransform = new TranslateTransform();

			InitAppBar();

			CacheStateStory = new Storyboard();
			ReloadIcon.RenderTransform = new RotateTransform() { CenterX = 7.5, CenterY = 7.5 };
			SimpleStory.DoubleAnimation( CacheStateStory, ReloadIcon.RenderTransform, "Angle", 0, 360, 2000, 0, new SineEase() );
			CacheStateStory.RepeatBehavior = RepeatBehavior.Forever;
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppBar", "ContextMenu", "AppResources" );

			// Major Controls
			FavBtn = UIAliases.CreateAppBarBtn( Symbol.Favorite, "" );
			FavBtn.Click += AddOrRemoveFav;

			TOCBtn = UIAliases.CreateAppBarBtn( Symbol.OpenWith, stx.Text( "TOC" ) );
			TOCBtn.Click += TOCBtn_Click;

			// Comment Button
			CommentBtn = UIAliases.CreateAppBarBtn( Symbol.Comment, stx.Text( "Comments", "AppResources" ) );

			// Minor Controls
			AppBarButton ThemeBtn = UIAliases.CreateAppBarBtn( Symbol.Caption, stx.Text( "CustomBackground", "ContextMenu" ) );
			ThemeBtn.Click += ( s, e ) => { FlyoutBase.ShowAttachedFlyout( ThemeBtn ); };

			FlyoutBase.SetAttachedFlyout( ThemeBtn, ( MenuFlyout ) Resources[ "ThemeFlyout" ] );

			BrowserBtn = UIAliases.CreateAppBarBtn( Symbol.Globe, stx.Text( "OpenInBrowser" ) );
			BrowserBtn.Click += BrowserBtn_Click;

			MajorControls = new ICommandBarElement[] { FavBtn, TOCBtn };
			MinorControls = new ICommandBarElement[] { ThemeBtn, BrowserBtn };
		}

		private void OpenBook( BookItem Book )
		{
			ThisBook = Book;
			Shared.CurrentBook = Book;

			PageProcessor.ReadSecondaryTile( Book );

			CacheStateStory.Begin();

			BookLoading = true;

			if ( Book.IsEx() ) Book.XSetProp( "Mode", X.Const<string>( XProto.WProtocols, "ACTION_BOOK_META" ) );
			BookLoader BL = new BookLoader( BookLoadComplete );

			BL.Load( Book, true );
			BL.LoadIntro( Book, true );

			SyncAnchors();
			SetContext();
		}

		private async void SyncAnchors()
		{
			if ( ThisBook == null || OneDriveRing.IsActive ) return;

			OneDriveRing.IsActive = true;
			await new AutoAnchor( ThisBook ).SyncSettings();
			OneDriveRing.IsActive = false;
		}

		private void BookLoadComplete( BookItem Book )
		{
			BookLoading = false;

			var j = Dispatcher.RunIdleAsync( x =>
			{
				if ( Book.IsSpider() )
				{
					bool BingExists = new BingService( Book ).Exists();

					BingBrowserBtn.IsEnabled
						= BingCoverBtn.IsEnabled
						= BingExists;

					bool CanBing = BingExists || string.IsNullOrEmpty( Book.CoverSrcUrl );

					UsingBing.Foreground = new SolidColorBrush(
						BingExists
						? Properties.APPEARENCE_THEME_MINOR_COLOR
						: Properties.APPEARENCE_THEME_SUBTLE_TEXT_COLOR );

					UsingBing.IsEnabled = CanBing;
				}

				CacheStateStory.Stop();
			} );
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
			ToggleFav();
			ToggleAppBar();

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
				CommentBtn.IsEnabled = !ThisBook.IsLocal();
				BrowserBtn.IsEnabled = !string.IsNullOrEmpty( ThisBook.OriginalUrl );
				LayoutRoot.DataContext = ThisBook;
				InfoBgGrid.DataContext = LayoutSettings.GetBgContext( "INFO_VIEW" );
			}
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

		private void FlyoutBase_Click( object sender, RoutedEventArgs e )
		{
			FlyoutBase.ShowAttachedFlyout( ( FrameworkElement ) sender );
		}

		private void BrowserBtn_Click( object sender, RoutedEventArgs e )
		{
			var j = Windows.System.Launcher.LaunchUriAsync( new Uri( ThisBook.OriginalUrl ) );
		}

		private void TOCBtn_Click( object sender, RoutedEventArgs e )
		{
			ControlFrame.Instance.SubNavigateTo( this, () => LayoutSettings.HorizontalTOC ? new TOCViewHorz( ThisBook ) : ( Page ) new TOCViewVert( ThisBook ) );
		}

		private void ReloadBtn_Click( object sender, RoutedEventArgs e )
		{
			if ( BookLoading ) return;
			BookLoading = true;

			CacheStateStory.Begin();
			BookLoader BL = new BookLoader( BookLoadComplete );
			BL.Load( ThisBook );
			BL.LoadIntro( ThisBook );
		}

		private void OpenExComments( object sender, RoutedEventArgs e )
		{
			ControlFrame.Instance.SubNavigateTo( this, () => new WComments( ThisBook ) );
		}

		private async void OpenTwitter( object sender, RoutedEventArgs e )
		{
			CommentBtn.IsEnabled = false;

			if ( !LayoutSettings.TwitterConfirmed )
			{
				LayoutSettings.TwitterConfirmed = true;
				StringResources stx = new StringResources( "Message" );
				await Popups.ShowDialog( UIAliases.CreateDialog( stx.Str( "ConfirmTwitter" ), "Twitter" ) );
			}

			if ( !await wenku8.Model.Twitter.AuthData.Authenticate() ) return;

			ControlFrame.Instance.SubNavigateTo( this, () => new TwitterCommentView( ThisBook ) );

			CommentBtn.IsEnabled = true;
		}

		private async void OpenHSComments( object sender, RoutedEventArgs e )
		{
			HSBtn.IsEnabled = false;

			string Token = ( string ) new TokenManager().GetAuthById( ThisBook.Id )?.Value;
			HubScriptItem HSI = await PageProcessor.GetScriptFromHub( ThisBook.Id, Token );

			if ( HSI == null )
			{
				// Suggest Upload
				ControlFrame.Instance.SubNavigateTo( this, () => new ScriptUpload( ThisBook, SHUploadComplete ) );
			}
			else
			{
				OpenHSComment( HSI );
			}

			HSBtn.IsEnabled = true;
		}

		private void OpenHSComment( HubScriptItem HSI )
		{
			ControlFrame.Instance.NavigateTo(
				PageId.SCRIPT_DETAILS
				, () => new ScriptDetails( HSI )
				, View => ( ( ScriptDetails ) View ).OpenComment() );
		}

		private async void SHUploadComplete( string Id, string AccessToken )
		{
			await ControlFrame.Instance.CloseSubView();
			HubScriptItem HSI = await PageProcessor.GetScriptFromHub( Id, AccessToken );

			if ( ThisBook.Id != Id )
			{
				ThisBook.Update( await ItemProcessor.GetBookFromId( Id ) );
			}

			if ( HSI != null ) OpenHSComment( HSI );
		}

		private void SearchAuthor( object sender, RoutedEventArgs e )
		{
			ControlFrame.Instance.NavigateTo(
				PageId.W_SEARCH, () => new WSearch()
				, P => ( ( WSearch ) P ).SearchAuthor( ThisBook.AuthorRaw ) );
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

		private void ToggleAppBar()
		{
			StringResources stx = new StringResources( "AppBar", "AppResources", "ContextMenu" );

			if ( ThisBook.IsEx() )
			{
				VoteButton.Visibility = Visibility.Visible;

				AuthorBtn = UIAliases.CreateAppBarBtn( Symbol.ContactPresence, stx.Str( "Author" ) );
				AuthorBtn.Click += SearchAuthor;

				CommentBtn.Click += OpenExComments;

				MajorControls = new ICommandBarElement[] { FavBtn, AuthorBtn, CommentBtn, TOCBtn };
			}
			else if( ThisBook.IsSpider() )
			{
				HSBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.HomeGroup, stx.Text( "ScriptDetails", "AppResources" ) );
				HSBtn.Click += OpenHSComments;

				FavBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.Pin, stx.Text( "PinToStart", "ContextMenu" ) );
				FavBtn.Click += PinSpider;

				CommentBtn.Click += OpenTwitter;

				MajorControls = new ICommandBarElement[] { FavBtn, HSBtn, CommentBtn, TOCBtn };
			}
			else
			{
				CommentBtn.Click += OpenTwitter;

				MajorControls = new ICommandBarElement[] { FavBtn, CommentBtn, TOCBtn };
			}

			ControlChanged?.Invoke( this );
		}

		private async void PinSpider( object sender, RoutedEventArgs e )
		{
			string TileId = await PageProcessor.PinToStart( ThisBook );

			if ( !string.IsNullOrEmpty( TileId ) )
			{
				PinManager PM = new PinManager();
				PM.RegPin( ThisBook, TileId, true );

				SpiderBook SpDef = await SpiderBook.CreateAsyncSpider( ThisBook.Id );
				await PageProcessor.RegLiveSpider( SpDef, ( BookInstruction ) ThisBook, TileId );
			}
		}

		private void ToggleFav()
		{
			StringResources stx = new StringResources( "AppBar" );
			if( ThisBook == null )
			{
				FavBtn.IsEnabled = false;
				FavBtn.Label = stx.Str( "FavIn" );
				return;
			}

			if( ThisBook.IsFav )
			{
				( ( SymbolIcon ) FavBtn.Icon ).Symbol = Symbol.UnFavorite;
				FavBtn.Label = stx.Str( "FavOut" );
			}
			else
			{
				( ( SymbolIcon ) FavBtn.Icon ).Symbol = Symbol.Favorite;
				FavBtn.Label = stx.Str( "FavIn" );
			}
		}

		private async void JumpButton_Click( object sender, RoutedEventArgs e )
		{
			Button Btn = ( Button ) sender;
			Btn.IsEnabled = false;

			TaskCompletionSource<TOCSection> TCS = new TaskCompletionSource<TOCSection>();
			new VolumeLoader( b =>
			{
				TCS.TrySetResult( new TOCSection( b ) );
			} ).Load( ThisBook );

			TOCSection TOCData = await TCS.Task;
			if( TOCData.AnchorAvailable )
			{
				ControlFrame.Instance.BackStack.Remove( PageId.CONTENT_READER );
				ControlFrame.Instance.NavigateTo( PageId.CONTENT_READER, () => new ContentReader( ThisBook, TOCData.AutoAnchor ) );
			}
			else
			{
				StringResources stx = new StringResources( "Message" );
				await Popups.ShowDialog( UIAliases.CreateDialog( stx.Str( "AnchorNotSetYet" ) ) );
			}

			Btn.IsEnabled = true;
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

		#region Bing Service
		private void OpenBingResult( object sender, RoutedEventArgs e )
		{
			string Url = new BingService( ThisBook ).GetSearchQuery();
			if ( !string.IsNullOrEmpty( Url ) )
			{
				var j = Windows.System.Launcher.LaunchUriAsync( new Uri( Url ) );
			}
		}

		private async void ChangeKeyword( object sender, RoutedEventArgs e )
		{
			BingService BingSrv = new BingService( ThisBook );
			string Keyword = BingSrv.GetKeyword();

			StringResources stx = new StringResources( "ContextMenu", "AppResources", "Settings", "Tips" );
			ValueHelpInput NVInput = new ValueHelpInput(
				BingSrv.DefaultKeyword, stx.Text( "ChangeKeyword" )
				, stx.Text( "Desc_InputKey", "AppResources" )
				, stx.Text( "Help", "Settings" )
			);

			NVInput.Value = Keyword;

			Flyout HelpText = new Flyout();
			HelpText.Content = new TextBlock() { Text = stx.Text( "HelpKeyword", "Tips" ) };

			NVInput.HelpBtnClick = ( s, NOP ) =>
			{
				FlyoutBase.SetAttachedFlyout( s, HelpText );
				FlyoutBase.ShowAttachedFlyout( s );
			};

			await Popups.ShowDialog( NVInput );

			if ( NVInput.Canceled ) return;

			Keyword = NVInput.Value;
			BingSrv.SetKeyword( Keyword );

			BingReloadCover();
		}

		private void ChangeCover( object sender, RoutedEventArgs e )
		{
			int Offset = int.Parse( ( ( FrameworkElement ) sender ).Tag.ToString() );
			new BingService( ThisBook ).SetOffset( Offset );

			BingReloadCover();
		}

		private void BingReloadCover()
		{
			BookLoader BL = new BookLoader( BookLoadComplete );
			ThisBook.CoverSrcUrl = null;
			BL.LoadCover( ThisBook, false );
		}

		private async void SetSubsKey( object sender, RoutedEventArgs e )
		{
			StringResources stx = new StringResources( "ContextMenu", "AppResources", "Tips" );
			ValueHelpInput NVInput = new ValueHelpInput(
				stx.Text( "UseDefault", "AppResources" )
				, stx.Text( "SetSubsKey" )
				, null, stx.Text( "HowToGetSubs", "Tips" )
			);

			NVInput.Value = Properties.MISC_COGNITIVE_API_KEY;
			NVInput.AllowEmpty = true;

			NVInput.HelpBtnClick = ( s, NOP ) =>
			{
				var j = Windows.System.Launcher.LaunchUriAsync( new Uri( AppLinks.HELP_API_KEY ) );
			};

			await Popups.ShowDialog( NVInput );

			if ( NVInput.Canceled ) return;

			Properties.MISC_COGNITIVE_API_KEY = NVInput.Value;
			BingService.SetApiKey( NVInput.Value );
		}
		#endregion

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			SplashCover.Opacity = 1;
			SplashCover.SplashIn();

			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel, "Opacity", 0, 1, 350, 100 );
			SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel.RenderTransform, "Y", 30, 0, 350, 100 );

			SimpleStory.DoubleAnimation( AnimaStory, StatusPanel, "Opacity", 0, 1, 350, 200 );
			SimpleStory.DoubleAnimation( AnimaStory, StatusPanel.RenderTransform, "Y", 30, 0, 350, 200 );

			SimpleStory.DoubleAnimation( AnimaStory, IntroText, "Opacity", 0, 1, 350, 300 );
			SimpleStory.DoubleAnimation( AnimaStory, IntroText.RenderTransform, "Y", 30, 0, 350, 300 );

			SimpleStory.DoubleAnimation( AnimaStory, Indicators, "Opacity", 0, 1, 350, 400 );
			SimpleStory.DoubleAnimation( AnimaStory, Indicators.RenderTransform, "Y", -30, 0, 350, 400 );

			AnimaStory.Begin();
			await Task.Delay( 1000 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			if( SplashCover.Filling )
			{
				SimpleStory.DoubleAnimation( AnimaStory, SplashCover, "Opacity", 1, 0, 350 );
			}

			SplashCover.SplashOut();

			SimpleStory.DoubleAnimation( AnimaStory, Indicators, "Opacity", 1, 0, 350, 400 );
			SimpleStory.DoubleAnimation( AnimaStory, Indicators.RenderTransform, "Y", 0, -30, 350, 400 );

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