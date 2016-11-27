using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku10;
using wenku10.Pages;
using wenku10.Pages.Dialogs;
using wenku10.Pages.Sharers;

namespace wenku8.System
{
    using CompositeElement;
    using Config;
    using Ext;
    using Model.ListItem;
    using Model.Loaders;
    using Resources;

    sealed class MasterCommandManager
    {
        private IObservableVector<ICommandBarElement> CommandList;
        private IObservableVector<ICommandBarElement> SecondCmdList;

        private ICommandBarElement[] MasterCommands;
        private ICommandBarElement[] M2ndCommands;

        private ICommandBarElement[] SHCommands;
        private ICommandBarElement[] SH2ndCommands;

        private ICommandBarElement[] w8Commands;
        private ICommandBarElement[] w82ndCommands;

        private ICommandBarElement[] CommonCommands;
        private ICommandBarElement[] SystemCommands;

        private StringResources stx = new StringResources( "AppResources", "Settings", "ContextMenu", "AppBar", "NavigationTitles" );

        SecretSwipeButton AboutBtn;

        private volatile bool Unlocked = false;
        private int InitMode = 0;

        public MasterCommandManager( IObservableVector<ICommandBarElement> CommandList, IObservableVector<ICommandBarElement> SecondCmdList )
        {
            this.CommandList = CommandList;
            this.SecondCmdList = SecondCmdList;
            DefaultCmds();
        }

        private void DefaultCmds()
        {
            SHMember = X.Singleton<IMember>( XProto.SHMember );
            SHMember.OnStatusChanged += SHMember_OnStatusChanged;

            // Init SHListener
            new wenku10.ShHub.MesgListerner();

            CreateSystemCommands();
            CreateSHCommands();
            CreateCommonCommands();

            InitCommands();
        }

        private void CreateCommonCommands()
        {
            SecondaryIconButton HistoryBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.History, stx.Text( "History", "NavigationTitles" ) );
            HistoryBtn.Click += CreateCmdHandler( PageId.HISTORY, () => new wenku10.Pages.History() );

            CommonCommands = new ICommandBarElement[] { HistoryBtn };
        }

        private void CreateSystemCommands()
        {
            List<ICommandBarElement> Btns = new List<ICommandBarElement>();

            SecondaryIconButton SettingsBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Settings, stx.Text( "Settings", "AppBar" ) );
            SettingsBtn.Click += CreateCmdHandler( PageId.MAIN_SETTINGS, () => new global::wenku10.Pages.Settings.MainSettings() );

            AboutBtn = new SecretSwipeButton( SegoeMDL2.Info )
            {
                Label = stx.Text( "About", "AppBar" ),
                Label2 = Properties.SMODE == 0 ? "wenku8" : "Grimoire",
                Glyph2 = SegoeMDL2.Accept
            };

            AboutBtn.PendingClick += CreateCmdHandler( PageId.ABOUT, () => new About() );
            AboutBtn.OnIndexUpdate += ( s, i ) => SwapCommands( i );

            Btns.Add( SettingsBtn );
            Btns.Add( AboutBtn );

