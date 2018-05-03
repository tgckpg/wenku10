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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using GR.CompositeElement;
using GR.DataSources;
using GR.Effects;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Section;

namespace wenku10.Pages
{
	public sealed partial class MasterExplorer : Page, ICmdControls, INavPage, IAnimaPage, IBackStackInterceptor
	{
		private enum NavigationType : byte { VIEW_SOURCE = 1, PAGE = 2 }
		private enum NavState : byte { OPENED = 1, CLOSED = 2 }

		private struct BackStackHistroy
		{
			public NavigationType NavType;
			public GRViewSource ViewSource;
		}

#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; private set; }
		public bool MajorNav { get; private set; } = false;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		public bool CanGoBack => ( PreferredState == NavState.CLOSED && NavState.CLOSED == MasterState ) || VSHistory.Any();
		public Action<object> Update_CanGoBack { get; set; }

		public Grid MainContainer => MainElements;

		NavState PreferredState => ( NavState ) Convert.ToByte( NavSensor.Tag );
		NavState MasterState => ( NavState ) Convert.ToByte( MasterNav.Tag );
		NavState CurrState;

		private TreeList NavTree;
		private TreeItem ZoneVS;

		private Stack<BackStackHistroy> VSHistory;
		private BackStackHistroy BHBuffer;

		public MasterExplorer()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public void SoftOpen( bool NavForward )
		{
			if ( !NavForward && VSHistory.Any() )
			{
				BHBuffer = VSHistory.Pop();
			}

			if( GRShortcuts.Visibility == Visibility.Visible )
			{
				GRShortcuts.LoadWidgets();
			}
			else
			{
				ExplorerView.Refresh();
			}
		}

		public void SoftClose( bool NavForward )
		{
			if ( NavForward )
			{
				VSHistory.Push( BHBuffer );
				BHBuffer = new BackStackHistroy() { NavType = NavigationType.PAGE, ViewSource = BHBuffer.ViewSource };
			}
		}

		public void NavigateToViewSource( GRViewSource Payload ) => NavigateToViewSource( Payload, true );

		public void NavigateToDataSource( Type TDataSource, Action<GRViewSource> VSAction = null )
		{
			GRViewSource VS = SearchDataSource( NavTree, TDataSource );
			if ( VS != null )
			{
				NavigateToViewSource( VS );
				VSAction?.Invoke( VS );
			}
		}

