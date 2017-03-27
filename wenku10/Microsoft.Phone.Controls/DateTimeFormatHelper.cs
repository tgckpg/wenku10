// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Phone.Controls
{
	internal static class DateTimeFormatHelper
	{
		private const double Hour = 60.0;
		private const double Day = 24 * Hour;

		private const string SingleMeridiemDesignator = "t";
		private const string DoubleMeridiemDesignator = "tt";

		private static DateTimeFormatInfo formatInfo_GetSuperShortTime = null;
		private static DateTimeFormatInfo formatInfo_GetMonthAndDay = null;
		private static DateTimeFormatInfo formatInfo_GetShortTime = null;

		private static object lock_GetSuperShortTime = new object();
		private static object lock_GetMonthAndDay = new object();
		private static object lock_GetShortTime = new object();

		private static readonly Regex rxMonthAndDay = new Regex( "(d{1,2}[^A-Za-z]M{1,3})|(M{1,3}[^A-Za-z]d{1,2})" );
		private static readonly Regex rxSeconds = new Regex( "([^A-Za-z]s{1,2})" );

		public static int GetRelativeDayOfWeek( DateTime dt )
		{
			return ( ( int ) dt.DayOfWeek - ( int ) CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek + 7 ) % 7;
		}

		#region DateTime comparison methods

		public static bool IsFutureDateTime( DateTime relative, DateTime given )
		{
			return relative < given;
		}

		public static bool IsAnOlderYear( DateTime relative, DateTime given )
		{
			return relative.Year > given.Year;
		}

		public static bool IsAnOlderWeek( DateTime relative, DateTime given )
		{
			if ( IsAtLeastOneWeekOld( relative, given ) )
			{
				return true;
			}
			else
			{
				return GetRelativeDayOfWeek( given ) > GetRelativeDayOfWeek( relative );
			}
		}

		public static bool IsAtLeastOneWeekOld( DateTime relative, DateTime given )
		{
			return ( ( int ) ( relative - given ).TotalMinutes >= 7 * Day );
		}

		public static bool IsPastDayOfWeek( DateTime relative, DateTime given )
		{
			return GetRelativeDayOfWeek( relative ) > GetRelativeDayOfWeek( given );
		}

		public static bool IsPastDayOfWeekWithWindow( DateTime relative, DateTime given )
		{
			return IsPastDayOfWeek( relative, given ) && ( ( int ) ( relative - given ).TotalMinutes > 3 * Hour );
		}

		#endregion

		#region Culture awareness methods

		public static bool IsCurrentCultureJapanese()
		{
			return ( CultureInfo.CurrentCulture.Name.StartsWith( "ja", StringComparison.OrdinalIgnoreCase ) );
		}

		public static bool IsCurrentCultureKorean()
		{
			return ( CultureInfo.CurrentCulture.Name.StartsWith( "ko", StringComparison.OrdinalIgnoreCase ) );
		}

		public static bool IsCurrentCultureTurkish()
		{
			return ( CultureInfo.CurrentCulture.Name.StartsWith( "tr", StringComparison.OrdinalIgnoreCase ) );
		}

		public static bool IsCurrentCultureHungarian()
		{
			return ( CultureInfo.CurrentCulture.Name.StartsWith( "hu", StringComparison.OrdinalIgnoreCase ) );
		}

		public static bool IsCurrentUICultureFrench()
		{
			return ( CultureInfo.CurrentUICulture.Name.Equals( "fr-FR", StringComparison.Ordinal ) );
		}

		#endregion

		#region String generating methods

		public static string GetAbbreviatedDay( DateTime dt )
		{
			if ( DateTimeFormatHelper.IsCurrentCultureJapanese() || DateTimeFormatHelper.IsCurrentCultureKorean() )
			{
				return "(" + dt.ToString( "ddd", CultureInfo.CurrentCulture ) + ")";
			}
			else
			{
				return dt.ToString( "ddd", CultureInfo.CurrentCulture );
			}
		}

		[SuppressMessage( "Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Metro design guidelines normalize strings to lowercase." )]
		public static string GetSuperShortTime( DateTime dt )
		{
			if ( formatInfo_GetSuperShortTime == null )
			{
				lock ( lock_GetSuperShortTime )
				{
					StringBuilder result = new StringBuilder( string.Empty );
					string seconds;

					formatInfo_GetSuperShortTime = ( DateTimeFormatInfo ) CultureInfo.CurrentCulture.DateTimeFormat.Clone();

					result.Append( formatInfo_GetSuperShortTime.LongTimePattern );
					seconds = rxSeconds.Match( result.ToString() ).Value;
					result.Replace( " ", string.Empty );
					result.Replace( seconds, string.Empty );
					if ( !( DateTimeFormatHelper.IsCurrentCultureJapanese()
						|| DateTimeFormatHelper.IsCurrentCultureKorean()
						|| DateTimeFormatHelper.IsCurrentCultureHungarian() ) )
					{
						result.Replace( DoubleMeridiemDesignator, SingleMeridiemDesignator );
					}

					formatInfo_GetSuperShortTime.ShortTimePattern = result.ToString();
				}
			}

			return dt.ToString( "t", formatInfo_GetSuperShortTime ).ToLowerInvariant();
		}

		public static string GetMonthAndDay( DateTime dt )
		{
			if ( formatInfo_GetMonthAndDay == null )
			{
				lock ( lock_GetMonthAndDay )
				{
					StringBuilder result = new StringBuilder( string.Empty );

					formatInfo_GetMonthAndDay = ( DateTimeFormatInfo ) CultureInfo.CurrentCulture.DateTimeFormat.Clone();

					result.Append( rxMonthAndDay.Match( formatInfo_GetMonthAndDay.ShortDatePattern ).Value );
					if ( result.ToString().Contains( "." ) )
					{
						result.Append( "." );
					}

					formatInfo_GetMonthAndDay.ShortDatePattern = result.ToString();
				}
			}

			return dt.ToString( "d", formatInfo_GetMonthAndDay );
		}

		public static string GetShortDate( DateTime dt )
		{
			return dt.ToString( "d", CultureInfo.CurrentCulture );
		}

		public static string GetShortTime( DateTime dt )
		{
			if ( formatInfo_GetShortTime == null )
			{
				lock ( lock_GetShortTime )
				{
					StringBuilder result = new StringBuilder( string.Empty );
					string seconds;

					formatInfo_GetShortTime = ( DateTimeFormatInfo ) CultureInfo.CurrentCulture.DateTimeFormat.Clone();

					result.Append( formatInfo_GetShortTime.LongTimePattern );
					seconds = rxSeconds.Match( result.ToString() ).Value;
					result.Replace( seconds, string.Empty );

					formatInfo_GetShortTime.ShortTimePattern = result.ToString();
				}
			}

			return dt.ToString( "t", formatInfo_GetShortTime );
		}

		#endregion
	}
}