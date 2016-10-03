using System.Threading.Tasks;
using System.Reflection;

using Net.Astropenguin.Logging;
using Net.Astropenguin.Logging.Handler;

namespace wenku8.System
{
    using AdvDM;
    using Config;
    using Ext;
    using Storage;

    using ResTaotu = libtaotu.Resources.Shared;

    internal sealed class Bootstrap
	{
        public static readonly string ID = typeof( Bootstrap ).Name;

        public static FileSystemLog LogInstance;

        public static string Version = AppSettings.SimpVersion
#if DEBUG
            + "d";
#elif TESTING 
            + "t";
#elif BETA
            + "b";
#else
            + "+";
#endif

        public Bootstrap() { }

		public async void Start()
		{
#if ARM && DEBUG || TESTING || BETA
            Resources.Shared.ShRequest.Server = new global::System.Uri( "https://w10srv.astropenguin.net/" );
#endif
			// Must follow Order!
			//// Fixed Orders
			// 1. Setting is the first to initialize
			AppSettingsInit();
            Logger.Log( ID, "Application Settings Initilizated", LogType.INFO );
            // 2. Migrate
            Logger.Log( ID, "Migration", LogType.INFO );
            await new Migration().Migrate();

            // Storage might already be initialized on Prelaunch
            if ( Resources.Shared.Storage == null )
            {
                // 2. following the appstorage, prepare directories.
                Resources.Shared.Storage = new GeneralStorage();
                Net.Astropenguin.IO.XRegistry.AStorage = Resources.Shared.Storage;
                Logger.Log( ID, "Shared.Storage Initilizated", LogType.INFO );
            }

            // SHRequest AppVersion param
            Resources.Shared.ShRequest.Ver = Version;
            // Set Version Compatibles
            Resources.Shared.ShRequest.Compat = new string[] { "1.8.6t", "1.3.4b", "1.0.0p" };

            // Connection Mode
			WHttpRequest.UA = string.Format( "Grimoire Reaper/{0} ( wenku8 engine; taotu engine; UAP ) wenku10 by Astropenguin", Version );

			WCacheMode.Initialize();
            Logger.Log( ID, "WCacheMode Initilizated", LogType.INFO );
            //// End fixed orders

            // Shared Resources
            ResTaotu.SourceView = typeof( global::wenku10.Pages.DirectTextViewer );
            ResTaotu.RenameDialog = typeof( global::wenku10.Pages.Dialogs.Rename );
            ResTaotu.SetExtractor( typeof( Taotu.WenkuExtractor ) );
            ResTaotu.SetMarker( typeof( Taotu.WenkuMarker ) );
            ResTaotu.SetListLoader( typeof( Taotu.WenkuListLoader ) );
            ResTaotu.CreateRequest = x => new SHttpRequest( x );

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