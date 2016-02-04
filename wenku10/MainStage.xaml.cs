using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Logging;

namespace wenku10
{
    public sealed partial class MainStage : Page
    {
        public static readonly string ID = typeof( MainStage ).Name;
        public static MainStage Instance;

        public Frame RootFrame { get { return MainView; } }
        public Canvas Canvas { get { return TransitionLayer; } }
        public Grid ObjectLayer { get { return TransitionObjectLayer; } }
        public bool IsPhone { get; private set; }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );

        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            if ( global::wenku8.Config.Properties.FIRST_TIME_RUN )
            {
                RootFrame.Navigate( typeof( Pages.Settings.FirstTimeSettings ) );
                return;
            }

            // if( Mode == null )
            {
                RootFrame.Navigate( typeof( Pages.ModeSelect ), e.Parameter );
                return;
            }

            // RootFrame.Navigate( typeof( MainPage ), e.Parameter );
        }

        public MainStage()
        {
            this.InitializeComponent();
            Instance = this;
            SetTemplate();
        }

        public void SetTemplate()
        {
            if ( IsPhone = Windows.Foundation.Metadata.ApiInformation.IsTypePresent( "Windows.UI.ViewManagement.StatusBar" ) )
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                var j = statusBar.HideAsync();
                Logger.Log( ID, "Status bar found. Guessing this is a phone." );
                global::wenku8.Effects.StarField.NumStars = 50;
            }
            else
            {
                Logger.Log( ID, "No status bar... not a phone?" );
                global::wenku8.Effects.StarField.NumStars = 100;
            }

            // Register a handler for BackRequested events and set the
            // visibility of the Back button
            SystemNavigationManager.GetForCurrentView().BackRequested += NavigationHandler.MasterNavigationHandler;
            NavigationHandler.OnNavigatedBack += OnBackRequested;

            // Initialize the Controls
            App.ViewControl = new global::wenku8.System.ViewControl();
            App.KeyboardControl = new KeyboardControl( Window.Current.CoreWindow );

            // Full Screen Ctrl + F
            App.KeyboardControl.RegisterCombination(
                ( x ) => { App.ViewControl.ToggleFullScreen(); }
                , Windows.System.VirtualKey.Control
                , Windows.System.VirtualKey.F
            );

            // Escape
            App.KeyboardControl.RegisterCombination( Escape, Windows.System.VirtualKey.Escape );
            App.KeyboardControl.RegisterCombination( Escape, Windows.System.VirtualKey.Back );

            SetGoBackButton();

            RootFrame.Navigated += OnNavigated;
        }

        private static readonly Type[] SpecialElement = new Type[]
        {
            typeof( TextBox ), typeof( RichEditBox )
        };
        private void Escape( KeyCombinationEventArgs e )
        {
            object o = FocusManager.GetFocusedElement();
            if ( o != null && SpecialElement.Contains( o.GetType() ) ) return;

            NavigationHandler.MasterNavigationHandler( RootFrame, null );
        }

        private void OnBackRequested( object sender, XBackRequestedEventArgs e )
        {
            if ( RootFrame.CanGoBack )
            {
                e.Handled = true;
                RootFrame.GoBack();
            }
        }

        private void OnNavigated( object sender, NavigationEventArgs e )
        {
            SetGoBackButton();
        }

        public void ClearNavigate( Type type, object Parameter = null )
        {
            RootFrame.Navigate( type, Parameter );
            RootFrame.BackStack.Clear();
            SetGoBackButton();
        }

        private void SetGoBackButton()
        {
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility
                = RootFrame.CanGoBack
                ? AppViewBackButtonVisibility.Visible
                : AppViewBackButtonVisibility.Collapsed
                ;
        }
    }
}
