using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

using Net.Astropenguin.Loaders;
using Windows.UI;

namespace wenku8.Converters
{
    public class HubStatusConverter : IValueConverter
    {
        public static readonly string ID = typeof( BoolDataConverter ).Name;

        private StringResources stx = new StringResources( "Error" );

        public object Convert( object value, Type targetType, object parameter, string language )
        {
            string ReturnType = ( string ) parameter;

            switch ( ReturnType )
            {
                case "String":
                    switch ( ( int ) value )
                    {
                        case -1: return stx.Str( "InvalidScript" );
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