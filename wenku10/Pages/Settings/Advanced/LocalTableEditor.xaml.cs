using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using GR.Database.Models;
using GR.DataSources;
using GR.Model.Book;
using GR.Model.ListItem;
using GR.Resources;
using GR.PageExtensions;

namespace wenku10.Pages.Settings.Advanced
{
	public sealed partial class LocalTableEditor : Page
	{
		private Dictionary<string, ConvViewSource> ViewSources;
		private ObservableCollection<NameValue<ConvViewSource>> Tables;
		private BookItem CurrentBook;
		private ConvViewSource CurrentVS;

		public bool NeedRedraw { get; private set; }

		public LocalTableEditor()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );

			CurrentBook = e.Parameter as BookItem;
			if ( CurrentBook != null )
			{
				SetTemplate();
			}
		}

		private async void SetTemplate()
		{
			ViewSources = new Dictionary<string, ConvViewSource>();
			Tables = new ObservableCollection<NameValue<ConvViewSource>>();

			List<CustomConv> PhaseSource = await Shared.BooksDb.LoadCollectionAsync( CurrentBook.Entry, x => x.ConvPhases, x => x.Phase );

			PhaseSource.ExecEach( ( x, i ) => Tables.Add( GetPhaseVS( i, x ) ) );
			Phases.ItemsSource = Tables;

			ToggleTableView();
		}

		private async void SwitchVS( ConvViewSource VS )
		{
			CurrentVS = VS;
			await TableView.View( VS );

			VS.Extension.Initialize( this );
			( ( ConvPageExt ) VS.Extension ).ToggleSaveBtn = ToggleSaveBtn;

			AddBtn.IsEnabled = true;
		}

		private NameValue<ConvViewSource> GetPhaseVS( int i, CustomConv Phase )
		{
			StringResources stx = StringResources.Load( "Settings" );

			string Id = "Custom." + CurrentBook.GID;
			string Name = string.Format( stx.Text( "Conv_Phase" ), i );

			return new NameValue<ConvViewSource>( Name, new ConvViewSource( Name, new ConvDisplayData( Id, Phase ) ) );
		}

		private void TableTypes_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if ( 0 < e.AddedItems.Count() )
			{
				SwitchVS( ( ( NameValue<ConvViewSource> ) e.AddedItems.First() ).Value );
			}
		}

		private void AddPhase_Click( object sender, RoutedEventArgs e )
		{
			CustomConv NewPhase = new CustomConv() { Book = CurrentBook.Entry };
			Tables.Add( GetPhaseVS( Tables.Count, NewPhase ) );

			if ( Tables.Count == 1 )
			{
				ToggleTableView();
			}
		}

		private void ToggleTableView()
		{
			DeleteBtn.IsEnabled = Tables.Any();
			AddBtn.IsEnabled = Tables.Any();

			if ( DeleteBtn.IsEnabled )
			{
				Phases.SelectedIndex = 0;
				SwitchVS( Tables.First().Value );
				TableView.Visibility = Visibility.Visible;
			}
			else
			{
				TableView.Visibility = Visibility.Collapsed;
			}
		}

		private void AddBtn_Click( object sender, RoutedEventArgs e ) => ( ( ConvPageExt ) CurrentVS.Extension ).AddItem();

		private void ToggleSaveBtn( bool IsEnabled ) => SaveBtn.IsEnabled = IsEnabled;

		private void DeleteBtn_Click( object sender, RoutedEventArgs e )
		{
			SaveBtn.IsEnabled = true;
			Tables.RemoveAt( Phases.SelectedIndex );

			StringResources stx = StringResources.Load( "Settings" );
			Tables.ExecEach( ( x, i ) =>
			{
				x.Name = string.Format( stx.Text( "Conv_Phase" ), i );
			} );

			ToggleTableView();
		}

		private void SaveBtn_Click( object sender, RoutedEventArgs e )
		{
			SaveBtn.IsEnabled = false;

			CurrentBook.Entry.ConvPhases = Tables.Select( x =>
			{
				x.Value.ConvDataSource.SaveTable();
				return x.Value.ConvDataSource.PhaseTable;
			} ).ToList();

			Shared.BooksDb.SaveBook( CurrentBook.Entry );

			NeedRedraw = true;
		}

	}
}