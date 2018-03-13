using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;

namespace GR.Model.Pages.ContentReader
{
	using Config;
	using Net.Astropenguin.Messaging;

	class ESContext : ActiveData
	{
		public SolidColorBrush ARBrush { get; private set; }
		public SolidColorBrush HHBrush { get; private set; }
		public SolidColorBrush MHBrush { get; private set; }
		public SolidColorBrush SBrush { get; private set; }
		public SolidColorBrush ESSBrush { get; private set; }
		public SolidColorBrush ESDBrush { get; private set; }
		public SolidColorBrush ESBGBrush { get; private set; }

		public ScaleTransform RenderTransform { get; private set; }

		public ESContext()
		{
			GRConfig.ConfigChanged.AddHandler( this, GRConfigChanged );

			RenderTransform = new ScaleTransform();
			RenderTransform.ScaleX = RenderTransform.ScaleY = 1.5;

			UpdateClock( "ARColor", GRConfig.ContentReader.Clock.ARColor );
			UpdateClock( "HHColor", GRConfig.ContentReader.Clock.HHColor );
			UpdateClock( "MHColor", GRConfig.ContentReader.Clock.MHColor );
			UpdateClock( "SColor", GRConfig.ContentReader.Clock.SColor );

			UpdateEpStepper( "SColor", GRConfig.ContentReader.EpStepper.SColor );
			UpdateEpStepper( "DColor", GRConfig.ContentReader.EpStepper.DColor );
			UpdateEpStepper( "BackgroundColor", GRConfig.ContentReader.EpStepper.BackgroundColor );
		}

		private void GRConfigChanged( Message Mesg )
		{
			if ( Mesg.TargetType == typeof( Config.Scopes.ContentReader.Conf_Clock ) )
			{
				UpdateClock( Mesg.Content, Mesg.Payload );
			}
			else if ( Mesg.TargetType == typeof( Config.Scopes.ContentReader.Conf_EpStepper ) )
			{
				UpdateEpStepper( Mesg.Content, Mesg.Payload );
			}
		}

		private void UpdateClock( string Choice, object Value )
		{
			switch ( Choice )
			{
				case "ARColor":
					ARBrush = new SolidColorBrush( ( Color ) Value );
					NotifyChanged( "ARBrush" );
					break;
				case "HHColor":
					HHBrush = new SolidColorBrush( ( Color ) Value );
					NotifyChanged( "HHBrush" );
					break;
				case "MHColor":
					MHBrush = new SolidColorBrush( ( Color ) Value );
					NotifyChanged( "MHBrush" );
					break;
				case "SColor":
					SBrush = new SolidColorBrush( ( Color ) Value );
					NotifyChanged( "SBrush" );
					break;
			}
		}

		private void UpdateEpStepper( string Choice, object Value )
		{
			switch ( Choice )
			{
				case "SColor":
					ESSBrush = new SolidColorBrush( ( Color ) Value );
					NotifyChanged( "ESSBrush" );
					break;
				case "DColor":
					ESDBrush = new SolidColorBrush( ( Color ) Value );
					NotifyChanged( "ESDBrush" );
					break;
				case "BackgroundColor":
					ESBGBrush = new SolidColorBrush( ( Color ) Value );
					NotifyChanged( "ESBGBrush" );
					break;
			}
		}

	}
}