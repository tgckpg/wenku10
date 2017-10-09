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

using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Services.Store.Engagement;

using Net.Astropenguin.IO;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Pages;
using wenku8.Resources;
using wenku8.Settings;

namespace wenku10.Pages
{
	using Scenes;
	using BgContext = wenku8.Settings.Layout.BookInfoView.BgContext;

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

		List<Grid> StarBoxes;
		List<TextBlock> DescTexts;
		List<FireFlies> FireFliesScenes;
		List<CanvasStage> Stages;
		List<CanvasAnimatedControl> Canvases;

		Stack<Particle> PStack;

		AppBarButton FeedbackBtn;
		AppBarButton NewsBtn;
		Storyboard NewsStory;

		int NumStars = 0;

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
			GridFlowDir.RegisterPropertyChangedCallback( TagProperty, UpdateCanvas );

			Canvases = new List<CanvasAnimatedControl>() { Stage1, Stage2, Stage3, Stage4 };
			StarBoxes = new List<Grid>() { StarBox1, StarBox2, StarBox3, StarBox4 };
			DescTexts = new List<TextBlock> { Desc1, Desc2, Desc3, Desc4 };

			NumStars = Canvases.Count();

			NTimer.Instance.Start();

			PStack = new Stack<Particle>();

			int l = MainStage.Instance.IsPhone ? 100 : 500;

			for ( int i = 0; i < l; i++ )
				PStack.Push( new Particle() );

			Stages = new List<CanvasStage>( NumStars );
			FireFliesScenes = new List<FireFlies>( NumStars );

			for ( int i = 0; i < NumStars; i++ )
			{
				StarBoxes[ i ].RenderTransform = new TranslateTransform();

				CanvasStage CS = new CanvasStage( Canvases[ i ] );

				TheOrb LoadingTrails = new TheOrb( PStack, i % 2 == 0 );
				FireFlies Scene = new FireFlies( PStack );
				CS.Add( Scene );
				CS.Add( LoadingTrails );

				FireFliesScenes.Add( Scene );
				Stages.Add( CS );
			}

			LayoutRoot.ViewChanged += LayoutRoot_ViewChanged;

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

