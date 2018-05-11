using Microsoft.Graphics.Canvas.UI.Xaml;
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

using GR.Config;
using GR.Config.Scopes;
using GR.GSystem;

namespace wenku10.Pages.Settings
{
	using Scenes;

	public sealed partial class CallibrateAcceler : Page
	{
		private AccelerScroll ACS;
		private Action<float> ODelta;

		private CanvasStage CvsStage;
		private AccelerTest AccTest;
		private Conf_ContentReader.Conf_AccelerScroll ACSConf;

		private float _a = 0;

		public CallibrateAcceler()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			if ( e.Parameter is AccelerScroll )
			{
				ACS = ( AccelerScroll ) e.Parameter;

				// This will ensure the SV lambda swapping triggered in ReaderContent
				ACS.Delta( 0 );

				ODelta = ACS.Delta;
				ACS.Delta = a => ODelta( AccTest.Accelerate( a ) );

				ForceBrake.IsChecked = ACS.ForceBrake;
				TrackAutoAnchor.IsChecked = ACS.TrackAutoAnchor;
				Brake.Value = ACS.Brake;
				BrakingForce.Value = ACS.BrakingForce;
				TerminalVelocity.Value = ACS.TerminalVelocity;
				BrakeOffset.Value = ACS.BrakeOffset;
				AccelerMultiplier.Value = ACS.AccelerMultiplier;
				AccTest.Accelerate( 0 );
				AccTest.Brake( ACS.BrakeOffset, ACS.Brake );

				ACS.StartCallibrate();

				// Update by slider
				if ( !ACS.Available )
				{
					AccelerReadings.Value = ACS.BrakeOffset + 0.5 * Brake.Value;
					AccelerReadings.Visibility = Visibility.Visible;
					Stage.Draw += Stage_Draw;
				}
			}
		}

		public void EndCallibration()
		{
			_a = 0;

			ACS.Delta = ODelta;
			ACS.EndCallibration();

			if ( Stage != null )
			{
				AccTest.Dispose();

				Stage.Draw -= Stage_Draw;
				Stage.Paused = true;
				Stage.RemoveFromVisualTree();
				Stage = null;
			}

			ODelta?.Invoke( 0 );
		}

		private void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args )
		{
			ACS.Delta( _a );
		}

		private void SetTemplate()
		{
			CvsStage = new CanvasStage( Stage );

			ACSConf = GRConfig.ContentReader.AccelerScroll;
			AccTest = new AccelerTest();
			CvsStage.Add( AccTest );
		}

		private void Brake_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			AccTest.Brake( ( float ) BrakeOffset.Value, ( float ) Brake.Value );
		}

		private void Brake_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.Brake = ( float ) Brake.Value;
			AccTest.Brake( ACS.BrakeOffset, ACS.Brake );
			ACSConf.Brake = ACS.Brake;
		}

		private void AccelerMultiplier_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			ACS.AccelerMultiplier = ( float ) AccelerMultiplier.Value;
		}

		private void AccelerMultiplier_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.AccelerMultiplier = ( float ) AccelerMultiplier.Value;
			ACSConf.AccelerMultiplier = ACS.AccelerMultiplier;
		}

		private void TerminalVelocity_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			ACS.TerminalVelocity = ( float ) TerminalVelocity.Value;
		}

		private void TerminalVelocity_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.TerminalVelocity = ( float ) TerminalVelocity.Value;
			ACSConf.TerminalVelocity = ACS.TerminalVelocity;
		}

		private void BrakeOffset_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.BrakeOffset = ( float ) BrakeOffset.Value;
			ACSConf.BrakeOffset = ACS.BrakeOffset;
		}

		private void ForceBrake_Checked( object sender, RoutedEventArgs e ) => ACS.ForceBrake = true;
		private void ForceBrake_Unchecked( object sender, RoutedEventArgs e ) => ACS.ForceBrake = false;

		private void BrakingForce_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			ACS.BrakingForce = ( float ) BrakingForce.Value;
		}

		private void BrakingForce_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.BrakingForce = ( float ) BrakingForce.Value;
			ACSConf.BrakingForce = ACS.BrakingForce;
		}

		private void TrackAutoAnchor_Checked( object sender, RoutedEventArgs e )
		{
			ACS.TrackAutoAnchor = true;
			ACSConf.TrackAutoAnchor = true;
		}

		private void TrackAutoAnchor_Unchecked( object sender, RoutedEventArgs e )
		{
			ACS.TrackAutoAnchor = false;
			ACSConf.TrackAutoAnchor = false;
		}

		private void AccelerReadings_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			_a = ( float ) AccelerReadings.Value;
		}

	}
}