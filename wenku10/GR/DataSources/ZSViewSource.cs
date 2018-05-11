using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.IO;
using Net.Astropenguin.Messaging;

namespace GR.DataSources
{
	using Data;
	using GSystem;
	using Model.Book;
	using Model.Book.Spider;
	using Model.Interfaces;
	using Model.ListItem;
	using Model.Pages;
	using Model.Section;
	using PageExtensions;
	using Settings;

	sealed class ZSViewSource : GRViewSource, IExtViewSource
	{
		public ZoneSpider ZS { get; private set; }

		public override GRDataSource DataSource => _DataSource ?? ( _DataSource = ( GRDataSource ) Activator.CreateInstance( DataSourceType, ZS ) );
		public override Action<IGRRow> ItemAction => RowAction;

		private BookDisplayPageExt _Extension;
		public PageExtension Extension => _Extension ?? ( _Extension = new BookDisplayPageExt( DataSourceType.Name + "." + ZS.ZoneId ) );

		public ZSViewSource( string Name, ZoneSpider ZS )
			: base( Name )
		{
			DataSourceType = typeof( ZSDisplayData );
			this.ZS = ZS;
		}

		private void Item_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "Desc" )
			{
				MessageBus.Send( GetType(), ( ( SpiderBook ) sender ).Desc );
			}
		}

		private async void RowAction( IGRRow _Row )
		{
			await PageExtOperations.Run( _RowAction( _Row ) );
		}

		private async Task<bool> _RowAction( IGRRow _Row )
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
			Item.PropertyChanged += Item_PropertyChanged;

			XParameter Metadata = Item.PSettings.Parameter( "METADATA" ) ?? new XParameter( "METADATA" );
			Metadata.SetValue( new XKey( "payload", Row.Source.Entry.Meta[ AppKeys.GLOBAL_SSID ] ) );
			Item.PSettings.SetParameter( Metadata );

			if ( !Item.ProcessSuccess && Item.CanProcess )
			{
				await ItemProcessor.ProcessLocal( Item );
			}

			Item.PropertyChanged -= Item_PropertyChanged;

			( ( BookDisplayPageExt ) Extension ).OpenItem( _Row );

			return true;
		}

	}
}