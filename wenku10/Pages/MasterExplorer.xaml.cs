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
	using Sharers;

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

		public void SetTemplate()
		{
			StringResources stx = new StringResources( "NavigationTitles", "AppBar" );
			List<TreeItem> Nav = new List<TreeItem>()
			{
				new BookDisplayVS( stx.Text( "MyLibrary" ), typeof( BookDisplayData ) ),
				new BookDisplayVS( stx.Text( "History" ), typeof( HistoryData ) ),
				new ONSViewSource( stx.Text( "OnlineScriptDir", "AppBar" ), typeof( ONSDisplayData ) ),
			};

			// Get Zone Entries
			ZoneVS = new TreeItem( "Zones" );
			List<TreeItem> Zones = new List<TreeItem>();

			Zones.AddRange( GR.Resources.Shared.ExpZones );

			ZSContext ZoneListCont = new ZSContext()
			{
				ZoneEntry = ( x ) =>
				{
					ZoneVS.Children = new ZSViewSource[] { new ZSViewSource( x.ZoneId, x ) };
				}
			};

			ZoneVS.Children = Zones;

			Nav.Add( ZoneVS );

			NavTree = new TreeList( Nav );
			MasterNav.ItemsSource = NavTree;

			ZoneListCont.ScanZones();
		}

		private async void MasterNav_ItemClick( object sender, ItemClickEventArgs e )
		{
			TreeItem Nav = ( TreeItem ) e.ClickedItem;
			if ( Nav is GRViewSource ViewSource )
			{
				await ExplorerView.View( ViewSource );
				ViewSourceCommand( ( ViewSource as IExtViewSource )?.Extension );
			}

			if ( Nav.Children?.Any() == true )
			{
				NavTree.Toggle( Nav );
			}
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
					OExt.ControlChanged -= ExtCmd_ControlChanged;
				}
				PageExt.Unload();
				PageExt = null;
			}

			if( Ext == null )
				return;

			Ext.Extend( this );

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

				ControlChanged?.Invoke( this );
			}

			PageExt = Ext;
		}

		private void ExtCmd_ControlChanged( object sender )
		{
			ControlChanged?.Invoke( this );
		}

	}
}