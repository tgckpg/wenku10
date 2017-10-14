using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
using Net.Astropenguin.Loaders;
using Net.Astropenguin.UI;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Model.Book.Spider;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.Pages;
using wenku8.Model.Section;
using wenku8.Resources;
using wenku8.Settings;

namespace wenku10.Pages
{
	sealed partial class ZoneSpidersView : Page, ICmdControls, INavPage, IAnimaPage
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private ZoneList ZoneListContext;
		private ZoneSpider SelectedZone;

		private SecondaryIconButton OpenZoneBtn;

		public ZoneSpidersView()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public void OpenZone( HubScriptItem HSI )
		{
			var j = ZoneListContext.OpenFile( HSI.ScriptFile );
		}

		private void SetTemplate()
		{
			InitAppBar();

			ZoneListContext = new ZoneList();

			LayoutRoot.RenderTransform = new TranslateTransform();
			LayoutRoot.DataContext = ZoneListContext;
			ZoneListContext.Zones.CollectionChanged += Zones_CollectionChanged;
		}

		private void Zones_CollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
		{
			TransitionDisplay.SetState( Desc, ZoneListContext.Zones.Any() ? TransitionState.Inactive : TransitionState.Active );
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppBar" );

			OpenZoneBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.OpenFile, stx.Text( "OpenZone" ) );
			OpenZoneBtn.Click += ( s, e ) =>
			{
				if ( ZoneListContext.CurrentZone == null )
				{
					ZoneListContext.OpenFile();
				}
			};

			Major2ndControls = new ICommandBarElement[] { OpenZoneBtn };
		}

		public void SoftOpen() { NavigationHandler.InsertHandlerOnNavigatedBack( OnNavBack ); }
		public void SoftClose() { NavigationHandler.OnNavigatedBack -= OnNavBack; }

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 0, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30, 350, 0, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		private void OnNavBack( object sender, XBackRequestedEventArgs e )
		{
			if ( ZoneListContext.CurrentZone != null )
			{
				ZoneListContext.ExitZone();
				e.Handled = true;
			}
		}

		private void EditZone( object sender, RoutedEventArgs e ) { EditItem( SelectedZone ); }
		private void ResetZoneState( object sender, RoutedEventArgs e ) { SelectedZone.Reset(); }
		private void ReloadZone( object sender, RoutedEventArgs e ) { SelectedZone.Reload(); }

		private void RemoveZone( object sender, RoutedEventArgs e )
		{
			ZoneListContext.RemoveZone( SelectedZone );
			SelectedZone = null;
		}

		private void ZoneList_ItemClick( object sender, ItemClickEventArgs e )
		{
			ZoneListContext.EnterZone( ( ZoneSpider ) e.ClickedItem );
		}

		private volatile bool Hold = false;
		private Dictionary<string, SpiderBook> ProcessedItems = new Dictionary<string, SpiderBook>();

		private async void ZoneSpider_ItemClick( object sender, ItemClickEventArgs e )
		{
			if ( Hold ) return;
			Hold = true;

			BookInstruction BInst = ( BookInstruction ) e.ClickedItem;
			SpiderBook Item;
			if ( ProcessedItems.ContainsKey( BInst.Id ) )
			{
				Item = ProcessedItems[ BInst.Id ];
			}
			else
			{
				ClickedInner.DataContext = BInst;

				// "Z" to let LocalFileList know this is a Zone directory
				BInst.SetId( AppKeys.SP_ZONE_PFX + ZoneListContext.CurrentZone.ZoneId );

				Item = await SpiderBook.CreateFromZoneInst( BInst );
				ClickedSpider.DataContext = Item;

				if ( !Item.ProcessSuccess && Item.CanProcess )
				{
					await ItemProcessor.ProcessLocal( Item );
					ProcessedItems[ BInst.Id ] = Item;
				}
			}

			if ( Item.ProcessSuccess )
			{
				ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( Item.GetBook() ) );
			}

			Hold = false;
		}

		private void ShowZoneAction( object sender, RightTappedRoutedEventArgs e )
		{
			Grid G = ( Grid ) sender;
			FlyoutBase.ShowAttachedFlyout( G );

			SelectedZone = ( ZoneSpider ) G.DataContext;
		}

		private void EditItem( IMetaSpider LB )
		{
			ControlFrame.Instance.NavigateTo( PageId.PROC_PANEL, () => new ProcPanelWrapper( LB.MetaLocation ) );
		}
	}
}
