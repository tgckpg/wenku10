using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

using wenku10.Pages;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Data;
	using DataSources;
	using Model.Book.Spider;
	using Model.ListItem;
	using Model.Loaders;
	using Model.Pages;
	using Model.Interfaces;
	using Resources;
	using Storage;

	sealed class BookSpiderPageExt : PageExtension, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => true;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private BookSpiderVS ViewSource;

		private MenuFlyout ContextMenu;

		MenuFlyoutItem Reanalyze;
		MenuFlyoutItem Edit;
		MenuFlyoutItem Copy;
		MenuFlyoutItem DeleteBtn;

		public BookSpiderPageExt( BookSpiderVS ViewSource )
			: base()
		{
			this.ViewSource = ViewSource;
		}

		public override void Unload()
		{
		}

		protected override void SetTemplate()
		{
			InitAppBar();

			StringResources stx = StringResources.Load( "ContextMenu" );
			ContextMenu = new MenuFlyout();

			Reanalyze = new MenuFlyoutItem() { Text = stx.Text( "Reanalyze" ) };
			Reanalyze.Click += Reanalyze_Click;
			ContextMenu.Items.Add( Reanalyze );

			Edit = new MenuFlyoutItem() { Text = stx.Text( "Edit" ) };
			Edit.Click += Edit_Click;
			ContextMenu.Items.Add( Edit );

			Copy = new MenuFlyoutItem() { Text = stx.Text( "Copy" ) };
			Copy.Click += Copy_Click;
			ContextMenu.Items.Add( Copy );

			DeleteBtn = new MenuFlyoutItem() { Text = stx.Text( "Delete" ) };
			DeleteBtn.Click += DeleteBtn_Click;
			ContextMenu.Items.Add( DeleteBtn );
		}

		private void InitAppBar()
		{
			StringResources stx = StringResources.Load( "ContextMenu" );

			AppBarButton ImportSpider = UIAliases.CreateAppBarBtn( SegoeMDL2.OpenFile, stx.Text( "ImportSpider" ) );
			ImportSpider.Click += OpenSpider;

			SecondaryIconButton SpiderEditor = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Edit, stx.Text( "SpiderEdit", "ContextMenu" ) );
			SpiderEditor.Click += ( s, e ) => ControlFrame.Instance.NavigateTo( PageId.PROC_PANEL, () => new ProcPanelWrapper( null ) );

			MajorControls = new ICommandBarElement[] { ImportSpider };
			Major2ndControls = new ICommandBarElement[] { SpiderEditor };
		}

		private void Edit_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ControlFrame.Instance.NavigateTo( PageId.PROC_PANEL, () => new ProcPanelWrapper( ( ( SpiderBook ) Row.Source ).MetaLocation ) );
			}
		}

		private void Copy_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ViewSource.Copy( ( SpiderBook ) Row.Source );
			}
		}

		private void Reanalyze_Click( object sender, RoutedEventArgs e )
		{
			ProcessItem( ( IGRRow ) ( ( FrameworkElement ) sender ).DataContext );
		}

		public async void ProcessItem( IGRRow DataContext )
		{
			if ( DataContext is GRRow<IBookProcess> Row )
			{
				SpiderBook BkProc = ( SpiderBook ) Row.Source;
				await ItemProcessor.ProcessLocal( BkProc );

				if ( BkProc.GetBook().Packed == true )
				{
					new VolumeLoader( ( x ) => { } ).Load( BkProc.GetBook() );
				}
			}
		}

		private void DeleteBtn_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ViewSource.Delete( Row );
			}
		}

		public override FlyoutBase GetContextMenu( FrameworkElement elem )
		{
			if ( elem.DataContext is GRRow<IBookProcess> Row )
			{
				IBookProcess BkProc = Row.Source;

				Copy.Visibility = Visibility.Visible;
				Edit.Visibility = Visibility.Visible;
				DeleteBtn.IsEnabled = !BkProc.Processing;

				return ContextMenu;
			}
			return null;
		}

		public async void OpenSpider( object sender, RoutedEventArgs e )
		{
			IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
			if ( ISF == null ) return;

			var j = ViewSource.OpenSpider( ISF );
		}

		private async void PinItemToStart( object sender, RoutedEventArgs e )
		{
			SpiderBook B = ( SpiderBook ) ( ( FrameworkElement ) sender ).DataContext;
			if ( B.ProcessSuccess )
			{
				BookInstruction Book = B.GetBook();
				string TileId = await PageProcessor.PinToStart( Book );

				if ( !string.IsNullOrEmpty( TileId ) )
				{
					PinManager PM = new PinManager();
					PM.RegPin( Book, TileId, true );

					await PageProcessor.RegLiveSpider( B, Book, TileId );
				}
			}
		}

	}
}