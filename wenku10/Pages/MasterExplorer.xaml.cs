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

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using GR.CompositeElement;
using GR.DataSources;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Section;
using GR.Effects;

namespace wenku10.Pages
{
	public sealed partial class MasterExplorer : Page, ICmdControls, INavPage
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; private set; }
		public bool MajorNav { get; private set; } = false;

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
			if ( NavTree.Contains( Payload ) )
			{
				MasterNav.SelectedItem = Payload;
				OpenView( Payload );
			}
		}

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
			StringResources stx = new StringResources( "NavigationTitles", "AppBar", "AppResources" );

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

			NavigationHandler.InsertHandlerOnNavigatedBack( OnBackRequested );
			MasterNav.RegisterPropertyChangedCallback( TagProperty, TagChanged );
			NavXTrans.FillBehavior = FillBehavior.HoldEnd;

			InitMasterNav();
		}

		private void OnBackRequested( object sender, XBackRequestedEventArgs e )
		{
			if ( PreferredState == "Closed" && "Opened" == ( string ) MasterNav.Tag )
			{
				MasterNav.Tag = "Closed";
				e.Handled = true;
			}
		}

		TranslateTransform StateTrans;
		Storyboard NavXTrans = new Storyboard();
		string PreferredState => ( string ) NavSensor.Tag;
		string CurrState;

		private void TagChanged( DependencyObject sender, DependencyProperty dp )
		{
			ToggleMasterNav( ( string ) MasterNav.GetValue( dp ) );
		}

		private void ToggleMasterNav( string NState )
		{
			if ( CurrState == NState || StateTrans == null )
				return;

			CurrState = NState;

			if( MainStage.Instance.IsPhone )
			{
				NavSensor.Visibility = ( NState == "Opened" ) ? Visibility.Visible : Visibility.Collapsed;
			}
			else
			{
				NavSensor.Visibility = ( NState == "Opened" ) ? Visibility.Collapsed : Visibility.Visible;
			}

			double dX = StateTrans.X;

			NavXTrans.Stop();
			NavXTrans.Children.Clear();

			if ( NState == "Opened" )
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
			StateTrans.X = ( PreferredState == "Closed" ) ? -MasterNav.Width : 0;

			if ( MainStage.Instance.IsPhone )
			{
				NavSensor.Tapped += ( s, e ) => MasterNav.Tag = "Closed";
				NavSensor.HorizontalAlignment = HorizontalAlignment.Stretch;
				NavSensor.Width = double.NaN;

				AppBarButton ToggleNav = UIAliases.CreateAppBarBtn( Symbol.OpenPane, "Toggle Pane" );
				ToggleNav.Click += ( s, e ) => MasterNav.Tag = ( "Opened" == ( string ) MasterNav.Tag ) ? "Closed" : "Opened";
				MajorControls = new ICommandBarElement[] { ToggleNav };
				ControlChanged?.Invoke( this );
			}
			else
			{
				MasterNav.PointerExited += ( s, e ) => MasterNav.Tag = PreferredState;
				NavSensor.PointerEntered += ( s, e ) => MasterNav.Tag = "Opened";
				NavSensor.HorizontalAlignment = HorizontalAlignment.Left;
				NavSensor.Width = 80;
			}

			ToggleMasterNav( PreferredState );
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
			int AnimaInt = 0;
			if ( PreferredState != ( string ) MasterNav.Tag )
			{
				MasterNav.Tag = PreferredState;
				AnimaInt = 250;
			}

			if ( TransitionDisplay.GetState( ExplorerView ) == TransitionState.Active )
			{
				TransitionDisplay.SetState( ExplorerView, TransitionState.Inactive );
				AnimaInt = 350;
			}

			if ( 0 < AnimaInt )
			{
				await Task.Delay( AnimaInt );
			}

			await ExplorerView.View( ViewSource );

			TransitionDisplay.SetState( ExplorerView, TransitionState.Active );
			LoadingMessage.DataContext = ViewSource;
			ViewSourceCommand( ( ViewSource as IExtViewSource )?.Extension );
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
	}
}