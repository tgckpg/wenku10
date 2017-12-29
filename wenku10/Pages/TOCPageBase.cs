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

using GR.Config;
using GR.CompositeElement;
using GR.Database.Models;
using GR.Model.Interfaces;
using GR.Model.Loaders;
using GR.Model.Pages;
using GR.Model.Section;
using GR.Storage;
using GR.Resources;

using BookItem = GR.Model.Book.BookItem;

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
		protected global::GR.Settings.Layout.BookInfoView LayoutSettings;

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
			LayoutSettings = new global::GR.Settings.Layout.BookInfoView();
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

			AppBarButtonEx ReloadBtn = UIAliases.CreateAppBarBtnEx( Symbol.Refresh, stx.Text( "Reload" ) );

			ReloadBtn.Click += ( sender, e ) =>
			{
				ReloadBtn.IsLoading = true;
				new VolumeLoader( b =>
				{
					SetTOC( b );
					ReloadBtn.IsLoading = false;
				} ).Load( ThisBook, false );
			};

			Btns.Add( ReaderDirBtn );
			Btns.Add( JumpMarkBtn );
			Btns.Add( ReloadBtn );

			MajorControls = Btns.ToArray();
		}

		protected void VolumeChanged( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count() < 1 ) return;
			TOCData.SelectVolume( ( Volume ) e.AddedItems[ 0 ] );
		}

		protected void ChapterSelected( object sender, ItemClickEventArgs e )
		{
			PageProcessor.NavigateToReader( ThisBook, ( Chapter ) e.ClickedItem );
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
			PageProcessor.NavigateToReader( ThisBook, TOCData.AutoAnchor );
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
					RightClickedVolume.Title, stx.Text( "AutoUpdate", "ContextMenu" )
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