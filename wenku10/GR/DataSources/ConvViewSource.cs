using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.DataSources
{
	using Data;
	using Model.Interfaces;
	using PageExtensions;

	sealed class ConvViewSource : GRViewSource, IExtViewSource
	{
		private ConvPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new ConvPageExt( this ) );
		public override GRDataSource DataSource => _DataSource ?? ( _DataSource = ( GRDataSource ) Activator.CreateInstance( DataSourceType, ItemTitle ) );

		public ConvViewSource( string Name )
			: base( Name )
		{
			DataSourceType = typeof( ConvDisplayData );
		}

		public void Delete( GRRow<IBookProcess> BkRow )
		{
		}

	}
}