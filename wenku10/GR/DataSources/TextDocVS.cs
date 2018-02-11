using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.IO;

namespace GR.DataSources
{
	using Data;
	using Model.ListItem;
	using Model.Interfaces;
	using PageExtensions;

	sealed class TextDocVS : GRViewSource, IExtViewSource
	{
		private TextDocPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new TextDocPageExt( this ) );

		public TextDocDisplayData BSData => ( TextDocDisplayData ) DataSource;

		public override Action<IGRRow> ItemAction => ( ( TextDocPageExt ) Extension ).ProcessItem;

		public TextDocVS( string Name )
			: base( Name )
		{
			DataSourceType = typeof( TextDocDisplayData );
		}

		public void Delete( GRRow<IBookProcess> BkRow )
		{
			BSData.Delete( BkRow );
		}

	}
}