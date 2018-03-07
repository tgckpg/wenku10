using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace GR.Converters
{
	using Model.Book;
	using Model.Loaders;
	using Model.Pages;
	using Resources;
	using Settings;

	sealed public class WIBConverter : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, string language )
		{
			if ( value is BookDisplay BkDisplay )
			{
				BookItem BItem = ItemProcessor.GetBookItem( BkDisplay.Entry );

				if( !BItem.CoverExist )
				{
					BookLoader Loader = new BookLoader();
					Loader.LoadCover( BItem, true );
				}

				return BItem;
			}

			return null;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language ) => throw new NotSupportedException();
	}
}