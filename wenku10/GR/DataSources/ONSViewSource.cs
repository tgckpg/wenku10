using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.DataSources
{
	using GR.Data;
	using Model.Interfaces;
	using PageExtensions;

	sealed class ONSViewSource : GRViewSource, IExtViewSource
	{
		private ONSPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new ONSPageExt( this ) );

		public override Action<IGRRow> ItemAction => ( ( ONSPageExt ) Extension ).OpenItem;

		public ONSViewSource( string Name, Type DataType )
			: base( Name )
		{
			DataSourceType = DataType;
		}
	}
}