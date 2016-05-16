using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.UI;

using wenku8.Config;
using wenku8.Model.ListItem;

namespace wenku10.Pages.Settings
{
    public sealed partial class MainSettings : Page
    {
        public static readonly string ID = typeof( MainSettings ).Name;

        public static MainSettings Instance;
        public MainSettings()
        {
            this.InitializeComponent();
            Instance = this;
            DefineSettings();
        }

        ~MainSettings() { Dispose(); }

        private void Dispose()
        {
            NavigationHandler.OnNavigatedBack -= ClosePopup;
            Instance = null;
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );
            NavigationHandler.InsertHandlerOnNavigatedBack( ClosePopup );

            // Reset Cache
            Frame RootFrame = MainStage.Instance.RootFrame;
            int OSize = RootFrame.CacheSize;
            Logger.Log( ID, string.Format( "Resetting Page Cache({0})", OSize ), LogType.DEBUG );

            RootFrame.CacheSize = 0;
            RootFrame.CacheSize = OSize;
        }

        private void ClosePopup( object sender, XBackRequestedEventArgs e )
        {
            // Restart Required
            if ( RestartMask.State == ControlState.Reovia ) return;

            // Close the popup first
            if ( PopupPage.State == ControlState.Reovia )
            {
                PopupPage.State = ControlState.Foreatii;
                e.Handled = true;
                return;
            }

            // Go back
            LoadingMask.HandleBack( Frame, e );
            Dispose();
        }

        ActionItem OneDriveButton;

