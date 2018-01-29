using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using GR.DataSources;
using GR.Model.Interfaces;

using wenku10.Pages.Explorer;

namespace wenku10.Pages
{
	public sealed partial class MasterExplorer : Page, ICmdControls, INavPage
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		public GRDataSource BkData;

		public MasterExplorer()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public void SetTemplate()
		{
			BkData = new BookDisplayData();
			GRTableView BkDisplayView = new GRTableView( BkData );
			LayoutRoot.Children.Add( BkDisplayView );
		}

		public void SoftOpen()
		{
			BkData.Reload();
		}

		public void SoftClose() { }

	}
}