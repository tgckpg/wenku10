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
using GR.Model.Text;

using Tasks;

namespace wenku10.Pages.Settings.Advanced
{
	public sealed partial class Misc : Page
	{
		public Misc()
		{
			this.InitializeComponent();
			SyntaxPatchToggle.IsOn = Properties.MISC_TEXT_PATCH_SYNTAX;
			ChunkVolsToggle.IsOn = Properties.MISC_CHUNK_SINGLE_VOL;
#if DEBUG
			BgTaskInterval.Minimum = 15;
#else
			BgTaskInterval.Minimum = 180;
#endif
			BgTaskInterval.Maximum = 2880;
			BgTaskInterval.Value = BackgroundProcessor.Instance.TaskInterval;
			BgTaskIntvlInput.Text = BgTaskInterval.Value.ToString();
		}

		private void ToggleSynPatch( object sender, RoutedEventArgs e )
		{
			Manipulation.DoSyntaxPatch = SyntaxPatchToggle.IsOn;
			Properties.MISC_TEXT_PATCH_SYNTAX = SyntaxPatchToggle.IsOn;
		}

		private void ToggleChunkVols( object sender, RoutedEventArgs e )
		{
			Properties.MISC_CHUNK_SINGLE_VOL = ChunkVolsToggle.IsOn;
		}

		private void BgTaskInterval_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			BackgroundProcessor.Instance.UpdateTaskInterval( ( uint ) BgTaskInterval.Value );

			if( BackgroundProcessor.Instance.TaskInterval != BgTaskInterval.Value )
			{
				BgTaskInterval.Value = BackgroundProcessor.Instance.TaskInterval;
			}

			BgTaskIntvlInput.Text = BgTaskInterval.Value.ToString();
		}

		private void BgTaskIntvlInput_LostFocus( object sender, RoutedEventArgs e )
		{
			BgTaskInterval_PointerCaptureLost( sender, null );
		}

		private void UpdateBgTaskSlider( object sender, TextChangedEventArgs e )
		{
			int Val;
			if ( int.TryParse( BgTaskIntvlInput.Text, out Val ) )
			{
				BgTaskInterval.Value = Val;
			}
		}

	}
}