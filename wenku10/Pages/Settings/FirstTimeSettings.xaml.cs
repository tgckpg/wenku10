using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Logging;

using GR.Config;
using GR.Storage;

namespace wenku10.Pages.Settings
{
	public sealed partial class FirstTimeSettings : Page
	{
		private static readonly string ID = typeof( FirstTimeSettings ).Name;

		public FirstTimeSettings()
		{
			this.InitializeComponent();

			SetTemplate();
			Prev( false ); Next( false );
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

			if ( Frame.CanGoBack ) Frame.BackStack.Clear();
		}

		private void SetTemplate()
		{
			OneDriveToggle.IsOn = GRConfig.System.EnableOneDrive;

			// Phone should disable double tap
			GRConfig.ContentReader.DoubleTap = !MainStage.Instance.IsPhone;
		}

		private void Prev( object sender, RoutedEventArgs e ) { Prev(); }
		private void Next( object sender, RoutedEventArgs e ) { Next(); }

		private void Complete( object sender, RoutedEventArgs e )
		{
			Properties.FIRST_TIME_RUN = false;
			SetTheme();
			Windows.ApplicationModel.Core.CoreApplication.Exit();
		}

		private void Prev( bool Auto = true )
		{
			if ( Auto && 0 < MainView.SelectedIndex ) MainView.SelectedIndex--;

			if( MainView.SelectedIndex == 0 )
			{
				PrevBtn.Visibility = Visibility.Collapsed;
				CompBtn.Visibility = Visibility.Collapsed;
			}

			if( MainView.SelectedIndex < MainView.Items.Count - 1)
			{
				NextBtn.Visibility = Visibility.Visible;
			}
		}

		private void Next( bool Auto = true )
		{
			if ( Auto )
			{
				MainView.SelectedIndex++;
			}

			if ( 0 < MainView.SelectedIndex )
			{
				PrevBtn.Visibility = Visibility.Visible;
			}

			if( MainView.SelectedIndex == MainView.Items.Count - 1 )
			{
				NextBtn.Visibility = Visibility.Collapsed;
				CompBtn.Visibility = Visibility.Visible;
			}
			else
			{
				CompBtn.Visibility = Visibility.Collapsed;
			}
		}

		private async void OneDrive( object sender, RoutedEventArgs e )
		{
			if( GRConfig.System.EnableOneDrive = OneDriveToggle.IsOn )
			{
				await OneDriveSync.Instance.Authenticate();
			}
			else
			{
				await OneDriveSync.Instance.UnAuthenticate();
			}
		}

		private void SetTheme()
		{
			global::GR.Settings.Theme.ThemeSet T;
			if ( ThemeToggle.IsOn )
			{
				T = global::GR.GSystem.ThemeManager.DefaultDark();
				T.GreyShades();

				GRConfig.ContentReader.BackgroundColor = Windows.UI.Color.FromArgb( 255, 20, 20, 20 );
				GRConfig.ContentReader.FontColor = Windows.UI.Color.FromArgb( 255, 45, 77, 59 );
				GRConfig.ContentReader.TapBrushColor = Windows.UI.Color.FromArgb( 255, 138, 41, 0 );
				GRConfig.ContentReader.BgColorNav = Windows.UI.Color.FromArgb( 255, 50, 50, 50 );
				GRConfig.ContentReader.BgColorAssist = Windows.UI.Color.FromArgb( 23, 0, 0, 0 );
			}
			else
			{
				T = global::GR.GSystem.ThemeManager.DefaultLight();
				T.BlackShades();

				GRConfig.ContentReader.BackgroundColor = Windows.UI.Color.FromArgb( 180, 0, 0, 0 );
				GRConfig.ContentReader.FontColor = Windows.UI.Color.FromArgb( 255, 98, 167, 130 );
				GRConfig.ContentReader.TapBrushColor = Windows.UI.Color.FromArgb( 255, 255, 88, 9 );
				GRConfig.ContentReader.BgColorNav = Windows.UI.Color.FromArgb( 255, 81, 94, 108 );
				GRConfig.ContentReader.BgColorAssist = Windows.UI.Color.FromArgb( 23, 0, 0, 0 );
			}

			T.Apply();
		}

		private void MainView_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			Prev( false ); Next( false );
		}
	}
}