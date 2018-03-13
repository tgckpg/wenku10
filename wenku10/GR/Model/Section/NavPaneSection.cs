using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;

using wenku10.Pages;

namespace GR.Model.Section
{
	using Config;
	using ListItem;
	using Net.Astropenguin.Messaging;

	class NavPaneSection : ActiveData
	{
		public object Context { get; private set; }

		private Brush bbrush = new SolidColorBrush( GRConfig.ContentReader.BgColorNav );
		private ContentReaderBase Reader;

		public IList<PaneNavButton> Nav { get; private set; }

		public Brush BackgroundBrush
		{
			get { return bbrush; }
			set
			{
				bbrush = value;
				NotifyChanged( "BackgroundBrush" );
			}
		}

		public NavPaneSection( ContentReaderBase ReaderPage, IList<PaneNavButton> NavButtons )
		{
			Reader = ReaderPage;
			Nav = NavButtons;
			SelectSection( NavButtons[ 0 ] );
			GRConfig.ConfigChanged.AddHandler( this, CRConfigChanged );
		}

		private void CRConfigChanged( Message Mesg )
		{
			if ( Mesg.TargetType == typeof( Config.Scopes.ContentReader ) && Mesg.Content == "BgColorNav" )
			{
				BackgroundBrush = new SolidColorBrush( ( Color ) Mesg.Payload );
			}
		}

		public void SelectSection( PaneNavButton P )
		{
			if ( P.Action != null )
			{
				P.Action();
				return;
			}

			Context = Activator.CreateInstance( P.Page, Reader );
			NotifyChanged( "Context" );
		}

	}
}