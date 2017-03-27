// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Microsoft.Phone.Controls
{
	public class RelativeTimeConverter : IValueConverter
	{
		// Control Resources Hack
		private ControlResources ControlResources = new ControlResources();

		private const double Minute = 60.0;
		private const double Hour = 60.0 * Minute;
		private const double Day = 24 * Hour;
		private const double Week = 7 * Day;
		private const double Month = 30.5 * Day;
		private const double Year = 365 * Day;

		private const string DefaultCulture = "en-US";

		private string[] PluralHourStrings;
		private string[] PluralMinuteStrings;
		private string[] PluralSecondStrings;

		private void SetLocalizationCulture( string language )
		{
			ControlResources.Language = language;

			PluralHourStrings = new string[ 4 ] {
				  ControlResources.Str( "XHoursAgo_2To4" ),
				  ControlResources.Str( "XHoursAgo_EndsIn1Not11" ),
				  ControlResources.Str( "XHoursAgo_EndsIn2To4Not12To14" ),
				  ControlResources.Str( "XHoursAgo_Other" )
			  };

			PluralMinuteStrings = new string[ 4 ] {
				  ControlResources.Str( "XMinutesAgo_2To4" ),
				  ControlResources.Str( "XMinutesAgo_EndsIn1Not11" ),
				  ControlResources.Str( "XMinutesAgo_EndsIn2To4Not12To14" ),
				  ControlResources.Str( "XMinutesAgo_Other" )
			  };

			PluralSecondStrings = new string[ 4 ] {
				  ControlResources.Str( "XSecondsAgo_2To4" ),
				  ControlResources.Str( "XSecondsAgo_EndsIn1Not11" ),
				  ControlResources.Str( "XSecondsAgo_EndsIn2To4Not12To14" ),
				  ControlResources.Str( "XSecondsAgo_Other" )
			  };
		}

		private static string GetPluralMonth( int month )
		{
			ControlResources C = new ControlResources();
			IFormatProvider i = C.Culture.DateTimeFormat;

			if ( month >= 2 && month <= 4 )
			{
				return string.Format( i, C.Str( "XMonthsAgo_2To4" ), month.ToString( i ) );
			}
			else if ( month >= 5 && month <= 12 )
			{
				return string.Format( i, C.Str( "XMonthsAgo_5To12" ), month.ToString( i ) );
			}
			else
			{
				throw new ArgumentException( "Invalid number of Months" );
			}
		}

		private static string GetPluralTimeUnits( int units, string[] resources )
		{
			int modTen = units % 10;
			int modHundred = units % 100;

			ControlResources C = new ControlResources();
			IFormatProvider i = C.Culture.DateTimeFormat;
			if ( units <= 1 )
			{
				throw new ArgumentException( "Invalid number of Time units" );
			}
			else if ( units >= 2 && units <= 4 )
			{
				return string.Format( i, resources[ 0 ], units.ToString( i ) );
			}
			else if ( modTen == 1 && modHundred != 11 )
			{
				return string.Format( i, resources[ 1 ], units.ToString( i ) );
			}
			else if ( ( modTen >= 2 && modTen <= 4 ) && !( modHundred >= 12 && modHundred <= 14 ) )
			{
				return string.Format( i, resources[ 2 ], units.ToString( i ) );
			}
			else
			{
				return string.Format( i, resources[ 3 ], units.ToString( i ) );
			}
		}

		private static string GetLastDayOfWeek( DayOfWeek dow )
		{
			string result;
			ControlResources ControlResources = new ControlResources();
			switch ( dow )
			{
				case DayOfWeek.Monday:
					result = ControlResources.Str( "last Monday" );
					break;
				case DayOfWeek.Tuesday:
					result = ControlResources.Str( "last Tuesday" );
					break;
				case DayOfWeek.Wednesday:
					result = ControlResources.Str( "last Wednesday" );
					break;
				case DayOfWeek.Thursday:
					result = ControlResources.Str( "last Thursday" );
					break;
				case DayOfWeek.Friday:
					result = ControlResources.Str( "last Friday" );
					break;
				case DayOfWeek.Saturday:
					result = ControlResources.Str( "last Saturday" );
					break;
				case DayOfWeek.Sunday:
					result = ControlResources.Str( "last Sunday" );
					break;
				default:
					result = ControlResources.Str( "last Sunday" );
					break;
			}

			return result;
		}


		private static string GetOnDayOfWeek( DayOfWeek dow )
		{
			string result;

			ControlResources ControlResources = new ControlResources();
			switch ( dow )
			{
				case DayOfWeek.Monday:
					result = ControlResources.Str( "on Monday" );
					break;
				case DayOfWeek.Tuesday:
					result = ControlResources.Str( "on Tuesday" );
					break;
				case DayOfWeek.Wednesday:
					result = ControlResources.Str( "on Wednesday" );
					break;
				case DayOfWeek.Thursday:
					result = ControlResources.Str( "on Thursday" );
					break;
				case DayOfWeek.Friday:
					result = ControlResources.Str( "on Friday" );
					break;
				case DayOfWeek.Saturday:
					result = ControlResources.Str( "on Saturday" );
					break;
				case DayOfWeek.Sunday:
					result = ControlResources.Str( "on Sunday" );
					break;
				default:
					result = ControlResources.Str( "on Sunday" );
					break;
			}

			return result;
		}

		public object Convert( object value, Type targetType, object parameter, string language )
		{
			// Target value must be a System.DateTime object.
			if ( !( value is DateTime ) )
			{
				throw new ArgumentException( "Not a valid DateTime object" );
			}

			string result;

			DateTime given = ( ( DateTime ) value ).ToLocalTime();

			DateTime current = DateTime.Now;

			TimeSpan difference = current - given;

			SetLocalizationCulture( language );

			if ( DateTimeFormatHelper.IsFutureDateTime( current, given ) )
			{
				// Future dates and times are not supported, but to prevent crashing an app
				// if the time they receive from a server is slightly ahead of the phone's clock
				// we'll just default to the minimum, which is "2 seconds ago".
				result = GetPluralTimeUnits( 2, PluralSecondStrings );
			}

			if ( difference.TotalSeconds > Year )
			{
				// "over a year ago"
				result = ControlResources.Str( "OverAYearAgo" );
			}
			else if ( difference.TotalSeconds > ( 1.5 * Month ) )
			{
				// "x months ago"
				int nMonths = ( int ) ( ( difference.TotalSeconds + Month / 2 ) / Month );
				result = GetPluralMonth( nMonths );
			}
			else if ( difference.TotalSeconds >= ( 3.5 * Week ) )
			{
				// "about a month ago"
				result = ControlResources.Str( "AboutAMonthAgo" );
			}
			else if ( difference.TotalSeconds >= Week )
			{
				int nWeeks = ( int ) ( difference.TotalSeconds / Week );
				if ( nWeeks > 1 )
				{
					// "x weeks ago"
					result = string.Format( CultureInfo.CurrentUICulture, ControlResources.Str( "XWeeksAgo_2To4" ), nWeeks.ToString( ControlResources.Str( "Culture" ) ) );
				}
				else
				{
					// "about a week ago"
					result = ControlResources.Str( "AboutAWeekAgo" );
				}
			}
			else if ( difference.TotalSeconds >= ( 5 * Day ) )
			{
				// "last <dayofweek>"	
				result = GetLastDayOfWeek( given.DayOfWeek );
			}
			else if ( difference.TotalSeconds >= Day )
			{
				// "on <dayofweek>"
				result = GetOnDayOfWeek( given.DayOfWeek );
			}
			else if ( difference.TotalSeconds >= ( 2 * Hour ) )
			{
				// "x hours ago"
				int nHours = ( int ) ( difference.TotalSeconds / Hour );
				result = GetPluralTimeUnits( nHours, PluralHourStrings );
			}
			else if ( difference.TotalSeconds >= Hour )
			{
				// "about an hour ago"
				result = ControlResources.Str( "AboutAnHourAgo" );
			}
			else if ( difference.TotalSeconds >= ( 2 * Minute ) )
			{
				// "x minutes ago"
				int nMinutes = ( int ) ( difference.TotalSeconds / Minute );
				result = GetPluralTimeUnits( nMinutes, PluralMinuteStrings );
			}
			else if ( difference.TotalSeconds >= Minute )
			{
				// "about a minute ago"
				result = ControlResources.Str( "AboutAMinuteAgo" );
			}
			else
			{
				// "x seconds ago" or default to "2 seconds ago" if less than two seconds.
				int nSeconds = ( ( int ) difference.TotalSeconds > 1.0 ) ? ( int ) difference.TotalSeconds : 2;
				result = GetPluralTimeUnits( nSeconds, PluralSecondStrings );
			}

			return result;
		}

		public object ConvertBack( object value, Type targetType, object parameter, string language )
		{
			throw new NotImplementedException();
		}
	}
}