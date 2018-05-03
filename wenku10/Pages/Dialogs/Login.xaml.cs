using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Loaders;

using GR.Ext;
using GR.GSystem;

namespace wenku10.Pages.Dialogs
{
	sealed partial class Login : ContentDialog
	{
		public bool Canceled = true;

		private IMember Member;

		private bool UseSavedPassword = false;

		public Login( IMember Member )
		{
			this.InitializeComponent();
			this.Member = Member;

			StringResources stx = StringResources.Load();
			PrimaryButtonText = stx.Text( "Login" );
			SecondaryButtonText = stx.Text( "Button_Back" );

			if ( !string.IsNullOrEmpty( Member.ServerMessage ) )
			{
				ShowMessage( Member.ServerMessage );
			}

			if( Member.CanRegister )
			{
				RegisterBtn.Visibility = Visibility.Visible;
			}

			DisplaySavedAuth();
		}

		private async void DisplaySavedAuth()
		{
			LoginInfo Info = await new CredentialVault().Retrieve( Member );

			if ( string.IsNullOrEmpty( Info.Account ) )
				return;

			Account.Text = Info.Account;

			if ( !string.IsNullOrEmpty( Info.Password ) )
			{
				Password.Password = "************";
				Password.Loaded += Password_Focus;
			}
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			args.Cancel = true;
			DetectInputLogin();
			Canceled = false;
		}

		private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
		}

		private void OnKeyDown( object sender, KeyRoutedEventArgs e )
		{
			if ( e.Key == Windows.System.VirtualKey.Enter )
			{
				e.Handled = DetectInputLogin();
			}
			else if ( sender == Password )
			{
				UseSavedPassword = false;
			}
		}

		private bool DetectInputLogin()
		{
			string Name = Account.Text.Trim();

			if ( string.IsNullOrEmpty( Name ) || string.IsNullOrEmpty( Password.Password ) )
			{
				if ( string.IsNullOrEmpty( Name ) )
				{
					Account.Focus( FocusState.Keyboard );
				}
				else
				{
					Password.Focus( FocusState.Keyboard );
				}
				return false;
			}
			else
			{
				Authenticate();
				return true;
			}
		}

		private async void Authenticate()
		{
			IsPrimaryButtonEnabled
				= IsSecondaryButtonEnabled
				= Account.IsEnabled
				= Password.IsEnabled
				= false
				;

			// Re-focus to disable keyboard
			this.Focus( FocusState.Pointer );

			// Auth async without await
			if ( UseSavedPassword )
			{
				LoginInfo Info = await new CredentialVault().Retrieve( Member );
				await Member.Authenticate( Info.Account, Info.Password, true );
			}
			else
			{
				await Member.Authenticate( Account.Text.Trim(), Password.Password, RememberInfo.IsChecked == true );
			}

			if ( Member.IsLoggedIn )
			{
				Hide();
			}
			else
			{
				IsPrimaryButtonEnabled
					= IsSecondaryButtonEnabled
					= Account.IsEnabled
					= Password.IsEnabled
					= true
					;
			}
		}

		private void ShowMessage( string Mesg )
		{
			if ( string.IsNullOrEmpty( Mesg ) ) return;

			ServerMessage.Text = Mesg;
			ServerMessage.Visibility = Visibility.Visible;
		}

		private void Password_Focus( object sender, RoutedEventArgs e )
		{
			Password.Focus( FocusState.Keyboard );
		}

		private void RegisterBtn_Click( object sender, RoutedEventArgs e )
		{
			Hide();
			Member.Register();
		}

	}
}