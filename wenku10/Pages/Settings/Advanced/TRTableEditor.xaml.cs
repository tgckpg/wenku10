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

namespace wenku10.Pages.Settings.Advanced
{
	public sealed partial class TRTableEditor : Page
	{
		public TRTableEditor()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private async void SetTemplate()
		{
			await TableView.View( new ConvViewSource( "ntw_ps2t" ) );
		}

	}
}