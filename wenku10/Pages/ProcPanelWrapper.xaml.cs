using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

using GFlow.Pages;
using GR.Model.Interfaces;

namespace wenku10.Pages
{
	public sealed partial class ProcPanelWrapper : Page, INavPage, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands => true;
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private ProcPanelWrapper()
		{
			this.InitializeComponent();
		}

		public ProcPanelWrapper( object Param )
			: this()
		{
			if ( Param is string Location )
			{
				if ( !Location.Contains( ':' ) )
					Location = "ms-appdata:///local/" + Location;

				OpenFile( Location );
			}
			else
			{
				LayoutRoot.Navigate( typeof( GFEditor ), Param );
			}
		}

		public void SoftOpen( bool NavForward ) { }
		public void SoftClose( bool NavForward ) => ( ( GFEditor ) LayoutRoot.Content ).Dispose();

		private async void OpenFile( string Location )
		{
			LayoutRoot.Navigate( typeof( GFEditor ), await StorageFile.GetFileFromApplicationUriAsync( new Uri( Location ) ) );
		}
	}
}