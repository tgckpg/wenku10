using Net.Astropenguin.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tasks
{
	sealed class THttpRequest : HttpRequest
	{
		public static string UA { get; internal set; }

		public THttpRequest( Uri RequestUri )
			: base( RequestUri )
		{
			EN_UITHREAD = false;
		}

		override protected void CreateRequest()
		{
			base.CreateRequest();
			UserAgent = UA;
		}
	}
}