using Microsoft.Services.Store.Engagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.UI;

using GR.Effects;
using GR.Model.Book;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Pages;
using GR.Settings;

namespace wenku10.Pages
{
	sealed partial class ControlFrame : Page
	{
		public static readonly string ID = typeof( ControlFrame ).Name;

		public CommandBar MajorCmdBar { get; private set; }
		public CommandBar MinorCmdBar { get; private set; }
		public global::GR.GSystem.MasterCommandManager CommandMgr { get; private set; }
		public global::GR.GSystem.BackStackManager BackStack { get; private set; }

		private bool InSubView { get { return TransitionDisplay.GetState( SubView ) == TransitionState.Active; } }

		public static ControlFrame Instance { get; private set; }
		public static volatile string LaunchArgs;

		private volatile bool Navigating = false;

		public ControlFrame()
		{
			Instance = this;

			InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			BackStack = new global::GR.GSystem.BackStackManager();

			MessageBus.Subscribe( this, MessageBus_OnDelivery );
			NavigationHandler.OnNavigatedBack += NavigationHandler_OnNavigatedBack;

			ApplyControlSet();
		}

		private async void MessageBus_OnDelivery( Message Mesg )
		{
			// Handles secondary tile launch on App opened
			if ( Mesg.Content == AppKeys.SYS_2ND_TILE_LAUNCH )
			{
				if ( Navigating )
				{
					ActionBlocked();
					return;
				}

				BookItem Book = await ItemProcessor.GetBookFromTileCmd( ( string ) Mesg.Payload );
				if ( Book != null )
				{
					NavigateTo( PageId.MONO_REDIRECTOR, () => new MonoRedirector(), P => ( ( MonoRedirector ) P ).InfoView( Book ) );
				}

			}
		}

		private void NavigationHandler_OnNavigatedBack( object sender, XBackRequestedEventArgs e )
		{
			if ( Navigating )
			{
				e.Handled = true;
				return;
			}

			if ( InSubView )
			{
				e.Handled = true;
				var j = CloseSubView();
			}
			else if ( BackStack.CanGoBack )
			{
				e.Handled = true;
				NavigateBack();
			}
		}

		public async void SetHomePage( string Id, Func<Page> FPage, Action<Page> PageAct = null )
		{
			// Handles secondary tile launch when App closed
			if ( !string.IsNullOrEmpty( LaunchArgs ) )
			{
				Id = PageId.BOOK_INFO_VIEW;
				BookItem Book = await ItemProcessor.GetBookFromTileCmd( LaunchArgs );
				FPage = () => new BookInfoView( Book );

				LaunchArgs = null;
			}

			MajorCmdBar.IsOpen = false;
			await NavigateToAsync( Id, FPage, PageAct );
			BackStack.Clear();
			SetBackButton( false );
		}

		public void CollapseAppBar()
		{
			MajorCmdBar.IsHitTestVisible = false;
			MinorCmdBar.IsHitTestVisible = false;
			MajorCmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
			MinorCmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;
		}

		public void StopReacting()
		{
			LayoutRoot.IsHitTestVisible = false;
			MajorCmdBar.IsHitTestVisible = false;
			MinorCmdBar.IsHitTestVisible = false;
			NavigationHandler.InsertHandlerOnNavigatedBack( PreventBack );
		}

		public void StartReacting()
		{
			LayoutRoot.IsHitTestVisible = true;
			MajorCmdBar.IsHitTestVisible = true;
			MinorCmdBar.IsHitTestVisible = true;
			NavigationHandler.OnNavigatedBack -= PreventBack;
			SetBackButton( InSubView );
		}

		public void NavigateTo( string Name, Func<Page> Target, Action<Page> Navigated = null )
		{
			var j = NavigateToAsync( Name, Target, Navigated );
		}

		public async Task NavigateToAsync( string Name, Func<Page> Target, Action<Page> Navigated = null )
		{
			// Prevent navigating with the same instance
			if ( Navigating ) return;

			if ( Name == ( string ) View.Tag )
			{
				Navigated?.Invoke( ( Page ) View.Content );
				return;
			}

			Logger.Log( ID, "NavigateTo: " + Name, LogType.INFO );

			Navigating = true;
			StopReacting();

			if ( TransitionDisplay.GetState( SubView ) == TransitionState.Active )
			{
				await CloseSubView( true );
				SubView.Content = null;
			}

			if ( View.Content != null )
			{
				if ( View.Content is IAnimaPage )
					await ( ( IAnimaPage ) View.Content ).ExitAnima();

				if ( View.Content is IBackStackInterceptor )
					( ( IBackStackInterceptor ) View.Content ).Update_CanGoBack = null;

				UnsubEvents( ( Page ) View.Content );
				( View.Content as INavPage )?.SoftClose( true );

				if ( !PageId.NonStackables.Contains( View.Tag ) )
					BackStack.Add( ( string ) View.Tag, ( Page ) View.Content );
			}

			Page P = BackStack.Get( Name );

			// We'll be navigating to a new instance
			// Remove old instances in the backstack
			if ( PageId.MonoStack.Contains( Name ) && P != null )
			{
				P = null;
				BackStack.Remove( Name );
			}

			// Let Page render silently
			View.Opacity = 0;

			LoadingScreen.State = ControlState.Reovia;
			await Task.Delay( 200 );

			View.Tag = Name;
			if ( P == null ) P = Target();

			View.Content = P;

			( P as INavPage )?.SoftOpen( true );
			await View.Dispatcher.RunIdleAsync( x =>
			{
				View.Opacity = 1;
				( View.Content as IAnimaPage )?.EnterAnima();
			} );

			SetControls( View.Content, true );

			// Do not close the loading screen if redirecting
			if ( !( P is MonoRedirector ) )
				LoadingScreen.State = ControlState.Foreatii;

			Navigating = false;
			StartReacting();

			Navigated?.Invoke( P );
		}

