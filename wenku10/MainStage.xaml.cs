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

using GR.Config;

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

			if ( Properties.FIRST_TIME_RUN )
			{
				GR.Database.ContextManager.Migrate();
				RootFrame.Navigate( typeof( Pages.Settings.FirstTimeSettings ) );
				return;
			}

			if ( Properties.CONSOLE_MODE )
			{
				RootFrame.Navigate( typeof( Pages.Settings.ConsoleMode ) );
				return;
			}

			if ( new GR.MigrationOps.MigrationManager().ShouldMigrate )
			{
				RootFrame.Navigate( typeof( Pages.Settings.BackupAndRestore ) );
				return;
			}

			if ( Properties.RESTORE_MODE )
			{
				Properties.RESTORE_MODE = false;
				RootFrame.Navigate( typeof( Pages.Settings.BackupAndRestore ) );
				return;
			}

#if DEBUG
			// GR.Database.ContextManager.Migrate();
#endif

			Background = new SolidColorBrush( GRConfig.Theme.BgColorMajor );
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
			App.AppKeyboard = new KeyboardControl( Window.Current.CoreWindow );

			// Full Screen Ctrl + F
			App.AppKeyboard.RegisterCombination(
				( x ) => App.ViewControl.ToggleFullScreen()
				, Windows.System.VirtualKey.Control
				, Windows.System.VirtualKey.F
			);

			// Escape / Backspace = Back
			App.AppKeyboard.RegisterCombination( Escape, Windows.System.VirtualKey.Escape );
			App.AppKeyboard.RegisterCombination( Escape, Windows.System.VirtualKey.Back );
		}

		private void Escape( KeyCombinationEventArgs e )
		{
			// Always Close the dialog first
			if ( Net.Astropenguin.Helpers.Popups.CloseDialog() ) return;

			NavigationHandler.MasterNavigationHandler( RootFrame, null );
		}

	}
}