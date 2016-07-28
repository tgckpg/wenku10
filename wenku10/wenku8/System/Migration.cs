using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

namespace wenku8.System
{
    using Config;

    sealed class Migration
    {
        public static readonly string ID = typeof( Migration ).Name;

        public Migration() { }

        public async Task Migrate()
        {
            // Current Version
            if ( Properties.VERSION == Bootstrap.Version )
            {
                Logger.Log( ID, "Already the latest version", LogType.INFO );
                return;
            }

            try
            {
                switch ( Properties.VERSION )
                {
                    // Keep up to 5 migration versions
                    case "1.6.19t":
                    case "1.7.0t":
                    case "1.7.1t":
                    case "1.7.2t":
                    case "1.7.3t":

                    case "1.2.6b":
                    case "1.2.7b":
                    case "1.2.8b":
                    case "1.2.9b":
                    case "1.2.10b":
                        break;

                    default:
                        Logger.Log( ID, "Unknown Version: Will try to migrate", LogType.ERROR );
                        // Just to ensure the migration should be run asynchronously
                        await Task.Delay( 1 );
                        break;
                }
                Migrate_Latest();
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }

            Properties.VERSION = Bootstrap.Version;
        }

        private void Migrate_Latest()
        {
            // Setting -> Settings
            AppStorage Storage = new AppStorage();
            IsolatedStorageFile ISFS = Storage.GetISOStorage();
            if( ISFS.DirectoryExists( "Setting" ) )
            {
                ISFS.MoveDirectory( "Setting", "Settings" );
            }
        }
    }
}