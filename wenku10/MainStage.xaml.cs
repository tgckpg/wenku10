using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
		public bool IsPhone { get; private set; }

		public Grid BadgeBlock { get { return PleaseWait; } }

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

			if ( global::GR.Config.Properties.FIRST_TIME_RUN )
			{
				RootFrame.Navigate( typeof( Pages.Settings.FirstTimeSettings ) );
				return;
			}

			RootFrame.Navigate( typeof( Pages.ControlFrame ) );
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
			}
			else
			{
				Logger.Log( ID, "No status bar... not a phone?" );
			}

			// Acquire Background Priviledge
			Tasks.BackgroundProcessor.AcquireBackgroundPriviledge();

			// Register Navigation Handler to BackRequested event
			SystemNavigationManager.GetForCurrentView().BackRequested += NavigationHandler.MasterNavigationHandler;

			// Initialize the Controls
			App.ViewControl = new global::GR.GSystem.ViewControl();
			App.KeyboardControl = new KeyboardControl( Window.Current.CoreWindow );

			// Full Screen Ctrl + F
			App.KeyboardControl.RegisterCombination(
				( x ) => App.ViewControl.ToggleFullScreen()
				, Windows.System.VirtualKey.Control
				, Windows.System.VirtualKey.F
			);

			// Escape / Backspace = Back
			App.KeyboardControl.RegisterCombination( Escape, Windows.System.VirtualKey.Escape );
			App.KeyboardControl.RegisterCombination( Escape, Windows.System.VirtualKey.Back );
		}

		private static readonly Type[] SpecialElement = new Type[]
		{
			typeof( TextBox ), typeof( RichEditBox ), typeof( PasswordBox )
		};

		private void Escape( KeyCombinationEventArgs e )
		{
			object o = FocusManager.GetFocusedElement();
			if ( o != null && SpecialElement.Contains( o.GetType() ) ) return;

			// Always Close the dialog first
			if ( Net.Astropenguin.Helpers.Popups.CloseDialog() ) return;

			NavigationHandler.MasterNavigationHandler( RootFrame, null );
		}

	}
}