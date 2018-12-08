using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
	sealed partial class EditProcListLoader : Page
	{
		public static readonly string ID = typeof( EditProcListLoader ).Name;

		private GrimoireListLoader EditTarget;

		public EditProcListLoader()
		{
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			if ( e.Parameter is GrimoireListLoader EditTarget )
			{
				this.EditTarget = EditTarget;
			}
		}

		private void SetPattern( object sender, RoutedEventArgs e )
		{
			TextBox Input = sender as TextBox;
			EditTarget.ItemPattern = Input.Text;
		}

		private void SetFormat( object sender, RoutedEventArgs e )
		{
			TextBox Input = sender as TextBox;
			EditTarget.ItemParam = Input.Text;
		}

		private void SetBanner( object sender, RoutedEventArgs e )
		{
			TextBox Input = sender as TextBox;
			EditTarget.BannerPath = Input.Text;
		}

		private void SetZoneName( object sender, RoutedEventArgs e )
		{
			TextBox Input = sender as TextBox;
			EditTarget.ZoneName = Input.Text;
		}

	}
}