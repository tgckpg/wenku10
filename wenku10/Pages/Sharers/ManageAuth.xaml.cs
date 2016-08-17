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

using wenku8.Model.Loaders;
using wenku8.Model.ListItem;
using wenku8.Model.ListItem.Sharers;
using wenku8.Section;

namespace wenku10.Pages.Sharers
{
    using CryptAES = wenku8.System.CryptAES;
    using RSAManager = wenku8.System.RSAManager;
    using AESManager = wenku8.System.AESManager;
    using TokenManager = wenku8.System.TokenManager;
    using SHTarget = wenku8.Model.REST.SharersRequest.SHTarget;

    sealed partial class ManageAuth : Page
    {
        private SharersHub ShHub;
        private RSAManager RSAMgr;
        private AESManager AESMgr;
        private TokenManager TokMgr;

        new Frame Frame { get; set; }

        private AuthItem SelectedItem;

        public ManageAuth( SharersHub ShHub, Frame PopupFrame )
        {
            this.InitializeComponent();
            this.ShHub = ShHub;
            ShHub.PropertyChanged += ShHub_PropertyChanged;

            Frame = PopupFrame;
            SetTemplate();
        }

        private void ShHub_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == "Loading" )
            {
                if ( !ShHub.Loading )
                {
                    RequestsList.ItemsSource = ShHub.Grants.Remap( x => new GrantProcess( x ) );
                }
            }
        }

        private async void SetTemplate()
        {
            StringResources stx = new StringResources( "AppResources", "ContextMenu", "WMessage", "LoadingMessage" );
            KeysSection.Header = stx.Text( "Secret" );
            TokensSection.Header = stx.Text( "AccessTokens", "ContextMenu" );
            RequestsSection.Header = stx.Text( "Requests" );

            if ( !ShHub.Member.IsLoggedIn )
            {
                ReqPlaceholder.Text = stx.Str( "4", "WMessage" );
            }
            else
            {
                ReqPlaceholder.Text = stx.Str( "ProgressIndicator_PleaseWait", "LoadingMessage" );
                ShHub.GetMyRequests( () =>
                {
                    ReqPlaceholder.Visibility = Visibility.Collapsed;
                    RequestsList.ItemsSource = ShHub.Grants.Remap( x => new GrantProcess( x ) );
                } );
            }

            RSAMgr = await RSAManager.CreateAsync();

            AESMgr = new AESManager();
            ReloadAuths( KeyList, SHTarget.KEY, AESMgr );

            TokMgr = new TokenManager();
            ReloadAuths( TokenList, SHTarget.TOKEN, TokMgr );
        }

        private void ShowContextMenu( object sender, RightTappedRoutedEventArgs e )
        {
            Border B = ( Border ) sender;
            SelectedItem = ( AuthItem ) B.DataContext;

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

        public void GotoRequests() { MasterPivot.SelectedItem = RequestsSection; }

        private void ParseGrant( object sender, RoutedEventArgs e )
        {
            ( ( GrantProcess ) ( ( Button ) sender ).DataContext ).Parse( RSAMgr.AuthList );
        }

        private async void WithdrawRequest( object sender, RoutedEventArgs e )
        {
            GrantProcess GP = ( GrantProcess ) ( ( Button ) sender ).DataContext;
            if ( await GP.Withdraw() )
            {
                RequestsList.ItemsSource = ( ( IEnumerable<GrantProcess> ) RequestsList.ItemsSource )
                    .Where( x => x != GP );
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

        private void ReloadAuths<T>( ListView LView, SHTarget Target, wenku8.System.AuthManager<T> Mgr )
        {
            LView.ItemsSource = Mgr.AuthList.Remap( x =>
            {
                NameValue<string> NX = x as NameValue<string>;
                AuthItem Item = new AuthItem( NX, Target );
                Item.Count = Mgr.ControlCount( NX.Value );
                return Item;
            } );
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
            string Tag = ( string ) Btn.Tag;
            IStorageFile ISF = await AppStorage.SaveFileAsync( "wenku10 Auth", new List<string>() { ".xml" }, Tag );
            if ( ISF == null ) return;

            try
            {
                using ( Stream s = await ISF.OpenStreamForWriteAsync() )
                {
                    await wenku8.Resources.Shared.Storage.GetStream(
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
    }
}