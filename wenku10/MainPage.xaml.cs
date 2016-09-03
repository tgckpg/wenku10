using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.UI;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;
using wenku8.Effects.P2DFlow.Reapers;
using wenku8.Effects.P2DFlow.Spawners;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Section;
using wenku8.Model.ListItem;
using wenku8.Model.Loaders;
using wenku8.Storage;

namespace wenku10
{
    public sealed partial class MainPage : Page, IDisposable
    {
        public static readonly string ID = typeof( MainPage ).Name;

        private IFavSection FS;
        private Canvas Marquee;
        private bool Init = false;

        public Action ClosePoupFrame { get; private set; }

        ~MainPage()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                NavigationHandler.OnNavigatedBack -= OnBackRequested;
                AdvDMStat.DataContext = null;
                CustomSection.DataContext = null;
                NavigationSection.DataContext = null;
                SettingsButton.DataContext = null;
                FavSectionView.DataContext = null;
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            NTimer.Instance.Stop();

            if ( e.NavigationMode == NavigationMode.Back ) Start();
            if ( e.NavigationMode != NavigationMode.New ) return;

            string Param = e.Parameter.ToString();
            if ( !string.IsNullOrEmpty( Param ) )
            {
                Logger.Log( ID, string.Format( "Param({0}) is present", Param ), LogType.INFO );
                GotoBookInfoView( e.Parameter.ToString() );
                return;
            }

            Start();
        }

        private void GotoBookInfoView( string id )
        {
            Logger.Log( ID, string.Format( "Navigate to BookInfoView, id: {0}", id ), LogType.INFO );
            StringResources stx = new StringResources( "LoadingMessage" );
            LoadingMask.Text = stx.Str( "ProgressIndicator_Message" );

            Action A = async () =>
            {
                TaskCompletionSource<BookItem> TCS = new TaskCompletionSource<BookItem>();
                new BookLoader( ( x ) => TCS.SetResult( x ) ).Load(
                    X.Instance<BookItem>( XProto.BookItemEx, id )
                    , true
                );

                BookItem b = await TCS.Task;

                if( b == null )
                {
                    LoadingMask.Text = "Failed to download data";
                    return;
                }

                LoadingMask.Text = b.Title;
                await Frame.Dispatcher.RunIdleAsync( x => Frame.Navigate( typeof( Pages.BookInfoView ), id ) );
            };
            LoadingMask.HandleForward( Frame, A );
        }

        public MainPage()
        {
            InitializeComponent();
            new global::wenku8.System.Bootstrap().Level2();

            X.Call<object>( XProto.Verification, "VersionCheck" );
            NavigationHandler.OnNavigatedBack += OnBackRequested;
            // Check for unittest
#if DEBUG
            new global::wenku8.UnitTest();
#endif
        }

        private void Start()
        {
            NTimer.Instance.Start();

            if ( Init )
            {
                FS?.Reload();
                return;
            }

            SetBackground();
            Init = true;

            if ( global::wenku8.Config.Properties.LOG_LEVEL == "DEBUG" )
            {
                AdvDMStat.Visibility = Visibility.Visible;
                AdvDMStat.DataContext = App.RuntimeTransfer;
            }

            INavSelections ns = X.Instance<INavSelections>( XProto.NavSelections );
            NavigationSection.DataContext = ns;

            if( ns.MainPage_Settings.IsStaffPicksEnabled )
            {
                StaffPicksSection.Visibility = Visibility.Visible;
                ISectionItem sp = X.Instance<ISectionItem>( XProto.StaffPicksSection );
                StaffPicksSection.DataContext = sp;
                sp.Load( X.Call<XKey[]>( XProto.WRequest, "GetStaffPicks" ) );
            }

            if( ns.MainPage_Settings.IsCustomSectionEnabled )
            {
                CustomSection.Visibility = Visibility.Visible;
                CustomSection.DataContext = ns.CustomSection();
            }

            DebugLog.Visibility = global::wenku8.Config.Properties.ENABLE_SYSTEM_LOG ? Visibility.Visible : Visibility.Collapsed;

            ns.Load();

            SettingsButton.DataContext = new LoginStatus();

            WaitUIs();
        }