		private GRViewSource SearchDataSource( IEnumerable<TreeItem> NTree, Type TDataSource )
		{
			foreach ( TreeItem Nav in NTree )
			{
				if ( Nav is GRViewSource GVS && GVS.DataSourceType == TDataSource )
				{
					return GVS;
				}
				else if ( Nav.Children.Any() )
				{
					GRViewSource K = SearchDataSource( Nav.Children, TDataSource );
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
			if ( ViewSource != null )
			{
				NavigateToViewSource( ViewSource );
			}
		}

		private void SetTemplate()
		{
			LoadingMessage.DataContext = new GR.GSystem.PageExtOperations();

			StringResources stx = StringResources.Load( "NavigationTitles", "AppBar", "AppResources" );
			VSHistory = new Stack<BackStackHistroy>();

			TreeItem MyLibrary = new TreeItem( stx.Text( "MyLibrary" ) )
			{
				Children = new TreeItem[]
				{
					new BookDisplayVS( stx.Text( "AllRecords" ), typeof( BookDisplayData ) ),
					new FTSViewSource( stx.Text( "FullTextSearch" ), typeof( FTSDisplayData ) ),
					new BookSpiderVS( stx.Text( "BookSpider", "AppResources" ) ),
					new TextDocVS( stx.Text( "LocalDocuments", "AppBar" ) ),
				}
			};

			List<TreeItem> Nav = new List<TreeItem>()
			{
				new GRHome( stx.Text( "Home" ), GRShortcuts ),
				MyLibrary,
				new BookDisplayVS( stx.Text( "History" ), typeof( HistoryData ) ),
				new ONSViewSource( stx.Text( "OnlineScriptDir", "AppBar" ), typeof( ONSDisplayData ) ),
			};

			// Get Zone Entries
			ZoneVS = new TreeItem( stx.Text( "Zones" ) );
			List<TreeItem> Zones = new List<TreeItem>();

			ZSManagerVS ManageZones = new ZSManagerVS( stx.Text( "ZoneSpider", "AppResources" ) );
			ManageZones.ZSMData.ZoneOpened += ( s, x ) => ZoneVS.AddChild( new ZSViewSource( x.Name, ( ZoneSpider ) x ) );
			ManageZones.ZSMData.ZoneRemoved += ( s, _ZS ) =>
			{
				ZoneSpider ZS = ( ZoneSpider ) _ZS;

				TreeItem ZVS = ZoneVS.Children
					.Where( x => x is ZSViewSource )
					.FirstOrDefault( x => ( ( ZSViewSource ) x ).ZS == ZS );

				Worker.UIInvoke( () => NavTree.Remove( ZVS ) );
				ZoneVS.RemoveChild( ZVS );
			};
			Zones.Add( ManageZones );

			Zones.AddRange( GR.Resources.Shared.ExpZones );

			ZoneVS.Children = Zones;

			Nav.Add( ZoneVS );

			NavTree = new TreeList( Nav );
			MasterNav.ItemsSource = NavTree;

			// Initialize ZoneSpiders
			ManageZones.ZSMData.StructTable();
			ManageZones.ZSMData.Reload();

			MasterNav.RegisterPropertyChangedCallback( TagProperty, TagChanged );
			NavXTrans.FillBehavior = FillBehavior.HoldEnd;

			InitMasterNav();

			// Get all available widgets
			List<GRViewSource> GWidgets = new List<GRViewSource>();
			ScanWidgets( NavTree, GWidgets );
			GRShortcuts.RegisterWidgets( GWidgets );

			OpenHome( ( GRHome ) Nav[ 0 ] );
		}

		private void ScanWidgets( IEnumerable<TreeItem> Items, List<GRViewSource> GVS )
		{
			GVS.AddRange( Items.Where( x =>
			{
				if ( x.Children.Any() )
					ScanWidgets( x.Children, GVS );
				return x is IGSWidget;
			} ).Cast<GRViewSource>() );
		}

		public async Task<bool> GoBack()
		{
			await Task.Delay( 0 );

			if ( PreferredState == NavState.CLOSED && NavState.OPENED == MasterState )
			{
				MasterNav.Tag = NavState.CLOSED;
				return true;
			}

			if ( VSHistory.Any() )
			{
				BackStackHistroy BH = VSHistory.Pop();

				while ( VSHistory.Any() && BH.ViewSource == BHBuffer.ViewSource )
					BH = VSHistory.Pop();

				BHBuffer = BH;

				if ( BH.NavType == NavigationType.VIEW_SOURCE )
				{
					NavigateToViewSource( BH.ViewSource, false );
					return true;
				}
				else if ( BH.NavType == NavigationType.PAGE && VSHistory.Any() )
				{
					// Peek the previous VS
					BH = VSHistory.Peek();
					NavigateToViewSource( BH.ViewSource, false );
				}
			}

			return false;
		}

		TranslateTransform StateTrans;
		Storyboard NavXTrans = new Storyboard();

		private void TagChanged( DependencyObject sender, DependencyProperty dp )
		{
			ToggleMasterNav( MasterState );
		}

		private void ToggleMasterNav( NavState NState )
		{
			if ( CurrState == NState || StateTrans == null )
				return;

			CurrState = NState;

			bool StateOpened = ( NState == NavState.OPENED );

			if ( MainStage.Instance.IsPhone )
			{
				NavSensor.Visibility = StateOpened ? Visibility.Visible : Visibility.Collapsed;
			}
			else
			{
				NavSensor.Visibility = StateOpened ? Visibility.Collapsed : Visibility.Visible;
			}

			double dX = StateTrans.X;

			NavXTrans.Stop();
			NavXTrans.Children.Clear();

			if ( StateOpened )
			{
				SimpleStory.DoubleAnimation( NavXTrans, StateTrans, "X", dX, 0, 500, 0, Easings.EaseOutQuintic );
			}
			else
			{
				SimpleStory.DoubleAnimation( NavXTrans, StateTrans, "X", dX, -MasterNav.Width + 3, 500, 0, Easings.EaseOutQuintic );
			}

			NavXTrans.Begin();
		}

		private void InitMasterNav()
		{
			StateTrans = ( TranslateTransform ) MasterNav.RenderTransform;
			StateTrans.X = ( PreferredState == NavState.CLOSED ) ? -MasterNav.Width : 0;

			if ( MainStage.Instance.IsPhone )
			{
				NavSensor.Tapped += ( s, e ) => MasterNav.Tag = NavState.CLOSED;
				NavSensor.HorizontalAlignment = HorizontalAlignment.Stretch;
				NavSensor.Width = double.NaN;

				AppBarButton ToggleNav = UIAliases.CreateAppBarBtn( Symbol.OpenPane, "Toggle Pane" );
				ToggleNav.Click += ( s, e ) => MasterNav.Tag = ( NavState.OPENED == MasterState ) ? NavState.CLOSED : NavState.OPENED;
				MajorControls = new ICommandBarElement[] { ToggleNav };
				ControlChanged?.Invoke( this );
			}
			else
			{
				MasterNav.PointerExited += ( s, e ) => MasterNav.Tag = PreferredState;
				NavSensor.PointerEntered += ( s, e ) => MasterNav.Tag = NavState.OPENED;
				NavSensor.HorizontalAlignment = HorizontalAlignment.Left;
				NavSensor.Width = 80;
			}

			ToggleMasterNav( PreferredState );
		}

		private void MasterNav_ItemClick( object sender, ItemClickEventArgs e )
		{
			TreeItem Nav = ( TreeItem ) e.ClickedItem;

			OpenTreeItem( Nav, true );

			if ( Nav.Children.Any() )
			{
				NavTree.Toggle( Nav );
			}
		}

		private void NavigateToViewSource( GRViewSource Payload, bool AddToQueue )
		{
			NavTree.Open( Payload );
			if ( NavTree.Contains( Payload ) )
			{
				MasterNav.SelectedItem = Payload;
				OpenTreeItem( Payload, AddToQueue );
			}
		}

		private void OpenTreeItem( TreeItem Nav, bool AddToQueue )
		{
			NavTree.Where( x => x != Nav ).ExecEach( x => x.IsActive = false );

			if ( !Nav.IsActive )
			{
				if ( Nav is GRViewSource ViewSource )
				{
					Nav.IsActive = true;
					OpenView( ViewSource );
					if ( AddToQueue )
						AddVSQueue( ViewSource );
				}
				else if ( Nav is GRHighlights HS )
				{
					Nav.IsActive = true;
					OpenHighlights( HS );
				}
				else if ( Nav is GRHome GH )
				{
					OpenHome( GH );
				}
			}
		}

		private async void OpenView( GRViewSource ViewSource )
		{
			if ( CloseAllViews( out int AnimaInt ) )
			{
				await Task.Delay( AnimaInt );
			}

			await ExplorerView.View( ViewSource );

			TransitionDisplay.SetState( ExplorerView, TransitionState.Active );
			ViewSourceCommand( ( ViewSource as IExtViewSource )?.Extension );
		}

		private async void OpenHighlights( GRHighlights ViewSource )
		{
			if ( CloseAllViews( out int AnimaInt ) )
			{
				await Task.Delay( AnimaInt );
			}

			GHighlights.View( ViewSource.Loader );

			ViewSourceCommand( ( ViewSource as IExtViewSource )?.Extension );

			GHighlights.Visibility = Visibility.Visible;
			await GHighlights.EnterAnima();
		}

		private async void OpenHome( GRHome GRH )
		{
			if ( CloseAllViews( out int AnimaInt ) )
			{
				await Task.Delay( AnimaInt );
			}

			GRShortcuts.LoadWidgets();

			ViewSourceCommand( ( GRH as IExtViewSource )?.Extension );

			GRShortcuts.Visibility = Visibility.Visible;
			await GRShortcuts.EnterAnima();
		}

		private bool CloseAllViews( out int AnimaInt )
		{
			AnimaInt = 0;

			if ( PreferredState != MasterState )
			{
				MasterNav.Tag = PreferredState;
				AnimaInt = 250;
			}

			if ( TransitionDisplay.GetState( ExplorerView ) == TransitionState.Active )
			{
				TransitionDisplay.SetState( ExplorerView, TransitionState.Inactive );
				AnimaInt = 350;
			}

			if ( GRShortcuts.Visibility == Visibility.Visible )
			{
				Worker.UIInvoke( async () =>
				{
					await GRShortcuts.ExitAnima();
					GRShortcuts.Dispose();
					GRShortcuts.Visibility = Visibility.Collapsed;
				} );
				AnimaInt = 550;
			}

			if ( GHighlights.Visibility == Visibility.Visible )
			{
				Worker.UIInvoke( async () =>
				{
					await GHighlights.ExitAnima();
					GHighlights.Dispose();
					GHighlights.Visibility = Visibility.Collapsed;
				} );

				AnimaInt = 900;
			}

			return 0 < AnimaInt;
		}

		private PageExtension PageExt;

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

					OExt.ControlChanged -= ExtCmd_ControlChanged;
				}
				PageExt.Unload();
				PageExt = null;
			}

