using System.Reflection;

using Net.Astropenguin.Logging;
using Net.Astropenguin.Logging.Handler;

namespace wenku8.System
{
    using AdvDM;
    using Config;
    using Ext;
    using Storage;

    internal sealed class Bootstrap
	{
        public static readonly string ID = typeof( Bootstrap ).Name;

        public static FileSystemLog LogInstance;

        public static string Version = AppSettings.SimpVersion
#if DEBUG || TESTING
            + "t";
#elif BETA
            + "b";
#else
            + "+";
#endif

        public Bootstrap() { }

		public async void Start()
		{
#if ARM && DEBUG
            Resources.Shared.ShRequest.Server = new global::System.Uri( "http://w10srv.astropenguin.net/" );
#endif
			// Must follow Order!
			//// Fixed Orders
			// 1. Setting is the first to initialize
			AppSettingsInit();
            Logger.Log( ID, "Application Settings Initilizated", LogType.INFO );
            // 2. Migrate
            Logger.Log( ID, "Migration", LogType.INFO );
            await new Migration().Migrate();
            // 2. following the appstorage, prepare directories.
            Resources.Shared.Storage = new GeneralStorage();
            Net.Astropenguin.IO.XRegistry.AStorage = Resources.Shared.Storage;
            Logger.Log( ID, "Shared.Storage Initilizated", LogType.INFO );
            Logger.Log( ID, "AppGate Initilizated", LogType.INFO );
            // Connection Mode
			WCacheMode.Initialize();
            Logger.Log( ID, "WCacheMode Initilizated", LogType.INFO );
            //// End fixed orders

            // Shared Resources
            libtaotu.Resources.Shared.SourceView = typeof( global::wenku10.Pages.DirectTextViewer );
            libtaotu.Resources.Shared.RenameDialog = typeof( global::wenku10.Pages.Dialogs.Rename );
            libtaotu.Resources.Shared.SetExtractor( typeof( Taotu.WenkuExtractor ) );
            libtaotu.Resources.Shared.SetMarker( typeof( Taotu.WenkuMarker ) );

            // Unlocking libraries
            Net.Astropenguin.UI.VerticalStack.LOCKED = false;

            // Set Logger for libeburc
            EBDictManager.SetLogger();
        }

        private static bool L2 = false;
        public void Level2()
        {
            if ( L2 ) return;
            L2 = true;
            X.Init();
            Logger.Log( ID, "Memeber Initilizated", LogType.INFO );
            // 1. Runtime Queue
            global::wenku10.App.RuntimeTransfer = new WRuntimeTransfer();
            Logger.Log( ID, "WRuntimeTransfer Initilizated", LogType.INFO );
        }

        private void AppSettingsInit()
		{
			AppSettings.Initialize();
			if( Properties.ENABLE_SYSTEM_LOG )
			{
				LogInstance = new FileSystemLog( "debug.log" );
			}

            // NetLog Re-initialize
            if ( Properties.ENABLE_RSYSTEM_LOG )
            {
                NetLog.Enabled = true;
                NetLog.RemoteIP = Properties.RSYSTEM_LOG_ADDRESS;
                NetLog.PostInit();
            }

            string Lang = Properties.LANGUAGE;
            // Language Override
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Lang;

#if DEBUG
            Logger.Log( ID, typeof( global::wenku10.App ).GetTypeInfo().Assembly.FullName, LogType.INFO );
#endif
            Logger.Log( ID, string.Format( "Language is {0}", Lang ), LogType.INFO );
		}
	}
}