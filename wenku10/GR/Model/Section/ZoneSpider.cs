using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using libtaotu.Controls;
using libtaotu.Models.Procedure;

namespace GR.Model.Section
{
	using Book;
	using Interfaces;
	using Loaders;
	using Taotu;
	using Settings;

	sealed class ZoneSpider : ActiveData, IMetaSpider
	{
		public static readonly string ID = typeof( ZoneSpider ).Name;

		public string ZoneId { get { return PM.GUID; } }
		public string MetaLocation { get { return FileLinks.ROOT_ZSPIDER + ZoneId + ".xml"; } }

		public ObservableCollection<Procedure> ProcList { get { return PM?.ProcList; } }
		public Uri Banner { get; private set; }

		private ProcManager PM;

		public string Message { get; private set; }

		private int loadLevel = 0;
		public bool IsLoading
		{
			get { return 0 < loadLevel; }
			private set
			{
				loadLevel += value ? 1 : -1;
				NotifyChanged( "IsLoading" );
			}
		}

		public ZoneSpider() { }

		private void SetBanner()
		{
			WenkuListLoader PLL = ( WenkuListLoader ) ProcList.FirstOrDefault( x => x is WenkuListLoader );

			if ( PLL == null )
			{
				throw new InvalidFIleException();
			}

			Banner = PLL.BannerSrc;
			NotifyChanged( "Banner" );
		}

		public void Reload()
		{
			try
			{
				Open( new XRegistry( "<zs />", MetaLocation ) );
			}
			catch ( Exception ) { }
		}

		public ZSFeedbackLoader<BookItem> CreateLoader()
		{
			return new ZSFeedbackLoader<BookItem>( PM.CreateSpider() );
		}

		public bool Open( XRegistry ZDef )
		{
			IsLoading = true;

			try
			{
				XParameter Param = ZDef.Parameter( "Procedures" );
				PM = new ProcManager( Param );
				NotifyChanged( "ProcList" );

				SetBanner();

				return true;
			}
			catch( InvalidFIleException )
			{
				ProcManager.PanelMessage( ID, Res.RSTR( "InvalidXML" ), LogType.ERROR );
			}
			catch( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.ERROR );
			}
			finally
			{
				IsLoading = false;
			}

			return false;
		}

		private class InvalidFIleException : Exception { }

	}
}