        public void DefineSettings()
        {
            StringResources stx = new StringResources( "Settings" );

            string CurrentLang = Properties.LANGUAGE;
            SettingsSection LangSection = new SettingsSection()
            {
                Title = stx.Text( "Language" )
                , Data = new ActiveItem[]
                {
                    new ActionItem(
                        stx.Text( "Language_E")
                        , CurrentLang == "en-US"
                            ? stx.Text( "Desc_Language_C" )
                            : stx.Text( "Desc_Language_AE" )
                        , "en-US"
                    )
                    , new ActionItem(
                        stx.Text( "Language_T")
                        , CurrentLang == "zh-TW"
                            ? stx.Text( "Desc_Language_C" )
                            : stx.Text( "Desc_Language_AT" )
                        , "zh-TW"
                    )
                    , new ActionItem(
                        stx.Text( "Language_S")
                        , CurrentLang == "zh-CN"
                            ? stx.Text( "Desc_Language_C" )
                            : stx.Text( "Desc_Language_AS" )
                        , "zh-CN"
                    )
                    , new ActionItem(
                        stx.Text( "Language_J")
                        , CurrentLang == "ja"
                            ? stx.Text( "Desc_Language_C" )
                            : stx.Text( "Desc_Language_AJ" )
                        , "ja"
                    )
                }
                , ItemAction = ChangeLanguage
                , IsEnabled = true
            };

            if ( MainStage.Instance.IsPhone )
            {
                if ( CurrentLang != "en-US" ) LangSection.Data.ElementAt( 0 ).Desc = "Mobile user may not be able change the language here, please visit the wiki for help";
                if ( CurrentLang != "zh-TW" ) LangSection.Data.ElementAt( 1 ).Desc = "\u624B\u6A5F\u7528\u6236\u53EF\u80FD\u7121\u6CD5\u8B8A\u66F4\u8A9E\u8A00\uFF0C\u8A73\u60C5\u8ACB\u53C3\u770B\u5E6B\u52A9";
                if ( CurrentLang != "zh-CN" ) LangSection.Data.ElementAt( 2 ).Desc = "\u624B\u673A\u7528\u6237\u53EF\u80FD\u65E0\u6CD5\u53D8\u66F4\u8BED\u8A00\uFF0C\u8BE6\u60C5\u8BF7\u53C2\u770B\u5E2E\u52A9";
                if ( CurrentLang != "ja" ) LangSection.Data.ElementAt( 3 ).Desc = "\u643A\u5E2F\u96FB\u8A71\u30E6\u30FC\u30B6\u30FC\u306F\u3001\u8A00\u8A9E\u3092\u5909\u66F4\u3067\u304D\u306A\u3044\u5834\u5408\u304C\u3042\u308A\u307E\u3059\u3002\u8A73\u7D30\u306B\u3064\u3044\u3066\u306F\u3001\u30D8\u30EB\u30D7\u3092\u53C2\u7167\u3057\u3066\u304F\u3060\u3055\u3044\u3002";
            }

            MainView.ItemsSource = new SettingsSection[]
            {
                new SettingsSection()
                {
                    Title = stx.Text( "Storage" )
                    , Data = new ActiveItem[]
                    {
                        new ActionItem( stx.Text( "Data_Cache"), stx.Text( "Desc_Data_Cache" ), typeof( Data.Cache ) )
                        , new ActionItem( stx.Text( "Data_Illustration"), stx.Text( "Desc_Data_Illustration" ), typeof( Data.Illustration ) )
                        , new ActionItem( stx.Text( "Data_Preload"), stx.Text( "Desc_Data_Preload" ), typeof( Data.Preload ) )
                        , new ActionItem( stx.Text( "EBWin"), stx.Text( "Desc_EBWin_Short" ), typeof( Data.EBWin ) )
                        , OneDriveButton = new ActionItem( "OneDrive", Properties.ENABLE_ONEDRIVE ? stx.Text( "Enabled" ) : stx.Text( "Disabled" ), false )
                        // , new ActionItem( stx.Text( "Data_Connection"), stx.Text( "Desc_Data_Connection" ), typeof( Data.Cache ) )
                    }
                    , ItemAction = PopupSettings
                    , IsEnabled = true
                }
                , new SettingsSection()
                {
                    Title = stx.Text( "Appearance" )
                    , Data = new ActiveItem[]
                    {
                        new ActionItem( stx.Text( "Appearance_ContentReader"), stx.Text( "Desc_Appearance_ContentReader" ), typeof( Themes.ContentReader ) )
                        , new ActionItem( stx.Text( "Appearance_Theme"), stx.Text( "Desc_Appearance_Backgrounds" ), typeof( Themes.ThemeColors ) )
                        , new ActionItem( stx.Text( "Appearance_Layout"), stx.Text( "Desc_Appearance_Layout" ), typeof( Themes.Layout ) )
                    }
                    , ItemAction = PopupSettings
                    , IsEnabled = true
                }
                , LangSection
                , new SettingsSection()
                {
                    Title = stx.Text( "Advanced" )
                    , Data = new ActiveItem[]
                    {
                        new ActionItem( stx.Text( "Advanced_Server"), stx.Text( "Desc_Advanced_Server" ), typeof( Advanced.ServerSelector ) )
                        , new ActionItem( stx.Text( "Advanced_Misc"), stx.Text( "Desc_Advanced_Misc" ), typeof( Advanced.Misc ) )
#if DEBUG || TESTING 
                        , new ActionItem( stx.Text( "Advanced_Debug"), stx.Text( "Desc_Advanced_Debug" ), typeof( Advanced.Debug ) )
#endif
                    }
                    , ItemAction = PopupSettings
                    , IsEnabled = true
                }
                , new SettingsSection()
                {
                    Title = stx.Text( "Help" )
                    , Data = new ActiveItem[]
                    {
                        new ActionItem( stx.Text( "Help_Wiki"), stx.Text( "Desc_Help_Wiki" ), "https://github.com/tgckpg/wenku10/wiki" )
                        , new ActionItem( stx.Text( "Help_Slack"), stx.Text( "Desc_Help_Slack" ), "https://blog.astropenguin.net/article/view/wenku10-%E7%9A%84%E8%A8%8E%E8%AB%96%E7%B5%84/" )
                    }
                    , ItemAction = HelpAction
                    , IsEnabled = true
                }
            };
        }

