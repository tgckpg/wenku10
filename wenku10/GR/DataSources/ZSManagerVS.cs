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

	sealed class ZSManagerVS : GRViewSource, IExtViewSource
	{
		private ZSMPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new ZSMPageExt( this ) );

		public ZSMDisplayData ZSMData => ( ZSMDisplayData ) DataSource;
		public override Action<IGRRow> ItemAction => ( ( ZSMPageExt ) Extension ).ProcessItem;

		public ZSManagerVS( string Name )
			: base( Name )
		{
			DataSourceType = typeof( ZSMDisplayData );
		}

	}
}