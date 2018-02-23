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

	sealed class FTSViewSource : GRViewSource, IExtViewSource
	{
		private FTSDataPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new FTSDataPageExt( this ) );

		public FTSDisplayData FTSData => ( FTSDisplayData ) DataSource;
		public override Action<IGRRow> ItemAction => ( ( FTSDataPageExt ) Extension ).OpenItem;

		public FTSViewSource( string Name, Type DataType )
			: base( Name )
		{
			DataSourceType = DataType;
		}
	}
}