        private void WaitUIs()
        {
            // After Login Code
            FS = X.Instance<IFavSection>( XProto.FavSection );

            MemberSections();

            if( global::wenku8.Config.Properties.ENABLE_ONEDRIVE )
            {
                OneDriveResync.Visibility = Visibility.Visible;
            }

            OneDriveResync.SetSync( ReSync );

            FS.PropertyChanged += FS_PropertyChanged;
        }

        private void FS_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == "IsLoading" )
            {
                TransitionDisplay.SetState( BgCover, FS.IsLoading ? TransitionState.Inactive : TransitionState.Active );
                if( !FS.IsLoading )
                {
                    FavSectionView.DataContext = FS;
                }
            }
        }

        private async Task ReSync()
        {
            FS.IsLoading = true;

            // OneDriveSync
            OneDriveSync.Instance = new OneDriveSync();
            await OneDriveSync.Instance.Authenticate();

            if( OneDriveSync.Instance.Authenticated )
            {
                await new BookStorage().SyncSettings();
                FS.Reload();
            }

            FS.IsLoading = false;
        }

        private async void MemberSections()
        {
            bool LoggedIn = await TryLogin();

            FS.Load();
        }

        private void ClosePaneButton( object sender, RoutedEventArgs e )
        {
            MainSplitPane.ClosePane();
        }

        private void BookClicked( object sender, ItemClickEventArgs e )
        {
            BookClicked( e.ClickedItem as BookInfoItem );
        }

        private async void BookClicked( object sender, StarCanvas.ItemClickedEventArgs e )
        {
            await Task.Delay( 500 );
            await Dispatcher.RunIdleAsync( ( x ) =>
            {
                BookClicked( e.ClickedItem as BookInfoItem );
            } );
        }

        private void BookClicked( BookInfoItem BookItem )
        {
            Logger.Log( ID, string.Format( "Clicked items is {0}, mode {1}", BookItem.Payload, BookItem.Mode ) );

            BookItem b = X.Instance<BookItem>( XProto.BookItemEx, BookItem.Payload );

            switch ( BookItem.Mode )
            {
                case SectionMode.InfoPane:
                    // Pane Loading = True
                    b.XSetProp( "Mode", X.Const<string>( XProto.WProtocols, "ACTION_BOOK_META" ) );

                    BookLoader loader = new BookLoader( UpdatePane );
                    loader.Load( b, true );
                    loader.LoadIntro( b, true );
                    RollOutInfoPane();
                    break;
                case SectionMode.DirectNavigation:
                    GotoBookInfoView( b.Id );
                    break;
            }
        }

        private void RollOutInfoPane()
        {
            PaneGridState.State = ControlState.Foreatii;
            PaneBackIcon.Visibility = Visibility.Collapsed;
            PaneLoadBubble.IsActive = true;

            StopMarquee();
            StartMarquee();
            MainSplitPane.OpenPane();
        }

        private async void StartMarquee()
        {
            if ( Marquee == null || MainSplitPane.State == PaneStates.Closed )
            {
                StopMarquee();
                return;
            }

            TextBlock T = Marquee.Children.First() as TextBlock;
            if ( T.ActualWidth < Marquee.ActualWidth && MarqueeStory != null )
            {
                StopMarquee();
                return;
            }


            await Task.Delay( 1000 );
            MakeMarquee();
        }

        private void StopMarquee()
        {
            if ( MarqueeStory == null ) return;
            MarqueeStory.Stop();
            MarqueeStory.Children.Clear();
        }

        private Storyboard MarqueeStory;
        private void MakeMarquee()
        {
            TextBlock T = Marquee.Children.First() as TextBlock;
            if ( MarqueeStory.GetCurrentState() != ClockState.Stopped ) return;

            MarqueeStory.Children.Clear();
            DoubleAnimationUsingKeyFrames d = new DoubleAnimationUsingKeyFrames();

            LinearDoubleKeyFrame still = new LinearDoubleKeyFrame();
            still.Value = 0;
            still.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromSeconds( 0 ) );

            LinearDoubleKeyFrame still_still = new LinearDoubleKeyFrame();
            still_still.Value = 0;
            still_still.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromSeconds( 2 ) );

            LinearDoubleKeyFrame move = new LinearDoubleKeyFrame();
            move.Value = -( T.ActualWidth + Marquee.ActualWidth );
            move.KeyTime = KeyTime.FromTimeSpan( TimeSpan.FromSeconds( 8 ) );

            d.Duration = new Duration( TimeSpan.FromSeconds( 8 ) );
            d.RepeatBehavior = RepeatBehavior.Forever;

            d.KeyFrames.Add( still );
            d.KeyFrames.Add( still_still );
            d.KeyFrames.Add( move );

            Storyboard.SetTarget( d, T );
            Storyboard.SetTargetProperty( d, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)" );
            MarqueeStory.Children.Add( d );

            MarqueeStory.Begin();
        }

        private void MarqueeComplete( object sender, object e )
        {
            StartMarquee();
        }

        private void UpdatePane( BookItem obj )
        {
            PaneLoadBubble.IsActive = false;
            PaneBackIcon.Visibility = Visibility.Visible;
            MainSplitPane.PaneGrid.DataContext = obj;
            PaneGridState.State = ControlState.Reovia;
        }

        private void LoginOrInfo( object sender, RoutedEventArgs e )
        {
            LoginStatus LS = ( sender as FrameworkElement ).DataContext as LoginStatus;
            PopupPage.DataContext = SettingsButton.DataContext;
            LS.PopupLoginOrInfo();
        }

        private void Navigate_Tapped( object sender, TappedRoutedEventArgs e )
        {
            object Tag = ( ( FrameworkElement ) sender ).Tag;

            if ( Tag != null )
            {
                GotoBookInfoView( Tag.ToString() );
            }
        }

        private async Task<bool> TryLogin()
        {
            if ( !X.Exists ) return false; 

            TaskCompletionSource<bool> IsLoggedIn = new TaskCompletionSource<bool>();

            IMember Member = X.Singleton<IMember>( XProto.Member );
            if ( Member.WillLogin )
            {
                TypedEventHandler<object, MemberStatus> A = null;
                A = ( s, e ) =>
                {
                    Member.OnStatusChanged -= A;

                    if ( e == MemberStatus.RE_LOGIN_NEEDED ) ReLogin();

                    IsLoggedIn.SetResult( Member.IsLoggedIn );
                };

                Member.OnStatusChanged += A;
            }
            else
            {
                IsLoggedIn.SetResult( Member.IsLoggedIn );
            }

            return await IsLoggedIn.Task;
        }

        private async void ReLogin()
        {
            await Task.Delay( 3000 );
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                ( ( LoginStatus ) SettingsButton.DataContext ).PopupLoginOrInfo();
            } );
        }

        private async void GotoSettings( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Message", "Settings", "AppBar" );

            bool Go = false;
            MessageDialog Msg = new MessageDialog( stx.Text( "Preface", "Settings" ), stx.Text( "Settings", "AppBar" ) );

            Msg.Commands.Add( new UICommand( stx.Str( "Yes" ), x => Go = true ) );
            Msg.Commands.Add( new UICommand( stx.Str( "No", "Message" ) ) );

            await Popups.ShowDialog( Msg );

            if ( Go )
            {
                Dispose();
                Frame.Navigate( typeof( Pages.Settings.MainSettings ) );
            }
        }

        private void GotoSearch( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( Pages.Search ) );
        }

        private void ShowAbout( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( Pages.About ) );
        }

        private void ShowLog( object sender, RoutedEventArgs e )
        {
            Frame.Navigate( typeof( Pages.DebugLog ) );
        }

        private void GotoNavigation( object sender, ItemClickEventArgs e )
        {
            SubtleUpdateItem s = e.ClickedItem as SubtleUpdateItem;
            if ( s.Nav == typeof( Pages.CategorizedList ) )
            {
                PopupPage.DataContext = new PopupList( s, Frame );
            }
            else
            {
                Frame.Navigate( s.Nav, s );
            }
        }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            if( !Frame.CanGoBack )
            {
                if( MainSplitPane.State == PaneStates.Opened )
                {
                    MainSplitPane.ClosePane();
                    e.Handled = true;
                    return;
                }

                BackAgainToExit( e );
            }
        }

        private void BackAgainToExit( XBackRequestedEventArgs e )
        {
            if( TransitionDisplay.GetState( BackAgainMessage ) == TransitionState.Active )
            {
                Windows.ApplicationModel.Core.CoreApplication.Exit();
                return;
            }

            e.Handled = true;
            ShowBackMessage();
        }
        private async void ShowBackMessage()
        {
            TransitionDisplay.SetState( BackAgainMessage, TransitionState.Active );
            await Task.Delay( 2000 );
            TransitionDisplay.SetState( BackAgainMessage, TransitionState.Inactive );
        }

        private void ShowFavContext( object sender, RightTappedRoutedEventArgs e )
        {
            FrameworkElement Elem = sender as FrameworkElement;
            FlyoutBase.ShowAttachedFlyout( Elem );
            FS.CurrentItem = Elem.DataContext as FavItem;
        }

        private void FavContext( object sender, RoutedEventArgs e )
        {
            MenuFlyoutItem Item = sender as MenuFlyoutItem;
            switch( Item.Tag.ToString() )
            {
                case "Pin": FS.C_Pin(); break;
                case "RSync": FS.C_RSync(); break;
                case "AutoCache": FS.C_AutoCache(); break;
                case "Delete": FS.C_Delete(); break;
            }
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            FS.SearchTerm = sender.Text.Trim();
        }

        private void OrderFavItems( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count < 1 ) return;
            FS.Reorder( ( sender as ComboBox ).SelectedIndex );
        }

        private void BookTitleLoaded( object sender, RoutedEventArgs e )
        {
            Marquee = sender as Canvas;
            if ( MarqueeStory == null )
            {
                MarqueeStory = new Storyboard();
                MarqueeStory.Completed += MarqueeComplete;
            }
        }

        private void ReloadCustomSection( object sender, RoutedEventArgs e )
        {
            Button b = sender as Button;
            CustomSection.Width = CustomSection.ActualWidth;

            INavSelections NS = NavigationSection.DataContext as INavSelections;
            IPaneInfoSection PS = NS.CustomSection();

            CustomSection.DataContext = PS;
        }

        private void FloatyButton_Loaded( object sender, RoutedEventArgs e )
        {
            FloatyButton Floaty = ( ( FloatyButton ) sender );
            Floaty.BindTimer( NTimer.Instance );

            Floaty.TextSpeed = NTimer.RandDouble( -2, 2 );
        }

        #region Dynamic Background
        private PFSimulator PFSim = new PFSimulator();

