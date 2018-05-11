using Microsoft.Services.Store.Engagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.GSystem
{
	static class ActionEvent
	{
#if !DEBUG
		public const string SECRET_MODE = "SecretMode";
		public const string NORMAL_MODE = "NormalMode";

		private static bool Normaled = false;
		private static bool Secreted = false;

		public static void Secret()
		{
			if ( Secreted ) return;
			Secreted = true;
			try { StoreServicesCustomEventLogger.GetDefault().Log( SECRET_MODE ); }
			catch ( Exception ) { }
		}

		public static void Normal()
		{
			if ( Normaled ) return;
			Normaled = true;
			try { StoreServicesCustomEventLogger.GetDefault().Log( NORMAL_MODE ); }
			catch ( Exception ) { }
		}
#else
		public static void Secret() { } 
		public static void Normal() { } 
#endif

	}
}