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
using Net.Astropenguin.Logging;

using wenku8.Config;
using wenku8.Ext;

namespace wenku10.Pages.Settings.Advanced
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ServerSelector : Page
    {
        public static readonly string ID = typeof( ServerSelector ).Name;
        public ServerSelector()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            ToggleSettings( EnableSS.IsOn = Properties.ENABLE_SERVER_SEL );
            MaxPing.Value = Properties.SERVER_MAX_PING;

            RefreshServers();
        }

        private void ToggleSS( object sender, RoutedEventArgs e )
        {
            ToggleSettings( Properties.ENABLE_SERVER_SEL = EnableSS.IsOn );
        }

        private void MaxPing_ValueChanged( object sender, RangeBaseValueChangedEventArgs e )
        {
            Properties.SERVER_MAX_PING = ( int ) MaxPing.Value;
        }

        private void MaxPing_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
        {
            Properties.SERVER_MAX_PING = ( int ) MaxPing.Value;
            RefreshServers();
        }

        private void RefreshServers()
        {
            IRuntimeCache wc = X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, false );
            wc.GET(
                new Uri( X.Const<string>( XProto.WProtocols, "APP_PROTOCOL" ) + "server.list" )
                , GotServerList, global::wenku8.System.Utils.DoNothing, true );

        }

        private async void GotServerList( DRequestCompletedEventArgs e, string key )
        {
            List<ServerChoice> SC = new List<ServerChoice>();

            throw new NotImplementedException( "ServerSelector ?" );
            try
            {
                /*
                await global::wenku8.System.ServerSelector.ProcessList( e.ResponseString );
                foreach( Weight<string> W in global::wenku8.System.ServerSelector.ServerList )
                {
                    SC.Add( new ServerChoice( W ) );
                }
                */
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }

            AvailableServers.ItemsSource = SC;
        }

        private void ToggleSettings( bool b )
        {
            MaxPing.IsEnabled = b;
            AvailableServers.IsEnabled = b;
        }

        private class ServerChoice : global::wenku8.Model.ListItem.ActiveItem
        {
            public ServerChoice( Weight<string> Server )
                :base( Server.Freight, Server.Factor.ToString(), null )
            {

            }
        }
    }
}
