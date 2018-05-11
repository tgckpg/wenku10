using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Messaging;

namespace GR.Model.Pages.ContentReader
{
	using Config;

	sealed class AssistContext : ActiveData
	{
		public SolidColorBrush AssistBG;

		public double? H;
		public double? W;
		public HorizontalAlignment HALeft;
		public HorizontalAlignment HARight;
		public VerticalAlignment VATop;
		public VerticalAlignment VABottom;

		public AssistContext()
		{
			AssistBG = new SolidColorBrush( GRConfig.ContentReader.BgColorAssist );

			if ( GRConfig.ContentReader.IsHorizontal )
			{
				H = 10.0;
				W = null;
				HALeft = HorizontalAlignment.Stretch;
				HARight = HorizontalAlignment.Stretch;
				VATop = VerticalAlignment.Top;
				VABottom = VerticalAlignment.Bottom;
			}
			else
			{
				H = null;
				W = 10.0;
				HALeft = HorizontalAlignment.Left;
				HARight = HorizontalAlignment.Right;
				VATop = VerticalAlignment.Stretch;
				VABottom = VerticalAlignment.Stretch;
			}

			GRConfig.ConfigChanged.AddHandler( this, CRConfigChanged );
		}

		private void CRConfigChanged( Message Mesg )
		{
			if ( Mesg.TargetType == typeof( Config.Scopes.Conf_ContentReader ) )
			{
				if ( Mesg.Content == "BgColorAssist" )
				{
					AssistBG = new SolidColorBrush( ( Color ) Mesg.Payload );
					NotifyChanged( "AssistBG" );
				}
			}
		}

	}
}