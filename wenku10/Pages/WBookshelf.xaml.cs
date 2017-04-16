using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.CompositeElement;
using wenku8.Config;
using wenku8.Ext;
using wenku8.Effects;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Pages;
using wenku8.Resources;
using wenku8.Storage;

namespace wenku10.Pages
{
	public sealed partial class WBookshelf : Page, ICmdControls, IAnimaPage, INavPage
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private IFavSection FS;

		AppBarButtonEx ReloadBtn;
		AppBarButtonEx PinAll;

		private volatile bool Locked = false;

		public WBookshelf()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		public void SoftOpen() { FS?.Reload(); }
		public void SoftClose() { }

		private void SetTemplate()
		{
			LayoutRoot.RenderTransform = new TranslateTransform();

			InitAppBar();

			FS = X.Instance<IFavSection>( XProto.FavSection );

			FS.PropertyChanged += FS_PropertyChanged;
			LayoutRoot.DataContext = FS;

			FS.Load();
		}

		private void FS_PropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "IsLoading" )
			{
				ReloadBtn.IsLoading = FS.IsLoading;
			}
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppBar" );

			ReloadBtn = UIAliases.CreateAppBarBtnEx( Symbol.Refresh, stx.Text( "Reload" ) );
			ReloadBtn.Click += ( s, e ) =>
			{
				if ( !ReloadBtn.IsLoading )
					FS.Reload( true );
			};

			PinAll = UIAliases.CreateAppBarBtnEx( Symbol.Pin, stx.Text( "PinAll" ) );
			PinAll.Click += ( s, e ) => FS.C_PinAll();

			MajorControls = new ICommandBarElement[] { ReloadBtn };

			if ( Properties.ENABLE_ONEDRIVE )
			{
				AppBarButtonEx OneDriveButton = UIAliases.CreateAppBarBtnEx( SegoeMDL2.Cloud, stx.Text( "SyncBookmarks" ) );
				ButtonOperation SyncOp = new ButtonOperation( OneDriveButton );

				SyncOp.SetOp( OneDriveRsync );
				MinorControls = new ICommandBarElement[] { PinAll, OneDriveButton };
			}
			else
			{
				MinorControls = new ICommandBarElement[] { PinAll };
			}
		}

		private async Task OneDriveRsync()
		{
			await new BookStorage().SyncSettings();
			FS.Reload();
		}

		private void ShowFavContext( object sender, RightTappedRoutedEventArgs e )
		{
			FrameworkElement Elem = sender as FrameworkElement;
			FlyoutBase.ShowAttachedFlyout( Elem );
			FS.CurrentItem = Elem.DataContext as FavItem;
		}

		private async void FavContext( object sender, RoutedEventArgs e )
		{
			MenuFlyoutItem Item = sender as MenuFlyoutItem;
			switch( Item.Tag.ToString() )
			{
				case "Pin": FS.C_Pin(); break;
				case "RSync":
					if ( await ControlFrame.Instance.CommandMgr.WAuthenticate() )
						FS.C_RSync();
					break;
				case "AutoCache": FS.C_AutoCache(); break;
				case "Delete": FS.C_Delete(); break;
			}
		}

		private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
		{
			FS.SearchTerm = sender.Text.Trim();
		}

		private void OrderFavItems( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count < 1 ) return;
			FS.Reorder( ( sender as ComboBox ).SelectedIndex );
		}

		private async void BookClicked( object sender, ItemClickEventArgs e )
		{
			if ( Locked ) return;
			Locked = true;

			string Id = ( ( BookInfoItem ) e.ClickedItem ).Payload;

			IDeathblow Deathblow = await ItemProcessor.GetDeathblow( Id );
			BookItem Book = Deathblow == null ? ItemProcessor.GetBookEx( Id ) : Deathblow.GetBook();

			ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( Book ) );

			Locked = false;
		}

	}
}