			MessageBus.SendUI( typeof( wenku8.System.ActionCenter ), AppKeys.PM_CHECK_TILES );
		}

		private void LayoutRoot_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			float CurrOffset = ( float ) LayoutRoot.VerticalOffset;
			FireFliesScenes?.ForEach( x => x.WindBlow( CurrOffset - PrevOffset ) );
			PrevOffset = CurrOffset;
		}

		private async void LoadContents()
		{
			IList<ActiveItem> Items = await Loader.NextPage( 4 );

			int i = 0;
			XRegistry SSettings = new XRegistry( "<sp />", FileLinks.ROOT_SETTING + FileLinks.LAYOUT_STAFFPICKS );
			foreach ( ActiveItem Item in Items )
			{
				var j = Stages[ i ].Remove( typeof( TheOrb ) );

				StarBoxes[ i ].DataContext = Item;
				StarBoxes[ i ].PointerEntered += SuperGiants_Hover;
				StarBoxes[ i ].PointerReleased += SuperGiants_Hover;
				StarBoxes[ i ].PointerPressed += SuperGiants_PointerPressed;
				StarBoxes[ i ].PointerCaptureLost += SuperGiants_PointerExited;
				StarBoxes[ i ].PointerExited += SuperGiants_PointerExited;
				StarBoxes[ i ].Tapped += SuperGiants_Tapped;

				// Set the bg context
				BgContext ItemContext = new BgContext( SSettings, "STAFF_PICKS" )
				{
					Book = await ItemProcessor.GetBookFromId( Item.Payload )
				};
				ItemContext.SetBackground( "Preset" );

				HyperBanner Banner = new HyperBanner( Item, ItemContext );
				Banner.Bind( LayoutRoot );

				Banner.TextSpeed = 0.005f * NTimer.RFloat();
				Banner.TextRotation = 6.2832f * NTimer.RFloat();

				SetBannerLayout( i, Banner );

				Stages[ i ].Insert( 0, Banner );

				i++;
			}
		}

		public void Dispose()
		{
			Stages.ForEach( x => x.Dispose() );
			Canvases.ForEach( x => x.RemoveFromVisualTree() );
		}

		private int CanvasIndex( object sender )
		{
			Grid Banner = ( Grid ) sender;
			return StarBoxes.IndexOf( Banner );
		}

		private void SuperGiants_Hover( object sender, PointerRoutedEventArgs e )
		{
			Stages[ CanvasIndex( sender ) ].GetScenes<HyperBanner>().FirstOrDefault()?.Hover();
		}

		private void SuperGiants_PointerPressed( object sender, PointerRoutedEventArgs e )
		{
			Stages[ CanvasIndex( sender ) ].GetScenes<HyperBanner>().FirstOrDefault()?.Focus();
		}

		private void SuperGiants_PointerExited( object sender, PointerRoutedEventArgs e )
		{
			Stages[ CanvasIndex( sender ) ].GetScenes<HyperBanner>().FirstOrDefault()?.Blur();
		}

		private void UpdateCanvas( DependencyObject sender, DependencyProperty dp )
		{
			for ( int i = 0; i < NumStars; i++ )
			{
				HyperBanner Banner = Stages[ i ].GetScenes<HyperBanner>().FirstOrDefault();
				if ( Banner == null ) continue;
				SetBannerLayout( i, Banner );
			}
		}

		private void SetBannerLayout( int i, HyperBanner Banner )
		{
			bool HyperBannerCenter = "V".Equals( GridFlowDir.Tag );
			if ( HyperBannerCenter )
			{
				Banner.Align = HorizontalAlignment.Center;
				Banner.SideLen = 280;
			}
			else
			{
				Banner.Align = i % 2 == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
				Banner.SideLen = 360;
			}
		}

		private void SuperGiants_Tapped( object sender, TappedRoutedEventArgs e )
		{
			ControlFrame.Instance.StopReacting();
			Stages[ CanvasIndex( sender ) ].GetScenes<HyperBanner>().FirstOrDefault()?.Click();

			NameValue<Func<Page>> Handler = PageProcessor.GetPageHandler( StarBoxes[ CanvasIndex( sender ) ].DataContext );
			ControlFrame.Instance.NavigateTo( Handler.Name, Handler.Value );
		}

		#region Anima
		Storyboard StarBoxStory = new Storyboard();

		public async Task EnterAnima()
		{
			StarBoxDescend();

			await Task.Delay( 1000 );
		}

		public async Task ExitAnima()
		{
			StarBoxVanish();

			foreach ( CanvasStage Stg in Stages )
			{
				var j = Stg.Remove( typeof( TheOrb ) );
			}

			await Task.Delay( 1000 );
		}

		private void StarBoxVanish()
		{
			StarBoxStory.Stop();
			StarBoxStory.Children.Clear();

			int i = 0;
			foreach ( Grid StarBox in StarBoxes.Reverse<Grid>() )
			{
				int Delay = i * 100;

				SimpleStory.DoubleAnimation( StarBoxStory, StarBox, "Opacity", 1, 0, 350, Delay, Easings.EaseInCubic );
				SimpleStory.DoubleAnimation( StarBoxStory, StarBox.RenderTransform, "Y", 0, 30, 350, Delay, Easings.EaseInCubic );
				i++;
			}

			StarBoxStory.Begin();
		}

		private void StarBoxDescend()
		{
			StarBoxStory.Stop();
			StarBoxStory.Children.Clear();

			int i = 0;
			foreach ( Grid StarBox in StarBoxes )
			{
				int Delay = i * 100;

				SimpleStory.DoubleAnimation( StarBoxStory, StarBox, "Opacity", 0, 1, 350, Delay );
				SimpleStory.DoubleAnimation( StarBoxStory, StarBox.RenderTransform, "Y", 30, 0, 350, Delay );
				i++;
			}

			StarBoxStory.Begin();
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