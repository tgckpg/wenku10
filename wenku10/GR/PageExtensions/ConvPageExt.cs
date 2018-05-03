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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

using wenku10.Pages.Dialogs;

namespace GR.PageExtensions
{
	using CompositeElement;
	using DataSources;
	using Data;
	using Model.Interfaces;
	using Model.ListItem;

	sealed class ConvPageExt : PageExtension, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands => true;
		public bool MajorNav => false;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private ConvViewSource ViewSource;

		AppBarButton SaveBtn;
		AppBarButton AddBtn;
		AppBarButton ResetBtn;
		AppBarButton ImportBtn;

		MenuFlyout ContextMenu;
		MenuFlyoutItem EditBtn;
		MenuFlyoutItem DeleteBtn;

		public Action<bool> ToggleSaveBtn;

		public ConvPageExt( ConvViewSource ViewSource )
			: base()
		{
			this.ViewSource = ViewSource;
		}

		public override void Unload()
		{
		}

		public async void AddItem()
		{
			StringResources stx = StringResources.Load( "AppBar" );
			NameValue<string> NewItem = new NameValue<string>( "", "" );
			NameValueInput NVInput = new NameValueInput(
				NewItem
				, stx.Text( "Add" )
				, ViewSource.DataSource.ColumnName( ViewSource.DataSource.Table.CellProps[ 0 ] )
				, ViewSource.DataSource.ColumnName( ViewSource.DataSource.Table.CellProps[ 1 ] )
			);

			await Popups.ShowDialog( NVInput );

			if ( !NVInput.Canceled )
			{
				ViewSource.ConvDataSource.AddItem( NewItem );
				ToggleSaveBtn( true );
			}
		}

		public async void ImportTable()
		{
			IStorageFile ISF = await AppStorage.OpenFileAsync( ".txt" );

			if ( ISF == null )
				return;

			ViewSource.ConvDataSource.ImportTable( await ISF.ReadString() );
			ToggleSaveBtn( true );
		}

		protected override void SetTemplate()
		{
			InitAppBar();

			StringResources stx = StringResources.Load( "ContextMenu" );

			ContextMenu = new MenuFlyout();

			EditBtn = new MenuFlyoutItem() { Text = stx.Text( "Edit" ) };
			EditBtn.Click += EditBtn_Click;
			ContextMenu.Items.Add( EditBtn );

			DeleteBtn = new MenuFlyoutItem() { Text = stx.Text( "Delete" ) };
			DeleteBtn.Click += DeleteBtn_Click;
			ContextMenu.Items.Add( DeleteBtn );
		}

		private void InitAppBar()
		{
			StringResources stx = StringResources.Load( "AppBar", "Settings" );
			SaveBtn = UIAliases.CreateAppBarBtn( Symbol.Save, stx.Str( "Save" ) );
			SaveBtn.IsEnabled = false;
			SaveBtn.Click += SaveBtn_Click;

			AddBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Str( "Add" ) );
			AddBtn.Click += AddBtn_Click;

			ResetBtn = UIAliases.CreateAppBarBtn( Symbol.Refresh, stx.Text( "Advanced_Server_Reset", "Settings" ) );
			ResetBtn.Click += ResetBtn_Click;

			ImportBtn = UIAliases.CreateAppBarBtn( Symbol.OpenFile, stx.Text( "Import" ) );
			ImportBtn.Click += ImportBtn_Click;

			MajorControls = new ICommandBarElement[] { AddBtn, SaveBtn };
			MinorControls = new ICommandBarElement[] { ResetBtn, ImportBtn };

			ToggleSaveBtn = ( x ) => SaveBtn.IsEnabled = x;
		}

		public override FlyoutBase GetContextMenu( FrameworkElement elem )
		{
			if ( elem.DataContext is GRRow<NameValue<string>> Row )
			{
				return ContextMenu;
			}
			return null;
		}

		private async void EditBtn_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<NameValue<string>> Row )
			{
				NameValueInput NVInput = new NameValueInput(
					Row.Source
					, EditBtn.Text
					, ViewSource.DataSource.ColumnName( ViewSource.DataSource.Table.CellProps[ 0 ] )
					, ViewSource.DataSource.ColumnName( ViewSource.DataSource.Table.CellProps[ 1 ] )
				);

				await Popups.ShowDialog( NVInput );
				if ( !NVInput.Canceled )
				{
					ToggleSaveBtn( true );
				}
			}
		}

		private void DeleteBtn_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<NameValue<string>> Row )
			{
				ViewSource.ConvDataSource.Remove( Row );
				ToggleSaveBtn( true );
			}
		}

		private void AddBtn_Click( object sender, RoutedEventArgs e ) => AddItem();

		private void SaveBtn_Click( object sender, RoutedEventArgs e )
		{
			ToggleSaveBtn( false );
			ViewSource.ConvDataSource.SaveTable();
		}

		private async void ResetBtn_Click( object sender, RoutedEventArgs e )
		{
			bool Reset = false;
			StringResources stx = StringResources.Load( "Message" );

			await Popups.ShowDialog( UIAliases.CreateDialog(
				stx.Str( "ConfirmResetConvTable" ), stx.Str( "ConfirmReset" )
				, () => Reset = true
				, stx.Str( "Yes" ), stx.Str( "No" )
			) );

			if ( Reset )
			{
				ToggleSaveBtn( false );
				ViewSource.ConvDataSource.ResetSource();
			}
		}

		private void ImportBtn_Click( object sender, RoutedEventArgs e ) => ImportTable();

	}
}