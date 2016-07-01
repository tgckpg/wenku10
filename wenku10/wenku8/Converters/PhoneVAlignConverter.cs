using System;
using wenku10;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace wenku8.Converters
{
    public class PhoneVAlignConverter : IValueConverter
    {
        public static readonly string ID = typeof( BoolDataConverter ).Name;

        public object Convert( object value, Type targetType, object parameter, string language )
        {
            string[] TF = parameter.ToString().Split( '|' );
            string Align = MainStage.Instance.IsPhone ? TF[ 0 ] : TF[ 1 ];
            switch ( Align )
            {
                case "Stretch":
                    return VerticalAlignment.Stretch;
                case "Bottom":
                    return VerticalAlignment.Bottom;
                case "Center":
                    return VerticalAlignment.Center;
                case "Top":
                    return VerticalAlignment.Top;
            }

            throw new InvalidOperationException( "Not a valid verticale alignment: " + Align );
        }

        public object ConvertBack( object value, Type targetType, object parameter, string language )
        {
            return false;
        }
    }
}
