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

using GR.DataSources;
using GR.Model.Interfaces;

namespace wenku10.Pages.Settings.Advanced
{
	public sealed partial class TRTableEditor : Page, INavPage
	{
		public TRTableEditor()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private ConvViewSource CurrentVS;

		public void SoftClose( bool NavForward )
		{
			LayoutRoot.Children.Remove( TableView );
		}

		public void SoftOpen( bool NavForward )
		{
		}

		private async void SetTemplate()
		{
			CurrentVS = new ConvViewSource( "ntw_ps2t" );
			await TableView.View( CurrentVS );
		}

	}
}