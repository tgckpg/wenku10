using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
					case "2.0.10t":
					case "2.0.11t":
					case "2.0.12t":
					case "2.0.13t":
					case "2.0.14t":
						break;

					case "1.4.1b":
					case "1.4.2b":
					case "1.0.0p":
					case "1.0.1p":
					case "1.0.2p":
					case "1.0.3p":
						await Task.Delay( 1 );
						Migrate208t_104p();
						break;

					case "1.0.4p":
						break;
				}
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.ERROR );
			}

			Properties.VERSION = Bootstrap.Version;
		}

		private void Migrate208t_104p()
		{
			// Fix task settings
			new Tasks.BackgroundProcessor().UpdateTaskInterval( 420 );
		}

	}
}