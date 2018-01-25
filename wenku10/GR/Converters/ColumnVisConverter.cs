using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

using Net.Astropenguin.UI.Converters;

namespace GR.Converters
{
	sealed class ColumnVisConverter : DataBoolConverter 
	{
		public override object Convert( object value, Type targetType, object parameter, string language )
		{
			return DataBool( ( ( GridLength ) value ).Value, parameter != null ) ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}