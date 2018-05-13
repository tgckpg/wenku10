using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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
using Net.Astropenguin.Messaging;
using Net.Astropenguin.Helpers;

using GR.Resources;
using GR.Storage;
using GR.Settings;

namespace wenku10
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		public static readonly string ID = typeof( App ).Name;

		internal static global::GR.GSystem.ViewControl ViewControl;
		internal static KeyboardControl KeyboardControl;

		private DateTime AppStartTime = DateTime.Now;

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			UnhandledException += App_UnhandledException;
			this.BootstrapApplication();

			Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
				Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
				Microsoft.ApplicationInsights.WindowsCollectors.Session );
			this.InitializeComponent();
			this.Suspending += OnSuspending;
		}

		private void App_UnhandledException( object sender, UnhandledExceptionEventArgs e )
		{
			try
			{
				Exception ex = e.Exception;

				// If app crashed within 30 seconds, this might be a start up crash.
				// Bring user into console mode
				if ( DateTime.Now.Subtract( AppStartTime ).TotalSeconds < 30 )
				{
					GR.Config.Properties.CONSOLE_MODE = true;
					GR.Config.Properties.LAST_ERROR = ex.Message + "\n" + ex.StackTrace;
				}

				if ( NetLog.Enabled && !NetLog.Ended )
				{
					Logger.Log( ID, ex.Message, LogType.ERROR );
					Logger.Log( ID, ex.StackTrace, LogType.ERROR );
					NetLog.FireEndSignal( ex );
					e.Handled = true;
				}
			}
			catch { }
		}

		private void BootstrapApplication()
		{
			new global::GR.GSystem.Bootstrap().Start();
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched( LaunchActivatedEventArgs e )
		{
#if DEBUG
			if ( global::System.Diagnostics.Debugger.IsAttached )
			{
				// this.DebugSettings.EnableFrameRateCounter = true;
			}
#endif

			if ( e.PrelaunchActivated )
			{
				if ( Shared.Storage == null )
				{
					Shared.Storage = new GeneralStorage();
					Net.Astropenguin.IO.XRegistry.AStorage = Shared.Storage;
				}

				Shared.Storage.CacheFileStatus();
				return;
			}

			Frame RootFrame = ActivateRootFrame();

			if ( MainStage.Instance == null )
			{
				Pages.ControlFrame.LaunchArgs = e.Arguments;
				RootFrame.Navigate( typeof( MainStage ) );
			}
			else if ( !string.IsNullOrEmpty( e.Arguments ) )
			{
				MessageBus.SendUI( GetType(), AppKeys.SYS_2ND_TILE_LAUNCH, e.Arguments );
			}

			Window.Current.Activate();
		}

		protected override void OnFileActivated( FileActivatedEventArgs e )
		{
			Frame RootFrame = ActivateRootFrame();

			if ( MainStage.Instance == null )
			{
				Pages.ControlFrame.LaunchArgs = e;
				RootFrame.Navigate( typeof( MainStage ) );
			}
			else
			{
				MessageBus.SendUI( GetType(), AppKeys.SYS_FILE_LAUNCH, e );
			}

			Window.Current.Activate();
		}

		private Frame ActivateRootFrame()
		{
			if ( !( Window.Current.Content is Frame RootFrame ) )
			{
				RootFrame = new Frame();

				RootFrame.NavigationFailed += OnNavigationFailed;

				// Place the frame in the current Window
				Window.Current.Content = RootFrame;
			}

			return RootFrame;
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		void OnNavigationFailed( object sender, NavigationFailedEventArgs e )
		{
			throw new Exception( "Failed to load Page " + e.SourcePageType.FullName );
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private void OnSuspending( object sender, SuspendingEventArgs e )
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			//TODO: Save application state and stop any background activity
			deferral.Complete();
		}
	}
}