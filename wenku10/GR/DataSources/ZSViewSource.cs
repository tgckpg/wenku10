using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.DataSources
{
	using Data;
	using Model.Book;
	using Model.Book.Spider;
	using Model.Interfaces;
	using Model.ListItem;
	using Model.Pages;
	using Model.Section;
	using PageExtensions;

	sealed class ZSViewSource : GRViewSource, IExtViewSource
	{
		private ZoneSpider ZS;

		public override GRDataSource DataSource => _DataSource ?? ( _DataSource = ( GRDataSource ) Activator.CreateInstance( DataSourceType, ZS ) );
		public override Action<IGRRow> ItemAction => RowAction;

		private BookDisplayPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new BookDisplayPageExt() );

		public ZSViewSource( string Name, ZoneSpider ZS )
			: base( Name )
		{
			DataSourceType = typeof( ZSDisplayData );
			this.ZS = ZS;
		}

		private async void RowAction( IGRRow _Row )
		{
			GRRow<BookDisplay> Row = ( GRRow<BookDisplay> ) _Row;

			BookInstruction Payload = ( BookInstruction ) Row.Source.Payload;
			if ( Payload != null )
			{
				// Save the book here
				Payload.SaveInfo();

				// Reload the BookDisplay as Entry might changed from SaveInfo
				Row.Source = new BookDisplay( Payload.Entry );
			}

			SpiderBook Item = await SpiderBook.CreateSAsync( Row.Source.Entry.ZoneId, Row.Source.Entry.ZItemId, Payload?.BookSpiderDef );

			if ( !Item.ProcessSuccess && Item.CanProcess )
			{
				await ItemProcessor.ProcessLocal( Item );
			}

			( ( BookDisplayPageExt ) Extension ).OpenItem( _Row );
		}
	}
}