		private void UnsubEvents( Page P )
		{
			if ( P is ICmdControls )
			{
				( ( ICmdControls ) P ).ControlChanged -= Controls_ControlChanged;
			}
		}

		public async void SubNavigateTo( object sender, Func<Page> Target )
		{
			if ( !( View.Content == sender || SubView.Content == sender ) )
			{
				Logger.Log( ID, "Main view has been differed, not showing sub view", LogType.INFO );
				return;
			}

			if ( Navigating ) return;
			Navigating = true;

			StopReacting();
			SetBackButton( true );

			if ( SubView.Content != null )
			{
				TransitionDisplay.SetState( SubView, TransitionState.Inactive );
				await Task.Delay( 350 );

				( SubView.Content as INavPage )?.SoftClose( true );
			}

			SubView.Content = Target();

			( SubView.Content as INavPage )?.SoftOpen( true );
			SetControls( SubView.Content, true );

			TransitionDisplay.SetState( SubView, TransitionState.Active );
			await Task.Delay( 350 );

			Navigating = false;
			StartReacting();
		}

		public async Task CloseSubView()
		{
			if ( Navigating ) return;

			Navigating = true;
			StopReacting();

			await CloseSubView( false );

			Navigating = false;
			StartReacting();
		}

		public void GoBack()
		{
			var j = Dispatcher.RunIdleAsync( ( x ) =>
			{
				if ( BackStack.CanGoBack ) NavigateBack();
			} );
		}

		private async void NavigateBack()
		{
			StopReacting();

			if ( Navigating ) return;
			Navigating = true;

			if ( View.Content is IAnimaPage )
				await ( ( IAnimaPage ) View.Content ).ExitAnima();

			UnsubEvents( ( Page ) View.Content );
			( View.Content as INavPage )?.SoftClose( false );

			NameValue<Page> P = BackStack.Back();

			// Keep going back if previous page == current page
			// This happens when pages in backstack got removed
			while ( P.Value == View.Content && BackStack.CanGoBack )
				P = BackStack.Back();

			View.Tag = P.Name;
			View.Content = P.Value;

			( P.Value as INavPage )?.SoftOpen( false );
			if ( P.Value is IAnimaPage AnimaPage )
			{
				await AnimaPage.EnterAnima();
			}
			SetControls( View.Content, true );

			Navigating = false;
			StartReacting();
		}

		private async Task CloseSubView( bool Override )
		{
			// Restore Main View's Controls
			SetControls( View.Content, false );

			if ( !Override && SubView.Content is IAnimaPage )
			{
				await ( ( IAnimaPage ) SubView.Content ).ExitAnima();
				TransitionDisplay.SetState( SubView, TransitionState.Hidden );
			}
			else
			{
				TransitionDisplay.SetState( SubView, TransitionState.Inactive );
				await Task.Delay( 350 );
			}

			UnsubEvents( ( Page ) SubView.Content );
			( SubView.Content as INavPage )?.SoftClose( false );
		}

		private void SetControls( object CPage, bool RegEvent )
		{
			if ( !( CPage is ICmdControls ) )
			{
				CommandMgr.SetMajorCommands( null, true );
				CommandMgr.Set2ndCommands( null );
				MinorCmdBar.PrimaryCommands.Clear();
				return;
			}

			ICmdControls CmdPage = ( ICmdControls ) CPage;

			if ( RegEvent )
			{
				CmdPage.ControlChanged -= Controls_ControlChanged;
				CmdPage.ControlChanged += Controls_ControlChanged;
			}

			MajorCmdBar.ClosedDisplayMode = CmdPage.NoCommands ? AppBarClosedDisplayMode.Hidden : AppBarClosedDisplayMode.Compact;

			MinorCmdBar.PrimaryCommands.Clear();
			if ( CmdPage.MinorControls != null )
			{
				foreach ( ICommandBarElement Btn in CmdPage.MinorControls )
					MinorCmdBar.PrimaryCommands.Add( Btn );
			}

			CommandMgr.SetMajorCommands( CmdPage.MajorControls, CmdPage.MajorNav );
			CommandMgr.Set2ndCommands( CmdPage.Major2ndControls );
		}

