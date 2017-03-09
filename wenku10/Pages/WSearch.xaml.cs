using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Resources;

namespace wenku10.Pages
{
	public sealed partial class WSearch : Page, ICmdControls, IAnimaPage
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get { return true; } }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		public static readonly string ID = typeof( WSearch ).Name;

		private IListLoader LL;
		private StringResources stx;

		private string SearchKey = null;

		public WSearch()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public void SearchAuthor( string Author )
		{
			SearchKey = Author;
			SearchTerm.Text = SearchKey;
			SCondition.SelectedIndex = 1;

			GetSearch( SearchKey );
		}

		private void Seacrh_ItemClick( object sender, ItemClickEventArgs e )
		{
			RestoreStatus();
			ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( ( BookItem ) e.ClickedItem ) );
		}

		private void GridView_Loaded( object sender, RoutedEventArgs e )
		{
			VGrid = sender as VariableGridView;
			VGrid.ViewChanged += VGrid_ViewChanged;
		}

		private void VGrid_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			if ( VGrid.HorizontalOffset == 0 && VGrid.VerticalOffset == 0 )
			{
				MainSplitView.OpenPane();
			}
			// This is to avoid internal code calling
			else if ( MainSplitView.State == PaneStates.Opened )
			{
				MainSplitView.ClosePane();
			}
		}

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, MainSplitView, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, MainSplitView.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, MainSplitView, "Opacity", 1, 0 );
			SimpleStory.DoubleAnimation( AnimaStory, MainSplitView.RenderTransform, "Y", 0, 30 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		private void SetTemplate()
		{
			MainSplitView.RenderTransform = new TranslateTransform();

			stx = new StringResources();
			SearchTerm.PlaceholderText = stx.Text( "Search_Tooltip" );
		}

		private void GetSearch( string Key )
		{
			IsLoading.IsActive = true;
			SearchTerm.MinWidth = 0;
			Status.Text = stx.Text( "Loading" );

			Expression<Action<IList<BookItem>>> handler = x => BookLoaded( x );
			LL = X.Instance<IListLoader>( XProto.ListLoader
				, X.Call<XKey[]>( XProto.WRequest, "GetSearch", GetSearchMethod(), Key )
				, Shared.BooksCache
				, handler.Compile()
				, false
			);
		}

		private string GetSearchMethod()
		{
			return SCondition.SelectedIndex == 0 ? "articlename" : "author";
		}

		private void RestoreStatus()
		{
			if ( string.IsNullOrEmpty( SearchKey ) ) return;

			IsLoading.IsActive = false;
			SCondition.Visibility
				= SearchTerm.Visibility
				= Visibility.Collapsed
				;
			Status.Visibility = Visibility.Visible;

			SearchTerm.Text = SearchKey;
		}

		private void BookLoaded( IList<BookItem> aList )
		{
			IsLoading.IsActive = false;
			SCondition.Visibility
				= SearchTerm.Visibility
				= Visibility.Collapsed
				;
			Status.Visibility = Visibility.Visible;

			SearchTerm.IsEnabled = false;

			Status.Text = stx.Text( "Search_ResultStamp_A" )
				+ " " + LL.TotalCount + " "
				+ stx.Text( "Search_ResultStamp_B" );

			Observables<BookItem, BookItem> ItemsObservable = new Observables<BookItem, BookItem>( aList );
			ItemsObservable.LoadEnd += ( a, b ) =>
			{
				IsLoading.IsActive = false;
			};
			ItemsObservable.LoadStart += ( a, b ) =>
			{
				IsLoading.IsActive = true;
			};

			ItemsObservable.ConnectLoader( LL );
			VGrid.ItemsSource = ItemsObservable;
		}

		private void SearchBox_QuerySubmitted( AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args )
		{
			SearchKey = args.QueryText.Trim();

			if ( string.IsNullOrEmpty( SearchKey ) )
			{
				return;
			}

			// Re-focus to disable keyboard
			this.Focus( FocusState.Pointer );

			GetSearch( SearchKey );
		}

		private void OpenSearchBar( object sender, RoutedEventArgs e )
		{
			if ( string.IsNullOrEmpty( SearchKey ) ) return;

			SCondition.Visibility
				= SearchTerm.Visibility
				= Visibility.Visible
				;

			SearchTerm.IsEnabled = true;
			Status.Visibility = Visibility.Collapsed;
			SearchTerm.Focus( FocusState.Keyboard );
		}

		private void Grid_Tapped( object sender, TappedRoutedEventArgs e )
		{
			RestoreStatus();
		}

	}
}