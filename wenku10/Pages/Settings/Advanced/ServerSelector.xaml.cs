using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.Config;
using wenku8.Ext;
using wenku8.Settings;

namespace wenku10.Pages.Settings.Advanced
{
    public sealed partial class ServerSelector : Page
    {
        public static readonly string ID = typeof( ServerSelector ).Name;

        public XRegistry ServerReg;

        public ServerSelector()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            ServerReg = new XRegistry( "<Server />", FileLinks.ROOT_SETTING + "Server.xml" );
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
            try
            {

                IRuntimeCache wc = X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, false );
                wc.GET(
                    new Uri( X.Const<string>( XProto.WProtocols, "APP_PROTOCOL" ) + "server.list" )
                    , GotServerList, global::wenku8.System.Utils.DoNothing, true );
            }
            catch ( DllNotFoundException ex )
            {
                Logger.Log( ID, "Protocol not present: " + ex.Message, LogType.INFO );
                EnableSS.IsEnabled = false;
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.WARNING );
            }
        }

        private void GotServerList( DRequestCompletedEventArgs e, string key )
        {
            IEnumerable<ServerChoice> SC = null;

            XParameter[] Params = ServerReg.GetParametersWithKey( "uri" );

            try
            {
                IEnumerable<string> Servers = X.Call<IEnumerable<string>>( XProto.ServerSelector, "ExtractList", e.ResponseString );

                SC = Servers.Remap( x =>
                {
                    string[] s = x.Split( new char[] { ',' } );
                    return new ServerChoice( s[ 0 ], s[ 1 ] );
                } );

                var j = Task.Run( async () =>
                {
                    await X.Call<Task>( XProto.ServerSelector, "ProcessList", Servers );

                    foreach( ServerChoice C in SC )
                    {
                        C.Preferred = X.Static<IEnumerable<Weight<string>>>( XProto.ServerSelector, "ServerList" ).Any( x =>
                        {
                            if ( x.Freight == C.Name )
                            {
                                C.Desc = x.Factor + "";
                                return true;
                            }

                            return false;
                        } );

                        C.IsLoading = false;
                    }
                } );
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }

            AvailableServers.ItemsSource = SC;
        }

        private void ToggleSettings( bool b )
        {
            ServiceUri.IsEnabled
                = AddUri.IsEnabled
                = MaxPing.IsEnabled
                = AvailableServers.IsEnabled
                = ServerReset.IsEnabled
                = b;
        }

        private void AddServer( object sender, KeyRoutedEventArgs e )
        {
            if( e.Key == Windows.System.VirtualKey.Enter )
            {
                AddServer();
            }
        }

        private void AddServer( object sender, RoutedEventArgs e ) { AddServer(); }

        private async void AddServer()
        {
            string SrvUri = ServiceUri.Text.Trim();
            if ( string.IsNullOrEmpty( SrvUri ) ) return;

            try
            {
                new Uri( SrvUri );
            }
            catch( Exception )
            {
                await Popups.ShowDialog( new MessageDialog( "Invalid Uri" ) );
                return;
            }

            XParameter Param = new XParameter( SrvUri );
            Param.SetValue( new XKey( "uri", 1 ) );
            ServerReg.SetParameter( Param );
            ServerReg.Save();

            ServiceUri.Text = "";

            RefreshServers();
        }

        private void ResetServer( object sender, RoutedEventArgs e )
        {
            ServerReg = new XRegistry( "<Server />", FileLinks.ROOT_SETTING + "Server.xml", false );
            ServerReg.Save();

            RefreshServers();
        }

        private void RemoveServer( object sender, RoutedEventArgs e )
        {
            Button B = sender as Button;
            if ( B == null ) return;

            ServerChoice C = B.DataContext as ServerChoice;
            XParameter Param = new XParameter( C.Name );
            Param.SetValue( new XKey( "disabled", true ) );
            ServerReg.SetParameter( Param );
            ServerReg.Save();

            RefreshServers();
        }

        private class ServerChoice : global::wenku8.Model.ListItem.ActiveItem
        {
            private bool _preferred = false;
            public bool Preferred
            {
                get { return _preferred; }
                set
                {
                    _preferred = value;
                    NotifyChanged( "Preferred" );
                }
            }

            private bool _loading = true;
            public bool IsLoading
            {
                get { return _loading; }
                set
                {
                    _loading = value;
                    NotifyChanged( "IsLoading" );
                }
            }

            public ServerChoice( string ServiceUri, string Rank )
                :base( ServiceUri, "", null )
            {
            }
        }

        private class CommandCopy : INamable
        {
            public string Name
            {
                get; set;
            }
        }

        private async void ShowCommand( Hyperlink sender, HyperlinkClickEventArgs args )
        {
            Logger.Log( ID, AppSettings.FamilyName, LogType.DEBUG );
            StringResources stx = new StringResources( "Settings" );
            await Popups.ShowDialog(
                new Dialogs.Rename(
                    new CommandCopy() { Name = "CheckNetIsolation LoopbackExempt -a -n=" + AppSettings.FamilyName }
                    , stx.Text( "Advanced_Server_Exempt_Command" ) , true ) );
        }
    }
}
