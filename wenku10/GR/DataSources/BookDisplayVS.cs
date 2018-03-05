using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using Model.Interfaces;
	using PageExtensions;

	sealed class BookDisplayVS : GRViewSource, IExtViewSource, IGSWidget
	{
		private BookDisplayPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new BookDisplayPageExt() );

		public override Action<IGRRow> ItemAction => ( ( BookDisplayPageExt ) Extension ).OpenItem;

		public BookDisplayVS( string Name, Type DataType )
			: base( Name )
		{
			DataSourceType = DataType;
		}

		public WidgetConfig DefaultWidgetConfig()
		{
			if ( DataSourceType == typeof( HistoryData ) )
			{
				return new WidgetConfig()
				{
					Name = ItemTitle,
					TargetType = DataSource.ConfigId,
					Enable = true,
					Template = "Banner"
				};
			}

			return null;
		}

	}
}