using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Loaders;

using GR.AdvDM;
using GR.Effects;
using GR.Ext;
using GR.Model.Comments;
using GR.Model.Interfaces;
using GR.Model.ListItem.Sharers;
using GR.Model.REST;
using GR.Resources;
using GR.Settings;
using GR.Storage;

using CryptAES = GR.GSystem.CryptAES;
using CryptRSA = GR.GSystem.CryptRSA;

namespace wenku10.Pages.Sharers
{
	using Dialogs.Sharers;
	using GR.CompositeElement;
	using SHTarget = SharersRequest.SHTarget;

	sealed partial class HSRequestView : Page, ICmdControls
	{
		public static readonly string ID = typeof( HSRequestView ).Name;

		private HubScriptItem BindItem;

		private RuntimeCache RCache = new RuntimeCache();

		private string AccessToken;
		private volatile SHTarget ReqTarget;

		private CryptAES Crypt;
		private XRegistry XGrant = new XRegistry( "<xg />", FileLinks.ROOT_SETTING + "XGrant.tmp" );
		private Observables<SHRequest, SHRequest> RequestsSource;

		private AppBarButton PlaceBtn;

		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private int LoadLevel = 0;

		private HSRequestView()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public HSRequestView( HubScriptItem HSI, CryptAES Crypt, SHTarget ReqTarget , string AccessToken )
			:this()
		{
			BindItem = HSI;
			this.Crypt = Crypt;
			this.ReqTarget = ReqTarget;
			this.AccessToken = AccessToken;

			XGrant.SetParameter( BindItem.Id, CustomAnchor.TimeKey );
			ShowRequest( ReqTarget );
		}

		private void SetTemplate()
		{
			InitAppBar();
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "ContextMenu" );

			PlaceBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Text( "PlaceRequest" ) );
			PlaceBtn.Click += ( sender, e ) => PlaceRequest();

			MajorControls = new AppBarButton[] { PlaceBtn };
		}

		public async void PlaceRequest()
		{
			StringResources stx = new StringResources();

			PlaceRequest RequestBox = new PlaceRequest(
				ReqTarget, BindItem
				, stx.Text( ( ReqTarget & SHTarget.KEY ) != 0 ? "KeyRequest" : "TokenRequest" )
			);

			await Popups.ShowDialog( RequestBox );
			if ( !RequestBox.Canceled ) OpenRequest( ReqTarget );
		}

		public void OpenRequest( SHTarget Target )
		{
			ShowRequest( Target );
		}

		private void ShowRequest( SHTarget Target )
		{
			// User have the thing. So he / she can grant requests for this script
			if ( ( Target & SHTarget.KEY ) != 0 )
			{
				RequestList.Tag = BindItem.Encrypted && Crypt != null;
			}
			else
			{
				// Default Targeted to TOKEN
				Target = SHTarget.TOKEN;
				RequestList.Tag = AccessToken;
			}

			ReloadRequests( Target );
		}

		private void GrantRequest( object sender, RoutedEventArgs e )
		{
			SHRequest Req = ( ( Button ) sender ).DataContext as SHRequest;
			if ( Req == null ) return;

			try
			{
				CryptRSA RSA = new CryptRSA( Req.Pubkey );
				string GrantData = null;

				switch ( ReqTarget )
				{
					case SHTarget.TOKEN:
						if ( !string.IsNullOrEmpty( AccessToken ) )
						{
							GrantData = RSA.Encrypt( AccessToken );
						}
						break;
					case SHTarget.KEY:
						if ( Crypt != null )
						{
							GrantData = RSA.Encrypt( Crypt.KeyBuffer );
						}
						break;
				}

				if ( !string.IsNullOrEmpty( GrantData ) )
				{
					RCache.POST(
						Shared.ShRequest.Server
						, Shared.ShRequest.GrantRequest( Req.Id, GrantData )
						, GrantComplete
						, GrantFailed
						, false
					);
				}
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message );
			}
		}

		private void GrantFailed( string CacheName, string Id, Exception ex )
		{
			System.Diagnostics.Debugger.Break();
		}

		private void GrantComplete( DRequestCompletedEventArgs e, string Id )
		{
			try
			{
				JsonStatus.Parse( e.ResponseString );
				SetGranted( Id );
			}
			catch( Exception ex )
			{
				Logger.Log( ID, ex.Message );
			}
		}

		private void SetGranted( string Id )
		{
			RequestsSource.Any( x =>
			{
				if ( x.Id == Id )
				{
					x.Granted = true;
					return true;
				}
				return false;
			} );

			XParameter XParam = XGrant.Parameter( BindItem.Id );
			XParam.SetParameter( new XParameter( Id ) );
			XGrant.SetParameter( XParam );
			XGrant.Save();
		}

		private async void ReloadRequests( SHTarget Target )
		{
			if ( 0 < LoadLevel ) return;

			ReqTarget = Target;
			MarkLoading();
			HSLoader<SHRequest> CLoader = new HSLoader<SHRequest>(
				BindItem.Id
				, Target
				, ( _Target, _Skip, _Limit, _Ids ) => Shared.ShRequest.GetRequests( _Target, _Ids[ 0 ], _Skip, _Limit )
			);
			CLoader.ConvertResult = xs =>
			{
				XParameter XParam = XGrant.Parameter( BindItem.Id );
				if ( XParam != null )
				{
					foreach ( SHRequest x in xs )
					{
						x.Granted = XParam.FindParameter( x.Id ) != null;
					}
				}
				return xs.ToArray();
			};

			IList<SHRequest> FirstPage = await CLoader.NextPage();
			MarkNotLoading();

			RequestsSource = new Observables<SHRequest, SHRequest>( FirstPage );
			RequestsSource.ConnectLoader( CLoader );

			RequestsSource.LoadStart += ( x, y ) => MarkLoading();
			RequestsSource.LoadEnd += ( x, y ) => MarkNotLoading();
			RequestList.ItemsSource = RequestsSource;
		}

		private void MarkLoading()
		{
			LoadLevel++;
		}

		private void MarkNotLoading()
		{
			LoadLevel--;
		}
	}
}
