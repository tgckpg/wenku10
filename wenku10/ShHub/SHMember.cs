using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using GR.AdvDM;
using GR.Ext;
using GR.Resources;
using GR.Settings;
using GR.Model.REST;
using GR.Model.Section.SharersHub;
using GR.GSystem;

namespace wenku10.SHHub
{
	sealed class SHMember : ActiveData, IMember
	{
		public static readonly string ID = typeof( SHMember ).Name;

		private RuntimeCache RCache = new RuntimeCache();

		public bool CanRegister => true;
		public string Id { get; private set; }

		private bool _IsLoggedIn;
		public bool IsLoggedIn
		{
			get => _IsLoggedIn;
			private set
			{
				if ( _IsLoggedIn == value )
					return;

				_IsLoggedIn = value;
				NotifyChanged( "IsLoggedIn" );
			}
		}

		public Activities Activities { get; private set; }

		public string ServerMessage { get; private set; }

		public SHMember()
		{
			Activities = new Activities();
		}

		public async Task<bool> Authenticate()
		{
			if ( IsLoggedIn )
			{
				return true;
			}

			if ( RestoreAuth() && await ValidateSession() )
			{
				return true;
			}

			TaskCompletionSource<bool> TCS = new TaskCompletionSource<bool>();
			MessageBus.SendUI( typeof( SHMember ), AppKeys.PROMPT_LOGIN, new Tuple<IMember, Action>( this, () => TCS.SetResult( IsLoggedIn ) ) );
			return await TCS.Task;
		}

		public async Task<bool> Authenticate( string Account, string Password, bool Remember )
		{
			TaskCompletionSource<bool> LoginRequest = new TaskCompletionSource<bool>();
			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.Login( Account, Password )
				, ( e, id ) =>
				{
					if ( e.ResponseHeaders.Contains( "Set-Cookie" ) )
					{
						SaveAuth( e.Cookies );
					}

					try
					{
						JsonStatus.Parse( e.ResponseString );
					}
					catch ( Exception ex )
					{
						ServerMessage = ex.Message;
					}

					LoginRequest.TrySetResult( true );
				}
				, Utils.DoNothing 
				, false
			);

			await LoginRequest.Task;
			await ValidateSession();

			if ( IsLoggedIn && Remember )
			{
				await CredentialVault.Protect( this, Account, Password );
			}

			return IsLoggedIn;
		}

		public async Task<bool> Register()
		{
			Pages.Dialogs.Sharers.Register RegisterDialog = new Pages.Dialogs.Sharers.Register();
			await Popups.ShowDialog( RegisterDialog );
			if ( RegisterDialog.Canceled ) return false;

			var j = Popups.ShowDialog( new Pages.Dialogs.Login( this ) );
			return true;
		}

		public void Logout()
		{
			IsLoggedIn = false;
			new CredentialVault().Remove( this );

			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.Logout()
				, ClearAuth, ClearAuth
				, false
			);
		}

		private async Task<bool> ValidateSession()
		{
			TaskCompletionSource<bool> IsValid = new TaskCompletionSource<bool>();

			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.SessionValid()
				, ( e, id ) =>
				{
					try
					{
						JsonObject JObj = JsonStatus.Parse( e.ResponseString );
						Id = JObj.GetNamedString( "data" );

						IsValid.TrySetResult( true );
					}
					catch ( Exception ex )
					{
						IsValid.TrySetResult( false );
						Logger.Log( ID, ex.Message, LogType.DEBUG );
					}
				}
				, ClearAuth
				, false
			);

			IsLoggedIn = await IsValid.Task;
			return IsLoggedIn;
		}

		private bool RestoreAuth()
		{
			string[] Cookie = ONSSystem.Config.AuthToken?.Split( '\n' );

			if ( Cookie == null )
				return false;

			try
			{
				Cookie MCookie = new Cookie( Cookie[ 0 ], Cookie[ 1 ], Cookie[ 2 ] );
				WHttpRequest.Cookies.Add( Shared.ShRequest.Server, MCookie );
				return true;
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.WARNING );
				ONSSystem.Config.AuthToken = null;
			}

			return false;
		}

		private bool SaveAuth( CookieCollection Cookies )
		{
			foreach ( Cookie cookie in Cookies )
			{
				if ( cookie.Name == "sid" )
				{
					Logger.Log( ID, string.Format( "Set-Cookie: {0}=...", cookie.Name ), LogType.DEBUG );
					ONSSystem.Config.AuthToken = string.Format( "{0}\n{1}\n{2}", cookie.Name, cookie.Value, cookie.Path );
					return true;
				}
			}

			return false;
		}

		private void ClearAuth( string arg1, string arg2, Exception arg3 ) { ClearAuth(); }
		private void ClearAuth( DRequestCompletedEventArgs arg1, string arg2 ) { ClearAuth(); }
		private void ClearAuth()
		{
			WHttpRequest.Cookies = new CookieContainer();
			ONSSystem.Config.AuthToken = null;
		}

	}
}