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
			StringResources stx = StringResources.Load( "AppResources", "NavigationTitles" );
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

		public static string FTSColumns( string Name )
		{
			StringResources stx = StringResources.Load( "Book", "AppResources" );
			switch ( Name )
			{
				case "Title":
					return stx.Text( Name );
				case "VolTitle":
					return stx.Text( "Volume" );
				case "EpTitle":
					return stx.Text( "Chapter" );
				case "Result":
					return stx.Text( "Search_Result", "AppResources" );
			}

			return Name;
		}

		public static string TSTColumns( string Name )
		{
			StringResources stx = StringResources.Load();
			switch ( Name )
			{
				case "Name":
					return stx.Text( "KV_Key" );
				case "Value":
					return stx.Text( "KV_Value" );
			}

			return Name;
		}
	}
}