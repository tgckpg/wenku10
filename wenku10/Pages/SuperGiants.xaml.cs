using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Microsoft.Services.Store.Engagement;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using GR.CompositeElement;
using GR.Effects;
using GR.Effects.P2DFlow;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Loaders;
using GR.Model.Pages;
using GR.Model.Topics;
using GR.Resources;
using GR.Settings;

namespace wenku10.Pages
{
	using Scenes;

	sealed partial class SuperGiants : Page, IAnimaPage, ICmdControls, IDisposable
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		// Fireflies scroll effect
		private float PrevOffset = 0;

		Stack<Particle> PStack;
		HyperBannerItem[] HBItems;

		AppBarButton FeedbackBtn;
		AppBarButton NewsBtn;
		Storyboard NewsStory;

		ILoader<ActiveItem> Loader;

		public SuperGiants( ILoader<ActiveItem> Loader )
		{
			this.Loader = Loader;

			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			InitAppBar();

			LayoutRoot.RenderTransform = new TranslateTransform();
			LayoutRoot.ViewChanged += LayoutRoot_ViewChanged;

			CanvasListView.RegisterPropertyChangedCallback( TagProperty, UpdateCanvas );

			PStack = new Stack<Particle>();

			int l = MainStage.Instance.IsPhone ? 100 : 500;

			for ( int i = 0; i < l; i++ )
				PStack.Push( new Particle() );

			LoadContents();
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "NavigationTitles" );

			if ( StoreServicesFeedbackLauncher.IsSupported() )
			{
				FeedbackBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.FeedbackApp, stx.Text( "Feedback" ) );
				FeedbackBtn.Click += FeedbackBtn_Click;
				MinorControls = new ICommandBarElement[] { FeedbackBtn };
			}

			NewsBtn = UIAliases.CreateAppBarBtn( Symbol.Important, stx.Text( "News" ) );
			NewsBtn.Click += NewsBtn_Click;

			NewsStory = new Storyboard();
			SimpleStory.DoubleAnimation( NewsStory, NewsBtn, "Opacity", 0, 1, 350 );
			NewsStory.AutoReverse = true;
			NewsStory.RepeatBehavior = RepeatBehavior.Forever;

			MajorControls = new ICommandBarElement[] { NewsBtn };

			GetAnnouncements();

			MessageBus.SendUI( typeof( GR.GSystem.ActionCenter ), AppKeys.PM_CHECK_TILES );
		}

		private void LayoutRoot_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			float CurrOffset = ( float ) LayoutRoot.VerticalOffset;
			HBItems.ExecEach( x => x.FireFliesScene.WindBlow( CurrOffset - PrevOffset ) );
			PrevOffset = CurrOffset;
		}

		private async void LoadContents()
		{
			IList<ActiveItem> Items = await Loader.NextPage( 4 );

			bool NarrowScreen = "V".Equals( CanvasListView.Tag );
			int i = 0;

			HBItems = Items.Remap( x =>
			{
				HyperBannerItem Item = new HyperBannerItem( x, PStack );
				Item.Index = i++;
				Item.NarrowScr = NarrowScreen;
				Item.SetBanner( LayoutRoot );
				return Item;
			} );

			CanvasListView.ItemsSource = HBItems;
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

		private void SuperGiants_Hover( object sender, PointerRoutedEventArgs e )
		{
			GridContext( sender ).Banner?.Hover();
		}

		private void SuperGiants_PointerPressed( object sender, PointerRoutedEventArgs e )
		{
			GridContext( sender ).Banner?.Focus();
		}

		private void SuperGiants_PointerExited( object sender, PointerRoutedEventArgs e )
		{
			GridContext( sender ).Banner?.Blur();
		}

		private void UpdateCanvas( DependencyObject sender, DependencyProperty dp )
		{
			bool NarrowScreen = "V".Equals( CanvasListView.Tag );
			HBItems?.ExecEach( x => x.NarrowScr = NarrowScreen );
		}

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
			Type Orb = typeof( TheOrb );
			HBItems.ExecEach( x => { var j = x.Stage.Remove( Orb ); } );

			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 500, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30, 350, 500, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 850 );
		}
		#endregion

		private async void GetAnnouncements()
		{
			NewsLoader AS = new NewsLoader();
			await AS.Load();

			if ( AS.HasNewThings ) NewsStory.Begin();
		}

		private void FeedbackBtn_Click( object sender, RoutedEventArgs e )
		{
			var j = StoreServicesFeedbackLauncher.GetDefault()?.LaunchAsync();
		}

		private void NewsBtn_Click( object sender, RoutedEventArgs e ) { ShowNews(); }

		private async void ShowNews()
		{
			NewsStory.Stop();

			Dialogs.Announcements NewsDialog = new Dialogs.Announcements();
			await Popups.ShowDialog( NewsDialog );
		}
	}
}