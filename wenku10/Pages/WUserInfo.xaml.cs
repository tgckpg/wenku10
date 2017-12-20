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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using GR.CompositeElement;
using GR.Ext;
using GR.Model.Interfaces;
using GR.Resources;

namespace wenku10.Pages
{
	public sealed partial class WUserInfo : Page, ICmdControls
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get ; private set; }

		private IMemberInfo Settings;

		public WUserInfo()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			InitAppBar();

			Settings = X.Instance<IMemberInfo>( XProto.MemberInfo );

			UserInfo.DataContext = Settings;
			Sign.Text = Settings.Signature;

			Settings.PropertyChanged += PropertyChanged;
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "Settings", "Message", "ContextMenu" );
			AppBarButton LogoutBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.ChevronLeft, stx.Text( "Account_Logout" ) );
			LogoutBtn.Click += async ( s, e ) =>
			{
				bool Yes = false;
				await Popups.ShowDialog( UIAliases.CreateDialog(
					stx.Str( "ConfirmLogout", "Message" )
					, () => Yes = true
					, stx.Str( "Yes", "Message" ), stx.Str( "No", "Message" )
				) );

				if ( Yes )
				{
					ControlFrame.Instance.CommandMgr.WLogout();
					ControlFrame.Instance.BackStack.Remove( PageId.W_USER_INFO );
					ControlFrame.Instance.GoBack();
				}
			};

			MajorControls = new ICommandBarElement[] { LogoutBtn };
		}

		private void PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
		{
			Settings.PropertyChanged -= PropertyChanged;
			InfoBubble.IsActive = false;
		}

		private async void Sign_LostFocus( object sender, RoutedEventArgs e )
		{
			string Sig = Sign.Text.Trim();

			if ( await new global::GR.SelfCencorship().Passed( Sig ) )
			{
				Settings.Signature = Sig;
			}
			else
			{
				Sign.Focus( FocusState.Keyboard );
			}
		}

	}
}