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

using GR.GFlow;

namespace wenku10.Pages.Dialogs.GFlow
{
	sealed partial class EditProcMark : Page
	{
		public static readonly string ID = typeof( EditProcMark ).Name;

		GrimoireMarker EditTarget;

		public EditProcMark()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			if ( e.Parameter is GrimoireMarker EditTarget )
			{
				this.EditTarget = EditTarget;
				LayoutRoot.DataContext = EditTarget;
			}
		}

		private void SetProp( object sender, RoutedEventArgs e )
		{
			TextBox Input = sender as TextBox;
			EditTarget.SetProp( Input.Tag as string, Input.Text.Trim() );
		}

		private void ToggleVAsync( object sender, RoutedEventArgs e ) => EditTarget.VolAsync = !EditTarget.VolAsync;
		private void ToggleEAsync( object sender, RoutedEventArgs e ) => EditTarget.EpAsync = !EditTarget.EpAsync;
	}
}