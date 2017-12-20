using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using GR.CompositeElement;
using GR.Ext;
using GR.Model.Interfaces;
using GR.Model.Loaders;
using GR.Model.ListItem;
using GR.Model.ListItem.Sharers;
using GR.Model.Section.SharersHub;

using CryptAES = GR.GSystem.CryptAES;
using RSAManager = GR.GSystem.RSAManager;
using AESManager = GR.GSystem.AESManager;
using TokenManager = GR.GSystem.TokenManager;
using SHTarget = GR.Model.REST.SharersRequest.SHTarget;

namespace wenku10.Pages.Sharers
{
	sealed partial class ManageAuth : Page, ICmdControls
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		AppBarButton ExportAuthBtn;
		AppBarButton ImportKeyBtn;
		AppBarButton ImportTokBtn;

		private RSAManager RSAMgr;
		private AESManager AESMgr;
		private TokenManager TokMgr;

		private AuthItem SelectedItem;
		private GrantProcess SelectedRequest;

		public ManageAuth()
		{
			this.InitializeComponent();

			SetTemplate();
		}

		private async void SetTemplate()
		{
			StringResources stx = new StringResources( "AppResources", "ContextMenu", "WMessage", "LoadingMessage", "AppBar" );

			InitAppBar( stx );

			KeysSection.Header = stx.Text( "Secret" );
			TokensSection.Header = stx.Text( "AccessTokens", "ContextMenu" );
			RequestsSection.Header = stx.Text( "Requests" );

			IMember Member = X.Singleton<IMember>( XProto.SHMember );

			if ( !Member.IsLoggedIn )
			{
				// Please login message
				ReqPlaceholder.Text = stx.Str( "4", "WMessage" );
			}
			else
			{
				ReqPlaceholder.Text = stx.Str( "ProgressIndicator_PleaseWait", "LoadingMessage" );
				LoadRequests();
			}

			RSAMgr = await RSAManager.CreateAsync();

			AESMgr = new AESManager();
			ReloadAuths( KeyList, SHTarget.KEY, AESMgr );

			TokMgr = new TokenManager();
			ReloadAuths( TokenList, SHTarget.TOKEN, TokMgr );
		}

		private async void LoadRequests()
		{
			MyRequests Reqs = new MyRequests();
			await Reqs.Get();

			ReqPlaceholder.Visibility = Visibility.Collapsed;
			RequestsList.ItemsSource = Reqs.Grants.Remap( x => new GrantProcess( x ) );
		}

		private void InitAppBar( StringResources stx )
		{
			ExportAuthBtn = UIAliases.CreateAppBarBtn( Symbol.SaveLocal, stx.Text( "Export", "AppBar" ) );
			ExportAuthBtn.Click += ExportAuths;

			ImportKeyBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Text( "Add", "AppBar" ) );
			ImportKeyBtn.Click += ImportKey;

			ImportTokBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Text( "Add", "AppBar" ) );
			ImportTokBtn.Click += ImportToken;
		}

		private void MasterPivot_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			ExportAuthBtn.Tag = null;
			if ( MasterPivot.SelectedItem == KeysSection )
			{
				MajorControls = new ICommandBarElement[] { ExportAuthBtn, ImportKeyBtn };
				ExportAuthBtn.Tag = "Keys";
			}
			else if ( MasterPivot.SelectedItem == TokensSection )
			{
				MajorControls = new ICommandBarElement[] { ExportAuthBtn, ImportTokBtn };
				ExportAuthBtn.Tag = "Tokens";
			}
			else if ( MasterPivot.SelectedItem == RequestsSection )
			{
				MajorControls = new ICommandBarElement[] {};
			}

			ControlChanged?.Invoke( this );
		}

		public void GotoRequests() { MasterPivot.SelectedItem = RequestsSection; }

		private void ShowContextMenu( object sender, RightTappedRoutedEventArgs e )
		{
			Border B = ( Border ) sender;
			SelectedItem = ( AuthItem ) B.DataContext;

			FlyoutBase.ShowAttachedFlyout( B );
		}

		private void ShowRequestContext( object sender, RightTappedRoutedEventArgs e )
		{
			StackPanel B = ( StackPanel ) sender;
			SelectedRequest = ( GrantProcess ) B.DataContext;

			FlyoutBase.ShowAttachedFlyout( B );
		}

		private async void Rename( object sender, RoutedEventArgs e )
		{
			string OName = SelectedItem.Name;

			Dialogs.Rename RenameBox = new Dialogs.Rename( SelectedItem );
			await Popups.ShowDialog( RenameBox );

			if ( RenameBox.Canceled ) return;

			string NewName = SelectedItem.Name;

			if ( SelectedItem.Value is CryptAES )
			{
				AESMgr.RenameAuth( OName, NewName );
			}
			else
			{
				TokMgr.RenameAuth( OName, NewName );
			}
		}

		private async void Delete( object sender, RoutedEventArgs e )
		{
			bool DoDelete = SelectedItem.Count == 0;

			if ( !DoDelete )
			{
				StringResources stx = new StringResources( "Message" );
				MessageDialog MsgBox = new MessageDialog( SelectedItem.DeleteMessage );

				MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { DoDelete = true; } ) );
				MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );
				await Popups.ShowDialog( MsgBox );
			}

			if ( DoDelete )
			{
				if ( SelectedItem.Value is CryptAES )
				{
					AESMgr.RemoveAuth( SelectedItem.Value.Value, ( CryptAES ) SelectedItem.Value );
					ReloadAuths( KeyList, SHTarget.KEY, AESMgr );
				}
				else
				{
					TokMgr.RemoveAuth( SelectedItem.Value.Value, SelectedItem.Value );
					ReloadAuths( TokenList, SHTarget.TOKEN, TokMgr );
				}

				SelectedItem = null;
			}
		}

		private void ParseGrant( object sender, RoutedEventArgs e )
		{
			( ( GrantProcess ) ( ( Button ) sender ).DataContext ).Parse( RSAMgr.AuthList );
		}

		private async void WithdrawRequest( object sender, RoutedEventArgs e )
		{
			if ( await SelectedRequest.Withdraw() )
			{
				RequestsList.ItemsSource = ( ( IEnumerable<GrantProcess> ) RequestsList.ItemsSource )
					.Where( x => x != SelectedRequest );
			}
		}

		private async void GotoScriptDetail( object sender, ItemClickEventArgs e )
		{
			GrantProcess GProc = ( GrantProcess ) e.ClickedItem;
			if ( GProc.GrantDef.SourceRemoved || GProc.IsLoading ) return;

			GProc.IsLoading = true;
			string AccessToken = ( string ) TokMgr.GetAuthById( GProc.ScriptId )?.Value;

			SHSearchLoader SHLoader = new SHSearchLoader(
				"uuid: " + GProc.ScriptId
				, AccessToken == null ? null : new string[] { AccessToken }
			);

			IList<HubScriptItem> HSIs = await SHLoader.NextPage();
			HubScriptItem HSI = HSIs.FirstOrDefault();

			if ( HSI != null )
			{
				ControlFrame.Instance.NavigateTo( PageId.SCRIPT_DETAILS, () => {
					ScriptDetails SDetails = new ScriptDetails( HSI );
					SDetails.OpenRequest( GProc.Target );
					return SDetails;
				} );
			}
		}

		private void ReloadAuths<T>( ListView LView, SHTarget Target, global::GR.GSystem.AuthManager<T> Mgr )
		{
			LView.ItemsSource = Mgr.AuthList.Remap( x =>
			{
				NameValue<string> NX = x as NameValue<string>;
				AuthItem Item = new AuthItem( NX, Target );
				Item.Count = Mgr.ControlCount( NX.Value );
				return Item;
			} );
		}

		private async void ImportKey( object sender, RoutedEventArgs e )
		{
			NameValue<string> NV = new NameValue<string>( "", "" );
			StringResources stx = new StringResources( "AppResources", "ContextMenu" );
			Dialogs.NameValueInput NVInput = new Dialogs.NameValueInput(
				NV, stx.Text( "New" ) + stx.Text( "Secret" )
				, stx.Text( "Name" ), stx.Text( "Secret" )
			);

			await Popups.ShowDialog( NVInput );

			if ( NVInput.Canceled ) return;

			try
			{
				AESMgr.ImportAuth( NV.Name, NV.Value );
				ReloadAuths( KeyList, SHTarget.KEY, AESMgr );
			}
			catch ( Exception )
			{ }
		}

		private async void ExportAuths( object sender, RoutedEventArgs e )
		{
			Button Btn = ( Button ) sender;
			Btn.IsEnabled = false;

			string Tag = ( string ) Btn.Tag;

			IStorageFile ISF = await AppStorage.SaveFileAsync( "wenku10 Auth", new List<string>() { ".xml" }, Tag );
			if ( ISF == null )
			{
				Btn.IsEnabled = true;
				return;
			}

			try
			{
				using ( Stream s = await ISF.OpenStreamForWriteAsync() )
				{
					await global::GR.Resources.Shared.Storage.GetStream(
						Tag == "Keys"
							? AESMgr.SettingsFile
							: TokMgr.SettingsFile
					).CopyToAsync( s );

					await s.FlushAsync();
				}
			}
			catch( Exception )
			{
				// Failed to save file
			}

			Btn.IsEnabled = true;
		}

		private async void ImportToken( object sender, RoutedEventArgs e )
		{
			NameValue<string> NV = new NameValue<string>( "", "" );

			StringResources stx = new StringResources( "AppResources", "ContextMenu" );
			Dialogs.NameValueInput NVInput = new Dialogs.NameValueInput(
				NV, stx.Text( "New" ) + stx.Text( "AccessTokens", "ContextMenu" )
				, stx.Text( "Name" ), stx.Text( "AccessTokens", "ContextMenu" )
			);

			await Popups.ShowDialog( NVInput );

			if ( NVInput.Canceled ) return;

			try
			{
				TokMgr.ImportAuth( NV.Name, NV.Value );
				ReloadAuths( TokenList, SHTarget.TOKEN, TokMgr );
			}
			catch( Exception )
			{ }
		}

		private class AuthItem : NameValue<NameValue<string>>
		{
			public override string Name
			{
				get { return base.Value.Name; }
				set
				{
					base.Value.Name = value;
					NotifyChanged( "Name" );
				}
			}

			public SHTarget AuthType { get; set; }
			public int Count { get; set; }

			public string DeleteMessage
			{
				get
				{
					StringResources stx = new StringResources( "Message" );
					return stx.Str( "DeleteEffective" + AuthType.ToString() );
				}
			}

			public AuthItem( NameValue<string> Value, SHTarget AuthType )
				: base( Value.Name, Value )
			{
				this.AuthType = AuthType;
			}
		}

	}
}