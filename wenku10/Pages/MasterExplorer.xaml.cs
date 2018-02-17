using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

using Net.Astropenguin.Loaders;

using GR.DataSources;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Section;

namespace wenku10.Pages
{
	public sealed partial class MasterExplorer : Page, ICmdControls, INavPage
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; private set; }
		public bool MajorNav { get; private set; } = true;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private TreeList NavTree;
		private TreeItem ZoneVS;

		public MasterExplorer()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public void SoftOpen()
		{
			ExplorerView.Refresh();
		}

		public void SoftClose() { }

		public void NavigateToViewSource( GRViewSource Payload )
		{
			NavTree.Open( Payload );
			if( NavTree.Contains( Payload ) )
			{
				MasterNav.SelectedItem = Payload;
				OpenView( Payload );
			}
		}

		public void NavigateToViewSource( Type TViewSource, Action<GRViewSource> VSAction )
		{
			GRViewSource VS = SearchViewSource( NavTree, TViewSource );
			if ( VS != null )
			{
				NavigateToViewSource( VS );
				VSAction?.Invoke( VS );
			}
		}

		private GRViewSource SearchViewSource( IEnumerable<TreeItem> NTree, Type TViewSource )
		{
			foreach ( TreeItem Nav in NTree )
			{
				if ( Nav.GetType() == TViewSource && Nav is GRViewSource GVS )
				{
					return GVS;
				}
				else if ( Nav.Children.Any() )
				{
					GRViewSource K = SearchViewSource( Nav.Children, TViewSource );
					if ( K != null )
					{
						return K;
					}
				}
			}

			return null;
		}

		internal void NavigateToZone( ZoneSpider ZS )
		{
			ZSViewSource ViewSource = ZoneVS.Children.Where( x => x is ZSViewSource ).Cast<ZSViewSource>().FirstOrDefault( x => x.ZS == ZS );
			if( ViewSource != null )
			{
				NavigateToViewSource( ViewSource );
			}
		}

		private void SetTemplate()
		{
			StringResources stx = new StringResources( "NavigationTitles", "AppBar", "AppResources" );

			TreeItem MyLibrary = new TreeItem( stx.Text( "MyLibrary" ) )
			{
				Children = new TreeItem[]
				{
					new BookDisplayVS( stx.Text( "AllRecords" ), typeof( BookDisplayData ) ),
					new BookSpiderVS( stx.Text( "BookSpider", "AppResources" ) ),
					new TextDocVS( stx.Text( "LocalDocuments", "AppBar" ) ),
				}
			};

			List<TreeItem> Nav = new List<TreeItem>()
			{
				MyLibrary,
				new BookDisplayVS( stx.Text( "History" ), typeof( HistoryData ) ),
				new ONSViewSource( stx.Text( "OnlineScriptDir", "AppBar" ), typeof( ONSDisplayData ) ),
			};

			// Get Zone Entries
			ZoneVS = new TreeItem( stx.Text( "Zones" ) );
			List<TreeItem> Zones = new List<TreeItem>();

			ZSManagerVS ManageZones = new ZSManagerVS( stx.Text( "ZoneSpider", "AppResources" ) );
			ManageZones.ZSMData.ZoneOpened += ( s, x ) => ZoneVS.AddChild( new ZSViewSource( x.Name, ( ZoneSpider ) x ) );
			Zones.Add( ManageZones );

			Zones.AddRange( GR.Resources.Shared.ExpZones );

			ZoneVS.Children = Zones;

			Nav.Add( ZoneVS );

			NavTree = new TreeList( Nav );
			MasterNav.ItemsSource = NavTree;

			// Initialize ZoneSpiders
			ManageZones.ZSMData.StructTable();
			ManageZones.ZSMData.Reload();
		}

		private void MasterNav_ItemClick( object sender, ItemClickEventArgs e )
		{
			TreeItem Nav = ( TreeItem ) e.ClickedItem;
			if ( Nav is GRViewSource ViewSource )
			{
				OpenView( ViewSource );
			}

			if ( Nav.Children.Any() )
			{
				NavTree.Toggle( Nav );
			}
		}

		private async void OpenView( GRViewSource ViewSource )
		{
			await ExplorerView.View( ViewSource );
			LoadingMessage.DataContext = ViewSource;
			ViewSourceCommand( ( ViewSource as IExtViewSource )?.Extension );
		}

		private PageExtension PageExt;

		private bool _NoCommands;
		private bool _MajorNav;

		private IList<ICommandBarElement> _MajorControls;
		private IList<ICommandBarElement> _Major2ndControls;
		private IList<ICommandBarElement> _MinorControls;

		private void ViewSourceCommand( PageExtension Ext )
		{

			// Unload Existing Page Extension
			if ( PageExt != null )
			{
				if ( PageExt is ICmdControls OExt )
				{
					MajorControls = _MajorControls;
					Major2ndControls = _Major2ndControls;
					MinorControls = _MinorControls;
					NoCommands = _NoCommands;
					MajorNav = _MajorNav;

					OExt.ControlChanged -= ExtCmd_ControlChanged;
				}
				PageExt.Unload();
				PageExt = null;
			}

			if( Ext == null )
			{
				ControlChanged?.Invoke( this );
				return;
			}

			Ext.Initialize( this );

			if ( Ext is ICmdControls ExtCmd )
			{
				_MajorControls = MajorControls;
				_Major2ndControls = Major2ndControls;
				_MinorControls = MinorControls;
				_NoCommands = NoCommands;
				_MajorNav = MajorNav;

				MajorControls = ExtCmd.MajorControls;
				Major2ndControls = ExtCmd.Major2ndControls;
				MinorControls = ExtCmd.MinorControls;
				NoCommands = ExtCmd.NoCommands;
				MajorNav = ExtCmd.MajorNav;
				ExtCmd.ControlChanged += ExtCmd_ControlChanged;
			}

			ControlChanged?.Invoke( this );
			PageExt = Ext;
		}

		private void ExtCmd_ControlChanged( object sender )
		{
			ControlChanged?.Invoke( this );
		}

	}
}