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

		public MasterExplorer()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private struct GRViewSource
		{
			public string Name { get; set; }

			public Type DataSourceType { get; set; }

			private GRDataSource _DataSource;
			public GRDataSource DataSource => _DataSource ?? ( _DataSource = ( GRDataSource ) Activator.CreateInstance( DataSourceType ) );
		}

		public void SoftOpen() { }
		public void SoftClose() { }

		public void SetTemplate()
		{
			List<GRViewSource> Nav = new List<GRViewSource>()
			{
				new GRViewSource() { Name = "My Library", DataSourceType = typeof( BookDisplayData ) },
				new GRViewSource() { Name = "History", DataSourceType = typeof( HistoryData ) }
			};

			MasterNav.ItemsSource = Nav;
		}

		private async void MasterNav_ItemClick( object sender, ItemClickEventArgs e )
		{
			GRViewSource Nav = ( GRViewSource ) e.ClickedItem;
			await ExplorerView.LoadDataSource( Nav.DataSource );
		}
	}
}