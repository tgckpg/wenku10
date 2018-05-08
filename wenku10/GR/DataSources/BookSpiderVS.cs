using System;
using System.Collections.Concurrent;
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
		public BookSpiderPageExt BSExt => ( BookSpiderPageExt ) Extension;

		public override Action<IGRRow> ItemAction => BSExt.ProcessOrOpenItem;

		private ConcurrentQueue<Tuple<string, string>> PQueue = new ConcurrentQueue<Tuple<string, string>>();

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

		public void Process( string ZoneId, string ZItemId )
		{
			PQueue.Enqueue( new Tuple<string, string>( ZoneId, ZItemId ) );

			BSData.PropertyChanged -= BSData_PropertyChanged;
			BSData.PropertyChanged += BSData_PropertyChanged;
		}

		private void BSData_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if( e.PropertyName == "IsLoading" && BSData.IsLoading == false )
			{
				while ( PQueue.TryDequeue( out Tuple<string, string> PQ ) )
				{
					IGRRow Row = BSData.FindRow( PQ.Item1, PQ.Item2 );
					if( Row != null )
					{
						BSExt.ProcessItem( Row );
					}
				}
			}
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