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

				StopRange.Value = ACS.StopRange;
				AccTest.Accelerate( 0 );
				AccTest.StopRange( ACS.StopRange );

				ACS.StartCallibrate();

				// Update by slider
				if ( !ACS.Available )
				{
					AccelerReadings.Visibility = Visibility.Visible;
					Stage.Draw += Stage_Draw;
				}
			}
		}

		public void EndCallibration()
		{
			ACS.Delta = ODelta;
			ACS.EndCallibration();
			if( Stage != null )
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

		private void StopRange_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
		{
			AccTest.StopRange( ( float ) StopRange.Value );
		}

		private void StopRange_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			ACS.StopRange = ( float ) StopRange.Value;
			AccTest.StopRange( ACS.StopRange );
			GRConfig.ContentReader.AccelerScroll.StopRange = ACS.StopRange;
		}

	}
}