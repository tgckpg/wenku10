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
	using Model.ListItem;
	using Model.Pages;
	using Model.Section;

	sealed class ZSViewSource : GRViewSource
	{
		private ZoneSpider ZS;

		public override GRDataSource DataSource => _DataSource ?? ( _DataSource = ( GRDataSource ) Activator.CreateInstance( DataSourceType, ZS ) );

		public ZSViewSource( string Name, ZoneSpider ZS )
			: base( Name )
		{
			DataSourceType = typeof( ZSDisplayData );
			this.ZS = ZS;
		}

		public override Action<IGRRow> ItemAction
		{
			get
			{
				if ( base.ItemAction == null )
					return base.ItemAction;

				return ( x ) =>
				{
					RowAction( x );
					base.ItemAction( x );
				};
			}
			set => base.ItemAction = value;
		}

		private async void RowAction( IGRRow _Row )
		{
			GRRow<BookDisplay> Row = ( GRRow<BookDisplay> ) _Row;

			BookInstruction Payload = ( BookInstruction ) Row.Source.Payload;
			// Save the book here
			Payload.SaveInfo();

			// Reload the BookDisplay as Entry might changed from SaveInfo
			Row.Source = new BookDisplay( Payload.Entry );

			SpiderBook Item = await SpiderBook.CreateSAsync( Payload.ZoneId, Payload.ZItemId, Payload.BookSpiderDef );

			if ( !Item.ProcessSuccess && Item.CanProcess )
			{
				await ItemProcessor.ProcessLocal( Item );
			}
		}
	}
}