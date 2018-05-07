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
				Brake.Value = ACS.Brake;
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
			ACS.Delta = ODelta;
			ACS.EndCallibration();
			if ( Stage != null )
			{
				AccTest.Dispose();

				Stage.RemoveFromVisualTree();
				Stage = null;
			}
		}

		private void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args )
		{
			var j = Dispatcher.RunAsync( Windows.UI.Core.CoreDispatcherPriority.High, () =>
				ACS.Delta( ( float ) AccelerReadings.Value )
			);
		}

		private void SetTemplate()
		{
			CvsStage = new CanvasStage( Stage );

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
			GRConfig.ContentReader.AccelerScroll.Brake = ACS.Brake;
		}

		private void AccelerMultiplier_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			ACS.AccelerMultiplier = ( float ) AccelerMultiplier.Value;
		}

		private void AccelerMultiplier_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.AccelerMultiplier = ( float ) AccelerMultiplier.Value;
			GRConfig.ContentReader.AccelerScroll.AccelerMultiplier = ACS.AccelerMultiplier;
		}

		private void TerminalVelocity_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			ACS.TerminalVelocity = ( float ) TerminalVelocity.Value;
		}

		private void TerminalVelocity_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.TerminalVelocity = ( float ) TerminalVelocity.Value;
			GRConfig.ContentReader.AccelerScroll.TerminalVelocity = ACS.TerminalVelocity;
		}

		private void BrakeOffset_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.BrakeOffset = ( float ) BrakeOffset.Value;
			GRConfig.ContentReader.AccelerScroll.BrakeOffset = ACS.BrakeOffset;
		}

		private void ForceBrake_Checked( object sender, RoutedEventArgs e ) => ACS.ForceBrake = true;
		private void ForceBrake_Unchecked( object sender, RoutedEventArgs e ) => ACS.ForceBrake = false;

	}
}