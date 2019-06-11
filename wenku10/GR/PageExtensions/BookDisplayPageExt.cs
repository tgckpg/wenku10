using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

using wenku10.Pages;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Data;
	using Database.Contexts;
	using Database.Models;
	using GSystem;
	using Model.Book;
	using Model.Book.Spider;
	using Model.ListItem;
	using Model.Pages;
	using Model.Interfaces;
	using Storage;

	sealed class BookDisplayPageExt : PageExtension
	{
		private MenuFlyout ContextMenu;

		MenuFlyoutItem OpenDefault;
		MenuFlyoutItem Edit;
		MenuFlyoutItem PinToStart;
		MenuFlyoutItem GotoTOC;
		MenuFlyoutItem GotoReader;
		MenuFlyoutItem GotoInfo;
		CompatMenuFlyoutItem DefaultTOC;
		CompatMenuFlyoutItem DefaultReader;
		CompatMenuFlyoutItem DefaultInfo;
		MenuFlyoutSubItem ExportBtn;
		MenuFlyoutItem XRBKBtn;
		MenuFlyoutItem BrowserBtn;

		MenuFlyoutSubItem ChangeDefault;
		MenuFlyoutSubItem OpenWith;

		NameValue<string> DefaultAction;

		private string ConfigId = "BkPageExt.Default";

		public BookDisplayPageExt( string ConfigId )
		{
			this.ConfigId = "BkPageExt.Default." + ConfigId;

			// Default action must be defined for widget view
			DefaultAction = new NameValue<string>( "", "" );
			DefaultAction.Value = GetDefault();
		}

		public override void Unload()
		{
		}

		protected override void SetTemplate()
		{
			StringResources stx = StringResources.Load( "AppBar", "AppResources", "ContextMenu", "Resources" );

			OpenDefault = new MenuFlyoutItem() { Text = "Open", FontWeight = FontWeights.Bold };
			OpenDefault.Click += OpenDefault_Click;
			OpenDefault.Text = stx.Text( "Open", "ContextMenu" );

			Edit = new MenuFlyoutItem() { Text = stx.Text( "Edit", "ContextMenu" ) };
			Edit.Click += Edit_Click;

			GotoTOC = new MenuFlyoutItem() { Text = stx.Text( "TOC" ) };
			GotoTOC.Click += GotoTOC_Click;

			GotoReader = new MenuFlyoutItem() { Text = stx.Str( "Kb_For_ContentReader", "Resources" ) };
			GotoReader.Click += GotoReader_Click;

			GotoInfo = new MenuFlyoutItem() { Text = stx.Text( "BookInfoView" ) };
			GotoInfo.Click += GotoInfo_Click;

			DefaultTOC = UIAliases.CreateMenuFlyoutItem( stx.Text( "TOC" ), new SymbolIcon( Symbol.Accept ) );
			DefaultTOC.Click += DefaultTOC_Click;

			DefaultReader = UIAliases.CreateMenuFlyoutItem( stx.Str( "Kb_For_ContentReader", "Resources" ), new SymbolIcon( Symbol.Accept ) );
			DefaultReader.Click += DefaultReader_Click;

			DefaultInfo = UIAliases.CreateMenuFlyoutItem( stx.Text( "BookInfoView" ), new SymbolIcon( Symbol.Accept ) );
			DefaultInfo.Click += DefaultInfo_Click;

			BrowserBtn = new MenuFlyoutItem() { Text = stx.Text( "OpenInBrowser" ) };
			BrowserBtn.Click += BrowserBtn_Click;

			XRBKBtn = new MenuFlyoutItem() { Text = "XRBK" };
			XRBKBtn.Click += ExportXRBKBtn_Click;

			ExportBtn = new MenuFlyoutSubItem() { Text = stx.Text( "Export" ) };
			ExportBtn.Items.Add( XRBKBtn );

			OpenWith = new MenuFlyoutSubItem() { Text = stx.Text( "OpenWith", "ContextMenu" ) };
			OpenWith.Items.Add( GotoTOC );
			OpenWith.Items.Add( GotoReader );
			OpenWith.Items.Add( GotoInfo );

			PinToStart = new MenuFlyoutItem() { Text = stx.Text( "PinToStart", "ContextMenu" ) };
			PinToStart.Click += PinToStart_Click;

			ContextMenu = new MenuFlyout();
			ContextMenu.Items.Add( OpenDefault );
			ContextMenu.Items.Add( Edit );
			ContextMenu.Items.Add( new MenuFlyoutSeparator() );
			ContextMenu.Items.Add( PinToStart );
			ContextMenu.Items.Add( ExportBtn );
			ContextMenu.Items.Add( new MenuFlyoutSeparator() );
			ContextMenu.Items.Add( OpenWith );
			ChangeDefault = new MenuFlyoutSubItem() { Text = stx.Text( "ChangeDefault", "ContextMenu" ) };
			ChangeDefault.Items.Add( DefaultTOC );
			ChangeDefault.Items.Add( DefaultReader );
			ChangeDefault.Items.Add( DefaultInfo );
			ContextMenu.Items.Add( ChangeDefault );
			ContextMenu.Items.Add( BrowserBtn );

			DefaultAction.PropertyChanged += DefaultAction_PropertyChanged;
		}

		public override FlyoutBase GetContextMenu( FrameworkElement elem )
		{
			if ( elem.DataContext is GRRow<BookDisplay> BkRow )
			{
				BookDisplay BkDisplay = BkRow.Source;
				BookType BType = BkDisplay.Entry.Type;

				Edit.Visibility = Visibility.Collapsed;
				GotoInfo.Visibility = Visibility.Collapsed;
				DefaultInfo.Visibility = Visibility.Collapsed;

				if ( BType == BookType.S )
				{
					Edit.Visibility = Visibility.Visible;
					Edit.IsEnabled = BkDisplay.Entry.Id != 0;
				}

				if( BType != BookType.L )
				{
					GotoInfo.Visibility = Visibility.Visible;
					DefaultInfo.Visibility = Visibility.Visible;
				}

				BrowserBtn.IsEnabled = !string.IsNullOrEmpty( BkDisplay.Entry.Info.OriginalUrl );

				return ContextMenu;
			}

			return null;
		}

		public void OpenItem( object obj )
		{
			switch ( DefaultAction.Value )
			{
				case "TOC": OpenTOC( obj ); break;
				case "Info": OpenInfo( obj ); break;
				case "Reader": OpenReader( obj ); break;
			}
		}

		private void BrowserBtn_Click( object sender, RoutedEventArgs e )
		{
			if ( ( ( FrameworkElement ) sender ).DataContext is GRRow<BookDisplay> BkRow )
			{
				var j = Windows.System.Launcher.LaunchUriAsync( new Uri( BkRow.Source.Entry.Info.OriginalUrl ) );
			}
		}

		private async void PinToStart_Click( object sender, RoutedEventArgs e )
		{
			if ( ( ( FrameworkElement ) sender ).DataContext is GRRow<BookDisplay> BkRow )
			{
				BookItem BkItem = ItemProcessor.GetBookItem( BkRow.Source.Entry );
				string TileId = await PageProcessor.PinToStart( BkItem );
				if ( !string.IsNullOrEmpty( TileId ) )
				{
					PinManager PM = new PinManager();
					PM.RegPin( BkItem, TileId, true );

					if ( BkItem is BookInstruction BInst )
					{
						SpiderBook SBook = await SpiderBook.CreateSAsync( BkItem.ZoneId, BkItem.ZItemId, null );
						await PageProcessor.RegLiveSpider( SBook, BInst, TileId );
					}
				}
			}
		}

		private void ExportXRBKBtn_Click( object sender, RoutedEventArgs e )
		{
			ExportXRBK( ( ( FrameworkElement ) sender ).DataContext );
		}

		private void OpenDefault_Click( object sender, RoutedEventArgs e )
		{
			switch ( DefaultAction.Value )
			{
				case "TOC": GotoTOC_Click( sender, e ); break;
				case "Info": GotoInfo_Click( sender, e ); break;
				case "Reader": GotoReader_Click( sender, e ); break;
			}
		}

		private async void Edit_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<BookDisplay> BkRow )
			{
				BookItem BkItem = ItemProcessor.GetBookItem( BkRow.Source.Entry );

				SpiderBook SBk = await SpiderBook.CreateSAsync( BkItem.ZoneId, BkItem.ZItemId, null );
				if ( SBk.CanProcess )
				{
					ControlFrame.Instance.NavigateTo( PageId.PROC_PANEL, () => new ProcPanelWrapper( SBk.MetaLocation ) );
				}
			}
		}

		private void DefaultAction_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if( e.PropertyName == "Value" )
			{
				DefaultInfo.Icon2.Opacity = 0;
				DefaultReader.Icon2.Opacity = 0;
				DefaultTOC.Icon2.Opacity = 0;

				StringResources stx = StringResources.Load( "AppBar", "Resources", "ContextMenu" );
				switch ( DefaultAction.Value )
				{
					case "TOC":
						DefaultTOC.Icon2.Opacity = 1;
						OpenDefault.Text = stx.Text( "Open", "ContextMenu" ) + " (" + stx.Text( "TOC" ) + ")";
						break;
					case "Info":
						DefaultInfo.Icon2.Opacity = 1;
						OpenDefault.Text = stx.Text( "Open", "ContextMenu" ) + " (" + stx.Text( "BookInfoView" ) + ")";
						break;
					case "Reader":
						DefaultReader.Icon2.Opacity = 1;
						OpenDefault.Text = stx.Text( "Open", "ContextMenu" ) + " (" + stx.Str( "Kb_For_ContentReader", "Resources" ) + ")";
						break;
				}
			}
		}

		private void DefaultReader_Click( object sender, RoutedEventArgs e ) => SetDefault( "Reader" );
		private void DefaultInfo_Click( object sender, RoutedEventArgs e ) => SetDefault( "Info" );
		private void DefaultTOC_Click( object sender, RoutedEventArgs e ) => SetDefault( "TOC" );

		private void GotoInfo_Click( object sender, RoutedEventArgs e ) => OpenInfo( ( ( FrameworkElement ) sender ).DataContext );
		private void GotoTOC_Click( object sender, RoutedEventArgs e ) => OpenTOC( ( ( FrameworkElement ) sender ).DataContext );
		private void GotoReader_Click( object sender, RoutedEventArgs e ) => OpenReader( ( ( FrameworkElement ) sender ).DataContext );

		private void OpenInfo( object DataContext )
		{
			if ( TryGetBookItem( DataContext, out BookItem BkItem ) )
			{
				ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new wenku10.Pages.BookInfoView( BkItem ) );
			}
		}

		private void OpenTOC( object DataContext )
		{
			if ( TryGetBookItem( DataContext, out BookItem BkItem ) )
			{
				if ( Page == null )
				{
					OpenInfo( DataContext );
				}
				else
				{
					PageProcessor.NavigateToTOC( Page, BkItem );
				}
			}
		}

		private async void OpenReader( object DataContext )
		{
			if ( TryGetBookItem( DataContext, out BookItem BkItem ) )
			{
				AsyncTryOut<Chapter> TryAutoAnchor = await PageExtOperations.Run( PageProcessor.TryGetAutoAnchor( BkItem ) );
				if ( TryAutoAnchor )
				{
					PageProcessor.NavigateToReader( BkItem, TryAutoAnchor.Out );
				}
				else
				{
					StringResources stx = StringResources.Load( "Message" );
					await Popups.ShowDialog( UIAliases.CreateDialog( stx.Str( "AnchorNotSetYet" ) ) );
					OpenTOC( BkItem );
				}
			}
		}

		private async void ExportXRBK( object DataContext )
		{
			if ( TryGetBookItem( DataContext, out BookItem BkItem ) )
			{
				IStorageFile ISF = await AppStorage.SaveFileAsync( "GR Book ( XRBK )", new string[] { ".xrbk" }, BkItem.Title );
				if ( ISF != null )
				{
					await ItemProcessor.WriteXRBK( BkItem, ISF );
				}
			}
		}

		private bool TryGetBookItem( object DataContext, out BookItem BkItem )
		{
			BkItem = null;
			if ( DataContext is GRRow<BookDisplay> BkRow )
			{
				BkItem = ItemProcessor.GetBookItem( BkRow.Source.Entry );
			}
			else if ( DataContext is BookItem )
			{
				BkItem = ( BookItem ) DataContext;
			}

			return BkItem != null;
		}

		private void SetDefault( string ActionName )
		{
			if ( ActionName == DefaultAction.Value )
				return;

			using ( SettingsContext Settings = new SettingsContext() )
			{
				GRSystem Config = Settings.System.Find( ConfigId );

				// Set the default configs
				if ( Config == null )
				{
					Config = new GRSystem() { Key = ConfigId, Type = GSDataType.STRING };
					Config.Value = ActionName;

					Settings.System.Add( Config );
				}
				else
				{
					Config.Value = ActionName;
					Settings.System.Update( Config );
				}

				Settings.SaveChanges();
			}

			DefaultAction.Value = ActionName;
		}

		private string GetDefault()
		{
			using ( SettingsContext Settings = new SettingsContext() )
			{
				GRSystem Config = Settings.System.Find( ConfigId );

				// Set the default configs
				if ( Config == null )
				{
					return "Info";
				}
				else
				{
					return Config.Value;
				}
			}
		}

	}
}