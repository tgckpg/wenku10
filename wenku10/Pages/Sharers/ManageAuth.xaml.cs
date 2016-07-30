using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Linq;

using wenku8.Model.Loaders;
using wenku8.Model.ListItem;
using wenku8.Section;

namespace wenku10.Pages.Sharers
{
    using RSAManager = wenku8.System.RSAManager;
    using TokenManager = wenku8.System.TokenManager;

    sealed partial class ManageAuth : Page
    {
        private SharersHub ShHub;
        private RSAManager RSAMgr;

        new Frame Frame { get; set; }

        public ManageAuth( SharersHub ShHub, Frame PopupFrame )
        {
            this.InitializeComponent();
            this.ShHub = ShHub;

            Frame = PopupFrame;
            SetTemplate();
        }

        private async void SetTemplate()
        {
            RSAMgr = await RSAManager.CreateAsync();
            RequestsList.ItemsSource = ShHub.Grants.Remap( x => new GrantProcess( x ) );
        }

        private void ParseGrant( object sender, RoutedEventArgs e )
        {
            ( ( GrantProcess ) ( ( Button ) sender ).DataContext ).Parse( RSAMgr.AuthList );
        }

        private async void GotoScriptDetail( object sender, ItemClickEventArgs e )
        {
            GrantProcess GProc = ( GrantProcess ) e.ClickedItem;
            if ( GProc.IsLoading ) return;

            GProc.IsLoading = true;
            string AccessToken = new TokenManager().GetAuthById( GProc.ScriptId ).Value;

            SHSearchLoader SHLoader = new SHSearchLoader(
                "uuid: " + GProc.ScriptId
                , AccessToken == null ? null : new string[] { AccessToken }
            );
            IList<HubScriptItem> HSIs = await SHLoader.NextPage();
            HubScriptItem HSI = HSIs.FirstOrDefault();

            if ( HSI != null )
            {
                NavigatedEventHandler Frame_Navigated = null;
                Frame_Navigated = ( s, e2 ) =>
                {
                    Frame.Navigated -= Frame_Navigated;

                    ScriptDetails SDetails = ( ScriptDetails ) Frame.Content;
                    RoutedEventHandler SDetails_Loaded = null;

                    SDetails_Loaded = ( s2, e3 ) =>
                    {
                        SDetails.Loaded -= SDetails_Loaded;
                        SDetails.OpenRequest( GProc.Target );
                    };

                    SDetails.Loaded += SDetails_Loaded;
                };

                Frame.Navigated += Frame_Navigated;
                Frame.Navigate( typeof( ScriptDetails ), HSI );
            }
        }

    }
}
