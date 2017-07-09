using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;

using wenku10.Pages;

namespace wenku8.Model.Section
{
	using Config;
	using ListItem;

	class NavPaneSection : ActiveData
	{
		public object Context { get; private set; }

		private Brush bbrush = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_NAVBG );
		private ContentReader Reader;

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

		public NavPaneSection( ContentReader ReaderPage, IList<PaneNavButton> NavButtons )
		{
			Reader = ReaderPage;
			Nav = NavButtons;
			SelectSection( NavButtons[ 0 ] );
			AppSettings.PropertyChanged += AppSettings_PropertyChanged;
		}

		~NavPaneSection()
		{
			AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
		}

		private void AppSettings_PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
		{
			if( e.PropertyName == Parameters.APPEARANCE_CONTENTREADER_NAVBG )
			{
				BackgroundBrush = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_NAVBG );
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