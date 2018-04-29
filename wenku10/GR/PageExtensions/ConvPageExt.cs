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

		MenuFlyout ContextMenu;
		MenuFlyoutItem EditBtn;
		MenuFlyoutItem DeleteBtn;

		public ConvPageExt( ConvViewSource ViewSource )
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

			StringResources stx = new StringResources( "ContextMenu" );

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
			StringResources stx = new StringResources( "AppBar" );
			SaveBtn = UIAliases.CreateAppBarBtn( Symbol.Save, stx.Str( "Save" ) );
			SaveBtn.IsEnabled = false;
			SaveBtn.Click += SaveBtn_Click;

			AddBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Str( "Add" ) );
			AddBtn.Click += AddBtn_Click;

			MajorControls = new ICommandBarElement[] { AddBtn, SaveBtn };
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
				SaveBtn.IsEnabled = !NVInput.Canceled;
			}
		}

		private void DeleteBtn_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<NameValue<string>> Row )
			{
				ViewSource.ConvDataSource.Remove( Row );
				SaveBtn.IsEnabled = true;
			}
		}

		private async void AddBtn_Click( object sender, RoutedEventArgs e )
		{
			NameValue<string> NewItem = new NameValue<string>( "", "" );
			NameValueInput NVInput = new NameValueInput(
				NewItem
				, AddBtn.Label
				, ViewSource.DataSource.ColumnName( ViewSource.DataSource.Table.CellProps[ 0 ] )
				, ViewSource.DataSource.ColumnName( ViewSource.DataSource.Table.CellProps[ 1 ] )
			);

			await Popups.ShowDialog( NVInput );

			if( !NVInput.Canceled )
			{
				SaveBtn.IsEnabled = true;
				ViewSource.ConvDataSource.AddItem( NewItem );
			}
		}

		private void SaveBtn_Click( object sender, RoutedEventArgs e )
		{
			SaveBtn.IsEnabled = false;
			ViewSource.ConvDataSource.SaveTable();
		}

	}
}