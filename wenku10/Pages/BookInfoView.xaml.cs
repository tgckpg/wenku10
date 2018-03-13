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
using Net.Astropenguin.Messaging;

using GR.AdvDM;
using GR.CompositeElement;
using GR.Config;
using GR.Database.Models;
using GR.Effects;
using GR.Ext;
using GR.Model.Book;
using GR.Model.Book.Spider;
using GR.Model.Interfaces;
using GR.Model.ListItem.Sharers;
using GR.Model.ListItem;
using GR.Model.Loaders;
using GR.Model.Pages;
using GR.Resources;
using GR.Settings;
using GR.Storage;

using TokenManager = GR.GSystem.TokenManager;
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

		private global::GR.Settings.Layout.BookInfoView LayoutSettings;

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

		public void SoftOpen( bool NavForward )
		{
			LayoutSettings.GetBgContext( "INFO_VIEW" ).ApplyBackgrounds();
			SyncAnchors();
		}

		public void SoftClose( bool NavForward ) { }

		private void SetTemplate()
		{
			LayoutSettings = new global::GR.Settings.Layout.BookInfoView();

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

			if ( Book.IsEx() )
				Book.XSetProp( "Mode", X.Const<string>( XProto.WProtocols, "ACTION_BOOK_META" ) );

			BookLoader BL = new BookLoader( BookLoadComplete );

			BL.Load( Book, true );
			BL.LoadIntro( Book, true );
			BL.LoadCover( Book, true );

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

					bool CanBing = BingExists || string.IsNullOrEmpty( Book.Info.CoverSrcUrl );

					UsingBing.Foreground = new SolidColorBrush( BingExists ? GRConfig.Theme.ColorMinor : GRConfig.Theme.SubtleColor );
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
				BrowserBtn.IsEnabled = !string.IsNullOrEmpty( ThisBook.Info.OriginalUrl );
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
			var j = Windows.System.Launcher.LaunchUriAsync( new Uri( ThisBook.Info.OriginalUrl ) );
		}

		private void TOCBtn_Click( object sender, RoutedEventArgs e )
		{
			PageProcessor.NavigateToTOC( this, ThisBook );
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

			if ( !await GR.Model.Twitter.AuthData.Authenticate() ) return;

			ControlFrame.Instance.SubNavigateTo( this, () => new TwitterCommentView( ThisBook ) );

			CommentBtn.IsEnabled = true;
		}

		private async void OpenHSComments( object sender, RoutedEventArgs e )
		{
			HSBtn.IsEnabled = false;

			string Token = ( string ) new TokenManager().GetAuthById( ThisBook.ZItemId )?.Value;
			HubScriptItem HSI = await PageProcessor.GetScriptFromHub( ThisBook.ZItemId, Token );

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

			if ( ThisBook.ZItemId != Id )
			{
				ThisBook.Update( await ItemProcessor.GetBookFromId( Id ) );
			}

			if ( HSI != null ) OpenHSComment( HSI );
		}

		private void SearchAuthor( object sender, RoutedEventArgs e )
		{
			MessageBus.Send( GetType(), AppKeys.SEARCH_AUTHOR, ThisBook.Entry );
		}

		private void AddOrRemoveFav( object sender, RoutedEventArgs e )
		{
			if ( ThisBook.IsFav )
			{
				ThisBook.IsFav = false;
			}
			else
			{
				ThisBook.IsFav = true;
			}

			ThisBook.SaveInfo();
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

				SpiderBook SpDef = await SpiderBook.CreateSAsync( ThisBook.ZItemId );
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
				( ( SymbolIcon ) FavBtn.Icon ).Symbol = Symbol.Favorite;
				FavBtn.Label = stx.Str( "FavOut" );
			}
			else
			{
				( ( SymbolIcon ) FavBtn.Icon ).Symbol = Symbol.OutlineStar;
				FavBtn.Label = stx.Str( "FavIn" );
			}
		}

		private async void JumpButton_Click( object sender, RoutedEventArgs e )
		{
			Button Btn = ( Button ) sender;
			Btn.IsEnabled = false;

			// AnchorSync is already handled on this page
			AsyncTryOut<Chapter> TryAutoAnchor = await PageProcessor.TryGetAutoAnchor( ThisBook, false );

			if( TryAutoAnchor )
			{
				PageProcessor.NavigateToReader( ThisBook, TryAutoAnchor.Out );
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
			BookLoader BL = new BookLoader();
			ThisBook.Info.CoverSrcUrl = null;
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
				SimpleStory.DoubleAnimation( AnimaStory, SplashCover, "Opacity", 1, 0, 350, 0, Easings.EaseInCubic );
			}

			SplashCover.SplashOut();

			SimpleStory.DoubleAnimation( AnimaStory, Indicators, "Opacity", 1, 0, 350, 400, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, Indicators.RenderTransform, "Y", 0, -30, 350, 400, Easings.EaseInCubic );

			SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel, "Opacity", 1, 0, 350, 300, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, HeaderPanel.RenderTransform, "Y", 0, 30, 350, 300, Easings.EaseInCubic );

			SimpleStory.DoubleAnimation( AnimaStory, StatusPanel, "Opacity", 1, 0, 350, 200, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, StatusPanel.RenderTransform, "Y", 0, 30, 350, 200, Easings.EaseInCubic );

			SimpleStory.DoubleAnimation( AnimaStory, IntroText, "Opacity", 1, 0, 350, 100, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, IntroText.RenderTransform, "Y", 0, 30, 350, 100, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 1000 );
		}
		#endregion

	}
}