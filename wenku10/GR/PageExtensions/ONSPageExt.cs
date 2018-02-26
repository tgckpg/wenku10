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
			MInstance.OnStatusChanged += SHMember_OnStatusChanged;
			SHMember_OnStatusChanged( MInstance, MInstance.Status );
		}

		public override void Unload()
		{
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppResources", "ContextMenu" );

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
			StringResources sts = new StringResources( "Settings" );
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
					new Uri( VH.Value );
					Config.Properties.SERVER_OSD_URI = VH.Value;
					Shared.ShRequest.UpdateServer();
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
			if ( !( await SHMember.Authenticate() ) )
				return;

			/*
			<Grid x:Name="ActivyList"
				  Grid.RowSpan="2"
				  Visibility="Collapsed"
				  MaxWidth="400"
				  VerticalAlignment="Top" HorizontalAlignment="Right">
				<Polygon Points="15,0 30,15 0,15" HorizontalAlignment="Right"
						 Margin="0,0,5,0"
						 Fill="{StaticResource MinorBrush}" />
				<ListView Margin="0,15,0,0" Padding="10"
						  IsItemClickEnabled="True" ItemClick="Activities_ItemClick"
						  ItemContainerStyle="{StaticResource BareListItem}"
						  ItemsSource="{Binding Activities}"
						  Background="{StaticResource MinorBrush}">
					<ListView.ItemTemplate>
						<DataTemplate>
							<StackPanel Margin="10,5">
								<TextBlock Foreground="{StaticResource RelativeMajorBrush}"
										   TextTrimming="CharacterEllipsis"
										   Text="{Binding Name}" />
								<TextBlock Foreground="{StaticResource RelativeMajorBrush}"
										   TextAlignment="Right" Opacity="0.8"
										   Visibility="{Binding TimeStamp, Converter={StaticResource DataVisConverter}}"
										   Text="{Binding TimeStamp, Converter={StaticResource RelativeTimeConverter}}" />
							</StackPanel>
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>
			</Grid>
			*/

			if ( MInstance.Activities.Count == 0 )
			{
				await new MyRequests().Get();
				await new MyInbox().Get();
			}
			else
			{
				/*
				if ( TransitionDisplay.GetState( ActivyList ) == TransitionState.Active )
				{
					TransitionDisplay.SetState( ActivyList, TransitionState.Inactive );
				}
				else
				{
					TransitionDisplay.SetState( ActivyList, TransitionState.Active );
				}
				*/
			}
		}

		private void SHMember_OnStatusChanged( object sender, MemberStatus args )
		{
			StringResources stx = new StringResBg( "AppResources", "Settings" );
			if ( args == MemberStatus.LOGGED_IN )
			{
				LoginBtn.Label = stx.Text( "Account", "Settings" );
				LoginBtn.Glyph = SegoeMDL2.Accounts;
			}
			else if( args == MemberStatus.RE_LOGIN_NEEDED )
			{
				var j = SHMember.Authenticate();
			}
			else
			{
				LoginBtn.Label = stx.Text( "Login" );
				LoginBtn.Glyph = SegoeMDL2.ChevronRight;
			}
		}

		private async void SHLoginOrInfo()
		{
			if ( MInstance.WillLogin ) return;
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