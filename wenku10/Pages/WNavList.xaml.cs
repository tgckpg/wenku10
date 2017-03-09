using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Section;

namespace wenku10.Pages
{
	sealed partial class WNavList : Page, IAnimaPage
	{
		public static readonly string ID = typeof( WNavList ).Name;

		private VariableGridView VGrid;

		private WNavList()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public WNavList( SubtleUpdateItem Item )
			:this()
		{
			ISectionItem PS = X.Instance<ISectionItem>( XProto.NavListSection, Item.Name );
			MainSplitView.DataContext = PS;
			PS.Load( Item.Payload.ToString(), true );
		}

		public WNavList( CategorizedSection CS )
			:this()
		{
			MainSplitView.DataContext = CS;
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
		}

		private void GridView_Loaded( object sender, RoutedEventArgs e )
		{
			VGrid = sender as VariableGridView;
			VGrid.ViewChanged += VGrid_ViewChanged;
		}

		private void VGrid_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			if( VGrid.HorizontalOffset == 0 && VGrid.VerticalOffset == 0 )
			{
				MainSplitView.OpenPane();
			}
			// This is to avoid internal code calling
			else if( MainSplitView.State == PaneStates.Opened )
			{
				MainSplitView.ClosePane();
			}
		}

		private void VariableGridView_ItemClick( object sender, ItemClickEventArgs e )
		{
			BookItem b = e.ClickedItem as BookItem;
			ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( b ) );
		}

	}
}