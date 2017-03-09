using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.UI.Converters;

namespace wenku8.Converters
{
	using Config;

	sealed class CurrDevConverter : DataBoolConverter 
	{
		public override object Convert( object value, Type targetType, object parameter, string language )
		{
			bool IsCurrDev = DataBool( ( string ) value == AppSettings.DeviceId, parameter != null );
			return IsCurrDev ? 1 : 0.6;
		}
	}
}