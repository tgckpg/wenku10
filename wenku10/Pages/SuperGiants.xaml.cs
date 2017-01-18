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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Model.Pages;
using wenku8.Resources;
using wenku8.Storage;

namespace wenku10.Pages
{
    using Scenes;

    sealed partial class SuperGiants : Page, IAnimaPage, ICmdControls
    {
#pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get { return true; } }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get; private set; }

        List<Grid> StarBoxes;
        List<FireFlies> FireFliesScenes;
        List<CanvasStage> Stages;
        List<FloatyButton> Stars;
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

        private void FloatyButton_Loaded( object sender, RoutedEventArgs e )
        {
            FloatyButton Floaty = ( ( FloatyButton ) sender );

            Floaty.BindTimer( NTimer.Instance );
            Floaty.TextSpeed = NTimer.RandDouble( -2, 2 );
        }

        private void SetTemplate()
        {
            InitAppBar();
            Canvases = new List<CanvasAnimatedControl>() { Stage1, Stage2, Stage3, Stage4 };
            StarBoxes = new List<Grid>() { StarBox1H, StarBox2H, StarBox3H, StarBox4H };
            Stars = new List<FloatyButton>() { Star1, Star2, Star3, Star4 };

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
                Stars[ i ].Visibility = Visibility.Collapsed;
                StarBoxes[ i ].RenderTransform = new TranslateTransform();

                CanvasStage CS = new CanvasStage( Canvases[ i ] );

                TheOrb LoadingTrails = new TheOrb( PStack, i % 2 == 0 );
                FireFlies Scene = new FireFlies( PStack );

                CS.Add( Scene );
                CS.Add( LoadingTrails );

                FireFliesScenes.Add( Scene );
                Stages.Add( CS );

                Stars[ i ].StateComplete += SuperGiants_StateComplete;
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

            PinManagerActions();
            GetAnnouncements();
        }

        private async void SuperGiants_StateComplete( object sender, FloatyState State )
        {
            if ( State == FloatyState.EXPLODE )
            {
                ( ( FloatyButton ) sender ).Visibility = Visibility.Collapsed;
                await Task.Delay( 2000 );
                ( ( FloatyButton ) sender ).Visibility = Visibility.Visible;
            }
        }

        private async void PinManagerActions()
        {
            PinManager PM = new PinManager();
            await PM.SyncSettings();
            if ( PM.Policy == PinPolicy.DO_NOTHING ) return;

            ActiveItem[] MissingPins = PM.GetLocalPins().Where(
                x => !Windows.UI.StartScreen.SecondaryTile.Exists( x.Payload )
            ).ToArray();

            if ( 0 < MissingPins.Length )
            {
                switch ( PM.Policy )
                {
                    case PinPolicy.ASK:
                        bool RemoveRecord = true;
                        StringResources stx = new StringResources( "Message", "AppBar", "ContextMenu" );
                        await Popups.ShowDialog( UIAliases.CreateDialog(
                            string.Format( stx.Str( "MissingPins" ), MissingPins.Length )
                            , () => RemoveRecord = false
                            , stx.Text( "PinToStart", "ContextMenu" ), stx.Text( "PinPolicy_RemoveMissing", "AppBar" )
                        ) );

                        if ( RemoveRecord ) goto case PinPolicy.REMOVE_MISSING;
                        goto case PinPolicy.PIN_MISSING;

                    case PinPolicy.PIN_MISSING:
                        foreach ( ActiveItem Item in MissingPins )
                        {
                            BookItem Book = await ItemProcessor.GetBookFromId( Item.Desc );
                            if ( Book != null )
                            {
                                BookLoader Loader = new BookLoader( b =>
                                {
                                    if ( b == null ) return;
                                    var j = PageProcessor.PinToStart( b );
                                } );
                                Loader.Load( Book, true );
                            }
                        }
                        break;

                    case PinPolicy.REMOVE_MISSING:
                        PM.RemovePin( MissingPins.Remap( x => x.Desc ) );
                        break;
                }
            }

        }

        private float PrevOffset = 0;

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
            foreach ( ActiveItem Item in Items )
            {
                var j = Stages[ i ].Remove( typeof( TheOrb ) );

                Stars[ i ].Visibility = Visibility.Visible;
                Stars[ i ].PointerReleased += SuperGiants_PointerReleased;

                StarBoxes[ i ].DataContext = Item;
                i++;
            }
        }

        private void SuperGiants_PointerReleased( object sender, PointerRoutedEventArgs e )
        {
            ControlFrame.Instance.StopReacting();

            FloatyButton Btn = ( FloatyButton ) sender;
            int i = Stars.IndexOf( Btn );
            Stages[ i ].Add( new TheOrb( PStack, i % 2 == 0 ) );

            NameValue<Func<Page>> Handler = PageProcessor.GetPageHandler( StarBoxes[ i ].DataContext );
            ControlFrame.Instance.NavigateTo( Handler.Name, Handler.Value );
        }

        #region Anima
        Storyboard StarBoxStory = new Storyboard();

        public async Task EnterAnima()
        {
            StarBoxDescend();
            StarsDescend();

            await Task.Delay( 1000 );
        }

        public async Task ExitAnima()
        {
            StarsExplode();
            StarBoxVanish();

            foreach( CanvasStage Stg in Stages )
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
            foreach( Grid StarBox in StarBoxes.Reverse<Grid>() )
            {
                int Delay = i * 100;

                SimpleStory.DoubleAnimation( StarBoxStory, StarBox, "Opacity", 1, 0, 350, Delay );
                SimpleStory.DoubleAnimation( StarBoxStory, StarBox.RenderTransform, "Y", 0, 30, 350, Delay );
                SimpleStory.ObjectAnimation( StarBoxStory, StarBox, "Visibility", Visibility.Visible, Visibility.Collapsed, 0, 350 + Delay );
                i++;
            }

            StarBoxStory.Begin();
        }

        private void StarBoxDescend()
        {
            StarBoxStory.Stop();
            StarBoxStory.Children.Clear();

            int i = 0;
            foreach( Grid StarBox in StarBoxes )
            {
                int Delay = i * 100;

                SimpleStory.DoubleAnimation( StarBoxStory, StarBox, "Opacity", 0, 1, 350, Delay );
                SimpleStory.DoubleAnimation( StarBoxStory, StarBox.RenderTransform, "Y", 30, 0, 350, Delay );
                SimpleStory.ObjectAnimation( StarBoxStory, StarBox, "Visibility", Visibility.Collapsed, Visibility.Visible, 0, Delay );
                i++;
            }

            StarBoxStory.Begin();
        }

        private async void StarsExplode()
        {
            for ( int i = 0; i < NumStars; i++ )
            {
                var j = Stars[ i ].Vanquish();
                await Task.Delay( 100 );
            }
        }

        private async void StarsDescend()
        {
            for( int i = 0; i < NumStars; i ++ )
            {
                var j = Stars[ i ].Descend();
                await Task.Delay( 100 );
            }
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