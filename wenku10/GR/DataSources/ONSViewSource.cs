using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Loaders;

namespace GR.DataSources
{
	using Model.Interfaces;
	using PageExtensions;

	sealed class ONSViewSource : GRViewSource, IExtViewSource
	{
		private PageExtension _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new ONSPageExtension( this ) );

		public ONSViewSource()
			: base( "OnlineScriptDir" )
		{
			StringResources sta = new StringResources( "AppBar" );
			ItemTitle = sta.Text( ItemTitle );
			DataSourceType = typeof( ONSDisplayData );
		}
	}
}