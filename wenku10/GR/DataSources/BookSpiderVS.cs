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

	sealed class BookSpiderVS : GRViewSource, IExtViewSource
	{
		private BookSpiderPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new BookSpiderPageExt( this ) );

		private BookSpiderDisplayData BSData => ( BookSpiderDisplayData ) DataSource;

		public override Action<IGRRow> ItemAction => ( ( BookSpiderPageExt ) Extension ).ProcessItem;

		public BookSpiderVS( string Name )
			: base( Name )
		{
			DataSourceType = typeof( BookSpiderDisplayData );
		}

		public async Task<bool> OpenSpider( IStorageFile ISF )
		{
			try
			{
				SpiderBook SBook = await SpiderBook.ImportFile( await ISF.ReadString(), true );
				BSData.ImportItem( SBook );

				return SBook.CanProcess || SBook.ProcessSuccess;
			}
			catch ( Exception ex )
			{
				// Logger.Log( ID, ex.Message, LogType.ERROR );
			}

			return false;
		}

		public async void Copy( SpiderBook BkProc )
		{
			BSData.ImportItem( await BkProc.Clone() );
		}

		public void Delete( GRRow<IBookProcess> BkRow )
		{
			BSData.Delete( BkRow );
		}

	}
}