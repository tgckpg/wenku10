using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace wenku8.Converters
{
    using Model.ListItem;

    public class IsSpiderConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, string language )
        {
            return value is SpiderBook;
        }

        public object ConvertBack( object value, Type targetType, object parameter, string language )
        {
            return false;
        }
    }
}