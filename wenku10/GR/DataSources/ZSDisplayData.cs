using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using static libtaotu.Pages.ProceduresPanel;

namespace GR.DataSources
{
	using GR.Data;
	using GR.Database.Models;
	using Model.Book;
	using Model.Section;

	/// <summary>
	/// Generic Loader DisplayData ( ILoader<BookItem> )
	/// </summary>
	sealed class GLDisplayData : BookDisplayData
	{
		protected override string ConfigId => "ZS-" + ZS.ZoneId;
		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Title", Width = 200 },
			new ColumnConfig() { Name = "Intro", Width = 265 },
			new ColumnConfig() { Name = "Author", Width = 100 },
			new ColumnConfig() { Name = "Status", Width = 100 },
			new ColumnConfig() { Name = "LastUpdateDate", Width = 160 },
		};

		private ZoneSpider ZS;

		public GLDisplayData( ZoneSpider ZS )
		{
			this.ZS = ZS;
		}

		private void MessageBus_OnDelivery( Message Mesg )
		{
			if ( Mesg.Payload is PanelLog PLog )
			{
				// Message = Mesg.Content;
				NotifyChanged( "Message" );
			}
		}

		public override async void Reload()
		{
			ILoader<BookItem> Loader = ZS.CreateLoader();

			IList<GRRow<BookDisplay>> FirstPage = ( await Loader.NextPage( 30 ) ).Remap( ToGRRow );
			Observables<BookItem, GRRow<BookDisplay>> ItemsObservable = new Observables<BookItem, GRRow<BookDisplay>>( FirstPage );
			// ItemsObservable.LoadEnd += ItemsObservable_LoadEnd;
			// ItemsObservable.LoadStart += ItemsObservable_LoadStart;

			ItemsObservable.ConnectLoader( Loader, b => b.Remap( ToGRRow ) );
			BkTable.Items = ItemsObservable;
		}

		private GRRow<BookDisplay> ToGRRow( BookItem B )
		{
			// Set the ZoneId to allow Entry save
			B.ZoneId = ZS.ZoneId;

			return new GRRow<BookDisplay>( BkTable )
			{
				Source = new BookDisplay( B.Entry ) { Payload = B },
				Cell = ( _i, _x ) => BkTable.CellProps[ _i ].Value( ( BookDisplay ) _x )
			};
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }
	}
}