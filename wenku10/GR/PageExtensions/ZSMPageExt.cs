using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using wenku10.Pages;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Data;
	using DataSources;
	using Model.Interfaces;
	using Resources;
	using Settings;

	sealed class ZSMPageExt : PageExtension, ICmdControls
	{
		public readonly string ID = typeof( TextDocPageExt ).Name;

#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => true;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private ZSManagerVS ViewSource;

		private MenuFlyout ContextMenu;

		MenuFlyoutItem Edit;
		MenuFlyoutItem DeleteBtn;

		public ZSMPageExt( ZSManagerVS ViewSource )
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

			Edit = new MenuFlyoutItem() { Text = stx.Text( "Edit" ) };
			Edit.Click += Edit_Click;
			ContextMenu.Items.Add( Edit );

			DeleteBtn = new MenuFlyoutItem() { Text = stx.Text( "Delete" ) };
			DeleteBtn.Click += DeleteBtn_Click;
			ContextMenu.Items.Add( DeleteBtn );
		}

		public void ProcessItem( IGRRow DataContext )
		{
			if ( DataContext is GRRow<IMetaSpider> Row )
			{
				MessageBus.SendUI( GetType(), AppKeys.OPEN_ZONE, Row.Source );
			}
		}

		private void InitAppBar()
		{
			StringResources stx = StringResources.Load( "AppBar" );
			AppBarButton OpenFile = UIAliases.CreateAppBarBtn( SegoeMDL2.OpenFile, stx.Text( "OpenZone" ) );
			OpenFile.Click += OpenFile_Click;

			MajorControls = new ICommandBarElement[] { OpenFile };
		}

		private void Edit_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IMetaSpider> Row )
			{
				ControlFrame.Instance.NavigateTo( PageId.PROC_PANEL, () => new ProcPanelWrapper( Row.Source.MetaLocation ) );
			}
		}

		private async void OpenFile_Click( object sender, RoutedEventArgs e )
		{
			IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
			if ( ISF == null ) return;

			var j = ViewSource.ZSMData.OpenFile( ISF );
		}

		private void DeleteBtn_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IMetaSpider> Row )
			{
				ViewSource.ZSMData.RemoveZone( Row );
			}
		}

		public override FlyoutBase GetContextMenu( FrameworkElement elem )
		{
			if ( elem.DataContext is GRRow<IMetaSpider> Row )
			{

				return ContextMenu;
			}
			return null;
		}
	}
}