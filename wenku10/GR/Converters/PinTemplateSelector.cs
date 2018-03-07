using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GR.Converters
{
	using Model.ListItem;

	sealed class PinTemplateSelector : DataTemplateSelector
	{
		public DataTemplate DevRecord { get; set; }
		public DataTemplate PinRecord { get; set; }

		protected override DataTemplate SelectTemplateCore( object item, DependencyObject container )
		{
			FrameworkElement element = ( FrameworkElement ) container;
			PinRecord Para = ( PinRecord ) item;

			return Para.TreeLevel == 0 ? DevRecord : PinRecord;
		}

	}
}