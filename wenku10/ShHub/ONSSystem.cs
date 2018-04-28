using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GR.Config.Scopes;

namespace wenku10.SHHub
{
	class ONSSystem
	{
		public static ONSConfig Config => new ONSConfig();
	}

	class ONSConfig : Conf_System
	{
		protected override string ScopeId => "ONS";

		public string AuthToken
		{
			get => GetValue<string>( "AuthToken", null );
			set => SetValue( "AuthToken", value );
		}

		public string ServiceUri
		{
#if DEBUG && !ARM
			get => GetValue<string>( "AuthToken", "https://w10srv.botanical.astropenguin.net/" );
#else
			get => GetValue<string>( "AuthToken", "https://w10srv.astropenguin.net/" );
#endif
			set => SetValue( "AuthToken", value );
		}
	}
}