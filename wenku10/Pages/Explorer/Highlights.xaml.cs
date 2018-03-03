using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using GR.Effects;
using GR.Effects.P2DFlow;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Pages;
using GR.Model.Topics;

namespace wenku10.Pages.Explorer
{
	using Scenes;

	public sealed partial class Highlights : UserControl, IAnimaPage,  IDisposable
	{
		public static readonly DependencyProperty ViewModeProperty = DependencyProperty.Register(
			"ViewMode", typeof( string ), typeof( Highlights )
			, new PropertyMetadata( "H", OnViewModeChanged ) );

		public string ViewMode
		{
			get { return ( string ) GetValue( ViewModeProperty ); }
			set { SetValue( ViewModeProperty, value ); }
		}

		// Fireflies scroll effect
		private float PrevOffset = 0;

		Stack<Particle> PStack;
		HyperBannerItem[] HBItems;

		public Highlights()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public async void View( ILoader<ActiveItem> Loader )
		{
			IList<ActiveItem> Items = await Loader.NextPage( 4 );

			bool NarrowScreen = ( ViewMode == "V" );

			HBItems = Items.Remap( ( x, i ) =>
			{
				HyperBannerItem Item = new HyperBannerItem( x, PStack )
				{
					Index = i,
					NarrowScr = NarrowScreen
				};
				Item.SetBanner( LayoutRoot );
				return Item;
			} );

			CanvasListView.ItemsSource = HBItems;
		}

		private void SetTemplate()
		{
			LayoutRoot.RenderTransform = new TranslateTransform();
			LayoutRoot.ViewChanged += LayoutRoot_ViewChanged;

			PStack = new Stack<Particle>();

			int l = MainStage.Instance.IsPhone ? 100 : 500;

			for ( int i = 0; i < l; i++ )
				PStack.Push( new Particle() );
		}

		private void LayoutRoot_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			float CurrOffset = ( float ) LayoutRoot.VerticalOffset;
			HBItems.ExecEach( x => x.FireFliesScene.WindBlow( CurrOffset - PrevOffset ) );
			PrevOffset = CurrOffset;
		}

		public void Dispose()
		{
			HBItems?.ExecEach( x => x.Dispose() );
		}

		private HyperBannerItem GridContext( object sender )
		{
			Grid Banner = ( Grid ) sender;
			return ( HyperBannerItem ) Banner.DataContext;
		}

		private void SuperGiants_Hover( object sender, PointerRoutedEventArgs e ) => GridContext( sender ).Banner?.Hover();
		private void SuperGiants_PointerPressed( object sender, PointerRoutedEventArgs e ) => GridContext( sender ).Banner?.Focus();
		private void SuperGiants_PointerExited( object sender, PointerRoutedEventArgs e ) => GridContext( sender ).Banner?.Blur();

		private void SuperGiants_Tapped( object sender, TappedRoutedEventArgs e )
		{
			ControlFrame.Instance.StopReacting();
			HyperBannerItem Item = GridContext( sender );
			Item.Banner?.Click();

			NameValue<Func<Page>> Handler = PageProcessor.GetPageHandler( Item.Source );
			ControlFrame.Instance.NavigateTo( Handler.Name, Handler.Value );
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
			await Task.Delay( 850 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 500, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30, 350, 500, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 850 );
		}
		#endregion

		private void ChangeView()
		{
			bool NarrowScreen = ViewMode == "V";
			HBItems?.ExecEach( x => x.NarrowScr = NarrowScreen );
		}

		private static void OnViewModeChanged( DependencyObject d, DependencyPropertyChangedEventArgs e ) => ( ( Highlights ) d ).ChangeView();
	}
}