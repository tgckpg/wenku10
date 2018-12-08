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

using GFlow.Pages;
using GR.Model.Interfaces;

namespace wenku10.Pages
{
	public sealed partial class ProcPanelWrapper : Page, INavPage
	{
		private ProcPanelWrapper()
		{
			this.InitializeComponent();
		}

		public ProcPanelWrapper( object Param )
			: this()
		{
			LayoutRoot.Navigate( typeof( GFEditor ), Param );
		}

		public void SoftOpen( bool NavForward ) { }

		public void SoftClose( bool NavForward )
		{
			( ( GFEditor ) LayoutRoot.Content ).Dispose();
		}

	}
}