#if DEBUG
        private bool ShowWireFrame = false;
#endif

        private TextureLoader Texture;

        private const int Texture_Glitter = 1;
        private const int Texture_Circle = 2;

        private Vector4 ThemeTint;

        private Wind ScrollWind = new Wind();

        private void SetBackground()
        {
            PFSim.Create( MainStage.Instance.IsPhone ? 25 : 50 );

            Texture = new TextureLoader();

            Windows.UI.Color C = wenku8.Config.Properties.APPEARENCE_THEME_HORIZONTAL_RIBBON_COLOR;
            ThemeTint = new Vector4( C.R * 0.0039f, C.G * 0.0039f, C.B * 0.0039f, C.A * 0.0039f );

            Stage.GameLoopStarting += Stage_GameLoopStarting;
            Stage.GameLoopStopped += Stage_GameLoopStopped;

            Stage.SizeChanged += Stage_SizeChanged;
            HomeHub.ViewChanged += HomeHub_ViewChanged;
        }

        private void Stage_GameLoopStopped( ICanvasAnimatedControl sender, object args )
        {
            Stage.Draw -= Stage_Draw;
        }

        private void Stage_GameLoopStarting( ICanvasAnimatedControl sender, object args )
        {
            Stage.Draw += Stage_Draw;
        }

        private float PrevOffset = 0;

        private void HomeHub_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
        {
            float CurrOffset = ( float ) HomeHub.RefSV.HorizontalOffset;
            ScrollWind.Strength = Vector2.Clamp( Vector2.One * ( CurrOffset - PrevOffset ), -3 * Vector2.One, 3 * Vector2.One ).X;
            PrevOffset = CurrOffset;
        }

        private void Stage_CreateResources( CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args )
        {
            args.TrackAsyncAction( LoadTextures( sender ).AsAsyncAction() );
        }

        private async Task LoadTextures( CanvasAnimatedControl CC )
        {
            await Texture.Load( CC, Texture_Glitter, "Assets/glitter.dds" );
            await Texture.Load( CC, Texture_Circle, "Assets/circle.dds" );
        }

        private void Stage_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            lock ( PFSim )
            {
                Size s = e.NewSize;
                PFSim.Reapers.Clear();
                PFSim.Reapers.Add( Age.Instance );
                PFSim.Reapers.Add( new Boundary( new Rect( -0.1 * s.Width, -0.1 * s.Height, s.Width * 1.2, s.Height * 1.2 ) ) );

                float SW = ( float ) s.Width;
                float SH = ( float ) s.Height;
                float HSW = 0.5f * SW;
                float HSH = 0.5f * SH;

                PFSim.Spawners.Clear();
                PFSim.Spawners.Add( new LinearSpawner( new Vector2( HSW, HSH ), new Vector2( HSW, HSH ), new Vector2( 10, 10 ) )
                {
                    Chaos = new Vector2( 1, 1 )
                    , otMin = 5, otMax = 10
                    , Texture = Texture_Circle
                    , SpawnTrait = PFTrait.IMMORTAL
                    , SpawnEx = ( P ) =>
                    {
                        P.Tint.M11 = ThemeTint.X;
                        P.Tint.M22 = ThemeTint.Y;
                        P.Tint.M33 = ThemeTint.Z;
                        P.Tint.M44 = ThemeTint.W * NTimer.LFloat();

                        P.mf *= NTimer.LFloat();
                        P.Scale = new Vector2( 0.5f, 0.5f ) + Vector2.One * ( NTimer.LFloat() - 0.25f );
                        P.vt.Y += 5 * NTimer.RFloat();
                    }
                } );

                ScrollWind.A = new Vector2( SW, 0 );
                ScrollWind.B = new Vector2( SW, SH );
                ScrollWind.MaxDist = SW;

                PFSim.Fields.Clear();
                PFSim.AddField( GenericForce.EARTH_GRAVITY );
                PFSim.AddField( ScrollWind );
            }
        }

        private void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args )
        {
            lock ( PFSim )
            {
                var Snapshot = PFSim.Snapshot();
                using ( CanvasDrawingSession ds = args.DrawingSession )
                using ( CanvasSpriteBatch SBatch = ds.CreateSpriteBatch() )
                {
                    while ( Snapshot.MoveNext() )
                    {
                        Particle P = Snapshot.Current;

                        float A = Vector2.Transform( new Vector2( 0, 1 ), Matrix3x2.CreateRotation( P.ttl * 0.01f ) ).X;

                        Vector4 Tint = new Vector4(
                            P.Tint.M11 + P.Tint.M21 + P.Tint.M31 + P.Tint.M41 + P.Tint.M51,
                            P.Tint.M12 + P.Tint.M22 + P.Tint.M32 + P.Tint.M42 + P.Tint.M52,
                            P.Tint.M13 + P.Tint.M23 + P.Tint.M33 + P.Tint.M43 + P.Tint.M53,
                            P.Tint.M14 + P.Tint.M24 + P.Tint.M34 + P.Tint.M44 + P.Tint.M54
                        );

                        Tint.W *= A;
                        ScrollWind.Strength *= 0.5f;

                        SBatch.Draw(
                            Texture[ P.TextureId ]
                            , P.Pos, Tint
                            , Texture.Center[ P.TextureId ], 0, P.Scale
                            , CanvasSpriteFlip.None );
                    }
#if DEBUG
                    if ( ShowWireFrame )
                    {
                        foreach ( IForceField IFF in PFSim.Fields )
                        {
                            IFF.WireFrame( ds );
                        }
                    }
#endif
                }
            }
        }
        #endregion

    }

}