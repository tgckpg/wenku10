using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku10.Pages;
using wenku10.Pages.Dialogs;
using wenku10.Pages.Sharers;
using wenku10.SHHub;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Data;
	using DataSources;
	using Effects;
	using Ext;
	using Model.Interfaces;
	using Model.ListItem.Sharers;
	using Model.Section.SharersHub;
	using Resources;
	using System.ComponentModel;
	using Windows.UI.Xaml.Data;

	sealed class ONSPageExt : PageExtension, ICmdControls
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => true;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private ONSViewSource ViewSource;

		private AppBarButtonEx ActivyBtn;
		private ActivityList ActivyList;

		private SHMember MInstance;
		private SecondaryIconButton LoginBtn;

		public ONSPageExt( ONSViewSource ViewSource )
		{
			this.ViewSource = ViewSource;
		}

		public void OpenItem( IGRRow Row )
		{
			if ( Row is GRRow<HSDisplay> )
			{
				HubScriptItem HSI = ( ( GRRow<HSDisplay> ) Row ).Source.Item;

				if ( HSI.Faultered )
				{
					// Report to admin
				}
				else
				{
					ControlFrame.Instance.NavigateTo( PageId.SCRIPT_DETAILS, () => new ScriptDetails( HSI ) );
				}
			}
		}

		protected override void SetTemplate()
		{
			InitAppBar();
			MInstance = X.Singleton<SHMember>( XProto.SHMember );
			MInstance.PropertyChanged += ( s, e ) => UpdateLoginButton();

			ActivyList = new ActivityList();
			ActivyList.ItemsSource = MInstance.Activities;
			ActivyList.ItemClick = CheckActivity;
			TransitionDisplay.SetState( ActivyList, TransitionState.Inactive );

			// Set binding to Count icon in Activity button
			Binding CountBinding = new Binding() { Path = new PropertyPath( "Count" ), Source = MInstance.Activities };
			BindingOperations.SetBinding( ActivyBtn, AppBarButtonEx.CountProperty, CountBinding );

			if( Page is MasterExplorer Expl )
			{
				Expl.MainContainer.Children.Add( ActivyList );
			}

			UpdateLoginButton();
		}

		public override void Unload()
		{
		}

		private void InitAppBar()
		{
			StringResources stx = StringResources.Load( "AppResources", "ContextMenu" );

			ActivyBtn = new AppBarButtonEx()
			{
				Icon = new SymbolIcon( Symbol.Message ),
				Label = stx.Text( "Messages" ),
				Foreground = new SolidColorBrush( LayoutSettings.RelativeMajorBackgroundColor )
			};

			ActivyBtn.Click += ToggleActivities;

			AppBarButton UploadBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.Upload, stx.Text( "SubmitScript" ) );
			UploadBtn.Click += ( s, e ) => ControlFrame.Instance.SubNavigateTo( Page, () => new ScriptUpload( UploadExit ) );

			SecondaryIconButton MAuthBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Manage, stx.Text( "ManageAuths", "ContextMenu" ) );
			MAuthBtn.Click += ManageAuths;

			MajorControls = new ICommandBarElement[] { ActivyBtn, UploadBtn };

			LoginBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.ChevronRight, stx.Text( "Login" ) );
			LoginBtn.Click += ( s, e ) => SHLoginOrInfo();

#if DEBUG || TESTING
			StringResources sts = StringResources.Load( "Settings" );
			SecondaryIconButton ChangeServer = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.DirectAccess, sts.Text( "Advanced_Server" ) );
			ChangeServer.Click += async ( s, e ) =>
			{
				ValueHelpInput VH = new ValueHelpInput(
					Shared.ShRequest.Server.ToString()
					, sts.Text( "Advanced_Server" ), "Address"
				);

				await Popups.ShowDialog( VH );
				if ( VH.Canceled ) return;

				try
				{
					Shared.ShRequest.Server = new Uri( VH.Value );
					ONSSystem.Config.ServiceUri = VH.Value;
				}
				catch ( Exception ) { }
			};

			Major2ndControls = new ICommandBarElement[] { LoginBtn, MAuthBtn, ChangeServer };
#else
			Major2ndControls = new ICommandBarElement[] { LoginBtn, UploadBtn, MAuthBtn };
#endif
		}

		private void UploadExit( string Id, string AccessToken )
		{
			var j = ControlFrame.Instance.CloseSubView();
			ViewSource.DataSource.Search = "uuid: " + Id;
		}

		private void ManageAuths( object sender, RoutedEventArgs e )
		{
			ControlFrame.Instance.SubNavigateTo( Page, () => new ManageAuth() );
		}

		private async void ToggleActivities( object sender, RoutedEventArgs e )
		{
			if ( !( await MInstance.Authenticate() ) )
				return;

			if ( MInstance.Activities.Count == 0 )
			{
				ActivyBtn.IsEnabled = false;
				await new MyRequests().Get();
				await new MyInbox().Get();
				ActivyBtn.IsEnabled = true;
			}

			// We'll have to set the target button here
			// Because activity list needs the button in the visual tree to work
			ActivyList.TargetBtn = ActivyBtn;

			if ( 0 < MInstance.Activities.Count )
			{
				if ( TransitionDisplay.GetState( ActivyList ) == TransitionState.Active )
				{
					TransitionDisplay.SetState( ActivyList, TransitionState.Inactive );
				}
				else
				{
					TransitionDisplay.SetState( ActivyList, TransitionState.Active );
				}
			}
		}

		private void CheckActivity( object sender, ItemClickEventArgs e )
		{
			TransitionDisplay.SetState( ActivyList, TransitionState.Inactive );
			MInstance.Activities.CheckActivity( ( Activity ) e.ClickedItem );
		}

		private void UpdateLoginButton()
		{
			StringResources stx = StringResources.Load( "AppResources", "Settings" );

			if ( MInstance.IsLoggedIn )
			{
				LoginBtn.Label = stx.Text( "Account", "Settings" );
				LoginBtn.Glyph = SegoeMDL2.Accounts;
			}
			else
			{
				LoginBtn.Label = stx.Text( "Login" );
				LoginBtn.Glyph = SegoeMDL2.ChevronRight;
			}
		}

		private async void SHLoginOrInfo()
		{
			if ( MInstance.IsLoggedIn )
			{
				ControlFrame.Instance.NavigateTo( PageId.SH_USER_INFO, () => new UserInfo() );
			}
			else
			{
				Login LoginDialog = new Login( MInstance );
				await Popups.ShowDialog( LoginDialog );
			}
		}

	}
}