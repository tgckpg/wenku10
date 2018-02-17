using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Loaders;

namespace GR.GStrings
{
	static class ColumnNameResolver
	{
		public static string IBookProcess( string Name )
		{
			StringResources stx = new StringResBg( "AppResources", "NavigationTitles" );
			switch ( Name )
			{
				case "Name":
					return stx.Text( Name );
				case "Zone":
					return stx.Text( "Zones", "NavigationTitles" );
				case "Desc":
					return stx.Text( "Messages" );
				case "Desc2":
					return "Source";
			}

			return Name;
		}
	}
}