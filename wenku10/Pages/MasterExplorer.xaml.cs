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

using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using GR.Data;
using GR.DataSources;
using GR.Model.Book;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Pages;
using GR.Model.Section;

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

		private TreeList NavTree;
		private TreeItem ZoneVS;

		public MasterExplorer()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public void SoftOpen() { }
		public void SoftClose() { }

		public void SetTemplate()
		{
			StringResources stx = new StringResources( "NavigationTitles" );
			List<TreeItem> Nav = new List<TreeItem>()
			{
				new GRViewSource( stx.Text( "MyLibrary" ) ) { DataSourceType = typeof( BookDisplayData ) },
				new GRViewSource( stx.Text( "History" ) ) { DataSourceType = typeof( HistoryData ) },
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
			if ( Nav is GRViewSource )
			{
				GRViewSource ViewSource = ( GRViewSource ) Nav;
				ViewSource.ItemAction = OpenBook;

				await ExplorerView.View( ViewSource );
			}

			if ( Nav.Children?.Any() == true )
			{
				NavTree.Toggle( Nav );
			}
		}

		private void OpenBook( IGRRow Row )
		{
			if ( Row is GRRow<BookDisplay> )
			{
				BookItem BkItem = ItemProcessor.GetBookItem( ( ( GRRow<BookDisplay> ) Row ).Source.Entry );
				ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( BkItem ) );
			}
		}

	}
}