using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace wenku8.Converters
{
	using Model.Loaders;
	using Model.Text;

	sealed public class ParaTemplateSelector : DataTemplateSelector
	{
		public bool IsHorizontal = true;

		protected override DataTemplate SelectTemplateCore( object item, DependencyObject container )
		{
			FrameworkElement element = ( FrameworkElement ) container;

			if ( item is IllusPara )
			{
				IllusPara Para = ( IllusPara ) item;

				if ( Para.EmbedIllus )
				{
					ContentIllusLoader.Instance.RegisterImage( Para );
					return ( DataTemplate ) element.FindName( "IllusEmbed" );
				}
				else
				{
					return ( DataTemplate ) element.FindName( "IllusIcon" + ( IsHorizontal ? "H" : "V" ) );
				}
			}

			return ( DataTemplate ) element.FindName( IsHorizontal ? "Horizontal" : "Vertical" );
		}

	}
}