		private void Controls_ControlChanged( object sender )
		{
			// Check if SubView opened first
			if ( InSubView )
			{
				// If SubView is opened, check for SubView only
				if ( sender == SubView.Content )
				{
					SetControls( sender, false );
					return;
				}
			}
			// Check for MainView
			else if ( sender == View.Content )
			{
				SetControls( sender, false );
				return;
			}

			Logger.Log( ID, "Sender is not current view, controls unchanged", LogType.INFO );
		}

		private void ApplyControlSet()
		{
			if ( MainStage.Instance.IsPhone )
			{
				MajorCmdBar = BottomCmdBar;
				MinorCmdBar = TopCmdBar;
				VerticalStack.UpdateDelay = 100;
			}
			else
			{
				MajorCmdBar = TopCmdBar;
				MinorCmdBar = BottomCmdBar;
			}

			MajorCmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
			MinorCmdBar.ClosedDisplayMode = AppBarClosedDisplayMode.Hidden;

			MajorCmdBar.Opening += ( sender, e ) => MinorCmdBar.IsOpen = MinorCmdBar.PrimaryCommands.Any();
			MajorCmdBar.Closing += ( sender, e ) => MinorCmdBar.IsOpen = false;
			// Be aware of possible infinite event loop
			MinorCmdBar.Closing += ( sender, e ) => MajorCmdBar.IsOpen = false;

			CommandMgr = new global::GR.GSystem.MasterCommandManager( MajorCmdBar.PrimaryCommands, MajorCmdBar.SecondaryCommands );
		}

		public void ReloadCommands()
		{
			SetControls( InSubView ? SubView.Content : View.Content, false );
		}

		private void CmdBar_Loaded( object sender, RoutedEventArgs e )
		{
			if ( sender == MajorCmdBar )
			{
				// Apply Add/Remove transitions for MajorCommandBar for PrimaryCommands
				ItemsControl ButtonControls = MajorCmdBar.ChildAt<ItemsControl>( 0, 0, 0, 1 );
				if ( ButtonControls != null && ButtonControls.ItemContainerTransitions == null )
				{
					ButtonControls.ItemContainerTransitions = new TransitionCollection();
					ButtonControls.ItemContainerTransitions.Add( new EdgeUIThemeTransition() );
				}
			}
			else if ( sender == MinorCmdBar )
			{
				// Hide the more button for Minor Command Bar
				Button MoreButton = MinorCmdBar.ChildAt<Button>( 0, 0, 1 );
				if ( MoreButton != null ) MoreButton.Visibility = Visibility.Collapsed;
			}
		}

		private void SetBackButton( bool Visible )
		{
			// Each time a navigation event occurs, update the Back button's visibility
			NavigationHandler.OnNavigatedBack -= BSInterceptorBack;

			if ( TryGetInterceptor( out IBackStackInterceptor Interceptor ) )
			{
				NavigationHandler.InsertHandlerOnNavigatedBack( BSInterceptorBack );
				Interceptor.Update_CanGoBack = Update_ViewBackButton;
			}

			Update_ViewBackButton( InSubView ? SubView : View );
		}

		private bool TryGetInterceptor( out IBackStackInterceptor Interceptor )
		{
			Interceptor = null;

			if ( InSubView )
			{
				if ( SubView.Content is IBackStackInterceptor )
				{
					Interceptor = ( IBackStackInterceptor ) SubView.Content;
				}
			}
			else if ( View.Content is IBackStackInterceptor )
			{
				Interceptor = ( IBackStackInterceptor ) View.Content;
			}

			return Interceptor != null;
		}

		private void Update_ViewBackButton( object sender )
		{
			bool CanGoBack = InSubView || BackStack.CanGoBack;

			if ( !CanGoBack && TryGetInterceptor( out IBackStackInterceptor Interceptor ) )
			{
				CanGoBack = Interceptor.CanGoBack;
			}

			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = CanGoBack
				? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
		}

		private async void BSInterceptorBack( object sender, XBackRequestedEventArgs e )
		{
			if ( TryGetInterceptor( out IBackStackInterceptor Interceptor ) )
			{
				e.Handled = await Interceptor.GoBack();
				Update_ViewBackButton( Interceptor );
			}
		}

		private void PreventBack( object sender, XBackRequestedEventArgs e )
		{
			e.Handled = true;
			ActionBlocked();
		}

		private volatile int ActionBlocking = 0;

		private async void ActionBlocked()
		{
			if ( 0 < ActionBlocking )
			{
				ActionBlocking = 2;
				return;
			}

			ActionBlocking = 1;

			TransitionDisplay.SetState( MajorCmdBar, TransitionState.Inactive );
			TransitionDisplay.SetState( MinorCmdBar, TransitionState.Inactive );
			TransitionDisplay.SetState( MainStage.Instance.BadgeBlock, TransitionState.Active );

			while ( 0 < ActionBlocking )
			{
				await Task.Delay( 1000 );
				ActionBlocking--;
			}

			TransitionDisplay.SetState( MajorCmdBar, TransitionState.Active );
			TransitionDisplay.SetState( MinorCmdBar, TransitionState.Active );
			TransitionDisplay.SetState( MainStage.Instance.BadgeBlock, TransitionState.Inactive );
		}

	}
}