            if ( MainStage.Instance.IsPhone )
            {
                SecondaryIconButton ExitBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.ChromeClose, stx.Text( "Exit", "AppBar" ) );
                ExitBtn.Click += ( s, e ) =>
                {
                    Windows.ApplicationModel.Core.CoreApplication.Exit();
                };
                Btns.Add( ExitBtn );
            }

            SystemCommands = Btns.ToArray();
        }

        private void CreateSHCommands()
        {
            SHLoginBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.ChevronRight, stx.Text( "Login" ) );
            SHLoginBtn.Click += CreateCmdHandler( SHLoginBtn_Click );

            AppBarToggleButton ZoneSpiders = UIAliases.CreateToggleBtn( SegoeMDL2.MapLayers, stx.Text( "ZoneSpider" ) );
            ZoneSpiders.Click += CreateCmdHandler( PageId.ZONE_SPIDER_VIEW, () => new ZoneSpidersView() );

            AppBarToggleButton LocalTextDocs = UIAliases.CreateToggleBtn( SegoeMDL2.TreeFolderFolder, stx.Text( "LocalDocuments", "AppBar" ) );
            LocalTextDocs.Click += CreateCmdHandler( PageId.LOCAL_DOCS_VIEW, () => new LocalDocumentsView() );

            AppBarToggleButton BookSpiders = UIAliases.CreateToggleBtn( SegoeMDL2.MapPin, stx.Text( "BookSpider" ) );
            BookSpiders.Click += CreateCmdHandler( PageId.BOOK_SPIDER_VIEW, () => new BookSpidersView() );

            AppBarToggleButton OnlineScriptDir = UIAliases.CreateToggleBtn( SegoeMDL2.HomeGroup, stx.Text( "OnlineScriptDir", "AppBar" ) );
            OnlineScriptDir.Click += CreateCmdHandler( PageId.ONLINE_SCRIPTS_VIEW, () => new OnlineScriptsView() );

            SecondaryIconButton SpiderEditor = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Edit, stx.Text( "SpiderEdit", "ContextMenu" ) );
            SpiderEditor.Click += CreateCmdHandler( PageId.PROC_PANEL, () => new ProcPanelWrapper() );

            SHCommands = new ICommandBarElement[] { LocalTextDocs, ZoneSpiders, BookSpiders, OnlineScriptDir };
            SH2ndCommands = new ICommandBarElement[] { SHLoginBtn, SpiderEditor };
        }

        private void InitCommands()
        {
            CommandList.Clear();

            MasterCommands = SHCommands;
            M2ndCommands = SH2ndCommands;

            foreach ( ICommandBarElement Btn in MasterCommands )
                CommandList.Add( Btn );

            InitMode = Properties.SMODE;
            SwapCommands( 0 );
        }

        private RoutedEventHandler CreateCmdHandler( string Name, Func<Page> ViewFunc )
        {
            return ( s, e ) =>
            {
                ToggleButtons( ( AppBarToggleButton ) s );
                ControlFrame.Instance.NavigateTo( Name, ViewFunc );
            };
        }

        private RoutedEventHandler CreateCmdHandler( RoutedEventHandler Handler )
        {
            return ( s, e ) =>
            {
                ToggleButtons( ( AppBarToggleButton ) s );
                Handler( s, e );
            };
        }

        private void UnlockSecret()
        {
            if ( Unlocked ) return;
            Unlocked = true;

            new Bootstrap().Level2();

            WMember = X.Singleton<IMember>( XProto.Member );
            WMember.OnStatusChanged += WMember_OnStatusChanged;

            WLoginBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.ChevronRight, stx.Text( "Login" ) );
            WLoginBtn.Click += CreateCmdHandler( WLoginBtn_Click );

            SecondaryIconButton SearchBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Search, stx.Text( "Search", "AppBar" ) );
            SearchBtn.Click += CreateCmdHandler( PageId.W_SEARCH, () => new WSearch() );

            w82ndCommands = new ICommandBarElement[] { WLoginBtn, SearchBtn };

            // Master commands
            INavSelections NavSeletions = X.Instance<INavSelections>( XProto.NavSelections );
            SubtleUpdateItem CustSections = NavSeletions.CustomSection();

            AppBarToggleButton MoreNavs = UIAliases.CreateToggleBtn( Symbol.AllApps, stx.Text( "Appearance_Layout_NavPages", "Settings" ) );
            MoreNavs.Click += CreateCmdHandler( PageId.W_NAV_SEL, () => new WNavSelections( NavSeletions ) );

            AppBarToggleButton CustBtn = UIAliases.CreateToggleBtn( SegoeMDL2.Asterisk, CustSections.Name );
            CustBtn.Click += CreateCmdHandler( PageId.W_NAV_LIST + CustSections.Name, () => new WNavList( CustSections ) );

            AppBarToggleButton BookShelfBtn = UIAliases.CreateToggleBtn( Symbol.Library, stx.Text( "Shelf", "AppBar" ) );
            BookShelfBtn.Click += CreateCmdHandler( PageId.W_BOOKSHELF, () => new WBookshelf() );

            w8Commands = new ICommandBarElement[] { BookShelfBtn, CustBtn, MoreNavs };
        }

        private void SwapCommands( int Index )
        {
            if( InitMode == 1 )
                Index = Index == 0 ? 1 : 0;

            if ( Index == 0 )
            {
                ActionEvent.Normal();
                Properties.SMODE = Index;

                MasterCommands = SHCommands;
                M2ndCommands = SH2ndCommands;

                ControlFrame.Instance.SetHomePage( PageId.SG_SH, () => new SuperGiants( new SHSLActiveItem( "", null ) ) );
            }
            else
            {
                ActionEvent.Secret();
                Properties.SMODE = Index;

                UnlockSecret();

                MasterCommands = w8Commands;
                M2ndCommands = w82ndCommands;

                ControlFrame.Instance.SetHomePage( PageId.SG_W, () => new SuperGiants( X.Instance<ILoader<ActiveItem>>( XProto.StaffPicks ) ) );
                // SetHomePage( PageId.BOOK_SPIDER_VIEW, () => new BookSpidersView() );
            }
        }

        public void Set2ndCommands( IList<ICommandBarElement> Commands )
        {
            SecondCmdList.Clear();

            if ( Commands != null && 0 < Commands.Count )
            {
                foreach ( ICommandBarElement e in Commands ) SecondCmdList.Add( e );
                SecondCmdList.Add( new AppBarSeparator() );
            }

            if ( 0 < M2ndCommands.Length )
            {
                foreach ( ICommandBarElement e in M2ndCommands ) SecondCmdList.Add( e );
                SecondCmdList.Add( new AppBarSeparator() );
            }

            foreach ( ICommandBarElement e in CommonCommands ) SecondCmdList.Add( e );

            SecondCmdList.Add( new AppBarSeparator() );
            foreach ( ICommandBarElement e in SystemCommands ) SecondCmdList.Add( e );
        }

        public void SetMajorCommands( IList<ICommandBarElement> Controls, bool MajorNav )
        {
            CommandList.Clear();

            if ( MajorNav )
            {
                foreach ( ICommandBarElement Btn in MasterCommands )
                    CommandList.Add( Btn );
            }

            if ( Controls != null )
            {
                if ( MajorNav )
                    CommandList.Add( new AppBarSeparator() );

                foreach ( ICommandBarElement Btn in Controls )
                    CommandList.Add( Btn );
            }
        }

        private void ToggleButtons( AppBarToggleButton s )
        {
            foreach ( AppBarToggleButton Btn in MasterCommands.Where( x => x != s ) ) Btn.IsChecked = false;
            s.IsChecked = true;
        }

        #region SHLoginBtn
        private IMember SHMember;
        private SecondaryIconButton SHLoginBtn;

        private void SHLoginBtn_Click( object sender, RoutedEventArgs e ) { SHLoginOrInfo(); }

        private void SHMember_OnStatusChanged( object sender, MemberStatus args )
        {
            if ( args == MemberStatus.LOGGED_IN )
            {
                SHLoginBtn.Label = stx.Text( "Account", "Settings" );
                SHLoginBtn.Glyph = SegoeMDL2.Accounts;
            }
            else
            {
                SHLoginBtn.Label = stx.Text( "Login" );
                SHLoginBtn.Glyph = SegoeMDL2.ChevronRight;
            }
        }

        public async Task<bool> Authenticate()
        {
            if ( !SHMember.IsLoggedIn )
            {
                Login LoginDialog = new Login( SHMember );
                await Popups.ShowDialog( LoginDialog );
                return !LoginDialog.Canceled;
            }

            return true;
        }

        public void SHLogout() { SHMember.Logout(); }
        private async void SHLoginOrInfo()
        {
            if ( SHMember.WillLogin ) return;
            if ( SHMember.IsLoggedIn )
            {
                ControlFrame.Instance.NavigateTo( PageId.SH_USER_INFO, () => new UserInfo() );
            }
            else
            {
                Login LoginDialog = new Login( SHMember );
                await Popups.ShowDialog( LoginDialog );
            }
        }
        #endregion

        #region WLogin
        private IMember WMember;
        private SecondaryIconButton WLoginBtn;

        private void WLoginBtn_Click( object sender, RoutedEventArgs e ) { WLoginOrInfo(); }
        private void WMember_OnStatusChanged( object sender, MemberStatus args )
        {
            if ( args == MemberStatus.LOGGED_IN )
            {
                WLoginBtn.Label = stx.Text( "Account", "Settings" );
                WLoginBtn.Glyph = SegoeMDL2.Accounts;
            }
            else
            {
                WLoginBtn.Label = stx.Text( "Login" );
                WLoginBtn.Glyph = SegoeMDL2.ChevronRight;
            }
        }

        public async Task<bool> WAuthenticate()
        {
            if ( !WMember.IsLoggedIn )
            {
                Login LoginDialog = new Login( WMember );
                await Popups.ShowDialog( LoginDialog );
                return !LoginDialog.Canceled;
            }

            return true;
        }

        public void WLogout() { WMember.Logout(); }
        private async void WLoginOrInfo()
        {
            if ( WMember.WillLogin ) return;
            if ( WMember.IsLoggedIn )
            {
                ControlFrame.Instance.NavigateTo( PageId.W_USER_INFO, () => new WUserInfo() );
            }
            else
            {
                Login LoginDialog = new Login( WMember );
                await Popups.ShowDialog( LoginDialog );
            }
        }
        #endregion

    }
}