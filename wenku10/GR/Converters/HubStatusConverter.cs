using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

using Net.Astropenguin.Loaders;
using Windows.UI;

namespace GR.Converters
{
	public class HubStatusConverter : IValueConverter
	{
		private StringResources stx = StringResources.Load( "Error" );

		public object Convert( object value, Type targetType, object parameter, string language )
		{
			string ReturnType = ( string ) parameter;

			switch ( ReturnType )
			{
				case "String":
					switch ( ( int ) value )
					{
						case -1: return stx.Str( "HS_INVALID" );
						case -2: return stx.Str( "HS_NO_VOLDATA" );
						default: return value;
					}

				case "Color":
					switch ( ( int ) value )
					{
						case 0: return Colors.Green;
						default: return Colors.Red;
					}
			}

			throw new Exception( "Invalid Return Type" );
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language )
		{
			return false;
		}
	}
}