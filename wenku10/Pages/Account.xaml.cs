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

using wenku8.Ext;

namespace wenku10.Pages
{
    public sealed partial class Account : Page
    {
        private IMemberInfo Settings;
        private Action Close;

        private Account()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        public Account( Action Close )
            :this()
        {
            this.Close = Close;
        }

        private void SetTemplate()
        {
            Settings = X.Instance<IMemberInfo>( XProto.MemberInfo );
            UserInfo.DataContext = Settings;
            Sign.Text = Settings.Signature;

            Settings.PropertyChanged += PropertyChanged;
        }

        private void PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
        {
            Settings.PropertyChanged -= PropertyChanged;
            InfoBubble.IsActive = false;
        }

        private async void Sign_LostFocus( object sender, RoutedEventArgs e )
        {
            string Sig = Sign.Text.Trim();

            if ( await new global::wenku8.SelfCencorship().Passed( Sig ) )
            {
                Settings.Signature = Sig;
            }
            else
            {
                Sign.Focus( FocusState.Keyboard );
            }
        }

        private void LogoutTapped( object sender, TappedRoutedEventArgs e )
        {
            X.Singleton<IMember>( XProto.Member ).Logout();
            Close();
        }

        private void Cancel( object sender, TappedRoutedEventArgs e )
        {
            Close();
        }
    }
}