			if ( Ext == null )
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

				MajorControls = ExtCmd.MajorControls;
				Major2ndControls = ExtCmd.Major2ndControls;
				MinorControls = ExtCmd.MinorControls;

				if ( ExtCmd.MajorNav )
				{
					if ( MajorControls != null && MajorControls.Any() )
					{
						if ( _MajorControls != null && _MajorControls.Any() )
						{
							MajorControls = MajorControls
								.Concat( new ICommandBarElement[] { new AppBarSeparator() } )
								.Concat( _MajorControls )
								.ToArray();
						}
					}
					else
					{
						MajorControls = _MajorControls;
					}
				}

				if ( Major2ndControls != null && Major2ndControls.Any() )
				{
					if ( _Major2ndControls != null && _Major2ndControls.Any() )
					{
						Major2ndControls = Major2ndControls
							.Concat( new ICommandBarElement[] { new AppBarSeparator() } )
							.Concat( _Major2ndControls )
							.ToArray();
					}
				}
				else
				{
					Major2ndControls = _Major2ndControls;
				}

				ExtCmd.ControlChanged += ExtCmd_ControlChanged;
			}

			ControlChanged?.Invoke( this );
			PageExt = Ext;
		}

		private void ExtCmd_ControlChanged( object sender )
		{
			ControlChanged?.Invoke( this );
		}

		private void AddVSQueue( GRViewSource Payload )
		{
			BackStackHistroy BSH = new BackStackHistroy() { ViewSource = Payload, NavType = NavigationType.VIEW_SOURCE };
			if ( !( default( BackStackHistroy ).Equals( BHBuffer ) || BSH.Equals( BHBuffer ) ) )
			{
				VSHistory.Push( BHBuffer );
				Update_CanGoBack?.Invoke( this );
			}
			BHBuffer = BSH;
		}

		Storyboard AnimaStory = new Storyboard();
		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1, 350 );

			AnimaStory.Begin();

			if ( GHighlights.Visibility == Visibility.Visible )
			{
				await GHighlights.EnterAnima();
			}
			else
			{
				await Task.Delay( 500 );
			}
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			if ( GHighlights.Visibility == Visibility.Visible )
			{
				SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 600, Easings.EaseInCubic );
				AnimaStory.Begin();
				await GHighlights.ExitAnima();
			}
			else
			{
				SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 0, Easings.EaseInCubic );
				AnimaStory.Begin();
				await Task.Delay( 500 );
			}
		}

		private class GRHome : TreeItem, IExtViewSource
		{
			private PageExtension _Extension;
			private Explorer.GShortcuts GRShortcuts;

			public PageExtension Extension => _Extension ?? ( _Extension = new GR.PageExtensions.WidgetsHomePageExt( GRShortcuts ) );

			public GRHome( string Name, Explorer.GShortcuts GRShortcuts )
				: base( Name )
			{
				this.GRShortcuts = GRShortcuts;
			}
		}
	}
}