        private void HelpAction( object Param )
        {
            Uri _url = new Uri( Param.ToString() );
            var j = Windows.System.Launcher.LaunchUriAsync( _url );
        }


        private async void ChangeLanguage( object Param )
        {
            string LangCode = Param.ToString();
            if ( Properties.LANGUAGE == LangCode ) return;

            if ( !await ConfirmRestart( "Language" ) ) return;

            Properties.LANGUAGE_TRADITIONAL = LangCode == "zh-TW";
            Properties.LANGUAGE = LangCode;
        }

        public async Task<bool> ConfirmRestart( string CaptionRes )
        {
            StringResources stx = new StringResources( "Settings" );
            StringResources stm = new StringResources( "Message" );

            // Ask for confirmatiosn
            MessageDialog Confirm = new MessageDialog( stm.Str( "NeedRestart" ), stx.Text( CaptionRes ) );

            bool Restart = false;

            Confirm.Commands.Add(
                new UICommand(
                    stm.Str( "Yes" )
                    , ( e ) => { Restart = true; }
                )
            );

            Confirm.Commands.Add(
                new UICommand( stm.Str( "No" ) )
            );

            await Popups.ShowDialog( Confirm );

            if( Restart )
            {
                Frame.BackStack.Clear();
                NavigationHandler.InsertHandlerOnNavigatedBack( Exit );
                RestartMask.State = ControlState.Reovia;
                PopupPage.State = ControlState.Foreatii;
            }

            return Restart;
        }

        private void ListView_ItemClick( object sender, ItemClickEventArgs e )
        {
            SettingsSection SettingsContext = ( sender as FrameworkElement ).DataContext as SettingsSection;
            ActionItem Item = ( ActionItem ) e.ClickedItem;

            SettingsContext.ItemAction( Item.Param );
        }

        private async void PopupSettings( object P )
        {
            if( P.GetType() == typeof( bool ) )
            {
                StringResources sts = new StringResources( "Settings" );
                if ( !Properties.ENABLE_ONEDRIVE )
                {
                    StringResources stx = new StringResources( "InitQuestions" );
                    StringResources stm = new StringResources( "Message" );
                    MessageDialog Msg = new MessageDialog( stx.Text( "EnableOneDrive" ), "OneDrive" );
                    Msg.Commands.Add(
                        new UICommand( stm.Str( "Yes" ), ( x ) => Properties.ENABLE_ONEDRIVE = true )
                    );
                    Msg.Commands.Add(
                        new UICommand( stm.Str( "No" ), ( x ) => Properties.ENABLE_ONEDRIVE = false )
                    );

                    await Popups.ShowDialog( Msg );

                    if ( Properties.ENABLE_ONEDRIVE )
                    {
                        if ( global::wenku8.Storage.OneDriveSync.Instance == null )
                        {
                            global::wenku8.Storage.OneDriveSync.Instance = new global::wenku8.Storage.OneDriveSync();
                        }
                        await global::wenku8.Storage.OneDriveSync.Instance.Authenticate();
                    }
                    OneDriveButton.Desc = sts.Text( "Enabled" );
                }
                else
                {
                    Properties.ENABLE_ONEDRIVE = false;
                    await global::wenku8.Storage.OneDriveSync.Instance.UnAuthenticate();
                    OneDriveButton.Desc = sts.Text( "Disabled" );
                }

                return;
            }

            PopupFrame.Navigate( ( Type ) P );
            PopupPage.State = ControlState.Reovia;
        }

        private class SettingsSection
        {
            public string Title { get; set; }
            public string Footnote { get; set; }
            public IEnumerable<ActiveItem> Data { get; set; }
            public bool IsEnabled { get; set; }
            public Action<object> ItemAction { get; set; }
        }

        private void Button_Tapped( object sender, TappedRoutedEventArgs e )
        {
            Windows.ApplicationModel.Core.CoreApplication.Exit();
        }

        private void Exit( object sender, XBackRequestedEventArgs e )
        {
            Windows.ApplicationModel.Core.CoreApplication.Exit();
        }
    }

}
