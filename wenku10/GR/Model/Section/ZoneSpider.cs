using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;

using GFlow.Controls;
using GFlow.Models.Procedure;

namespace GR.Model.Section
{
	using Book;
	using Interfaces;
	using Loaders;
	using GFlow;
	using Settings;

	sealed class ZoneSpider : ActiveData, IMetaSpider
	{
		public static readonly string ID = typeof( ZoneSpider ).Name;

		public string ZoneId { get { return PM.GUID; } }
		public string MetaLocation { get { return FileLinks.ROOT_ZSPIDER + ZoneId + ".xml"; } }

		public ObservableCollection<Procedure> ProcList { get { return PM?.ProcList; } }

		public string Name { get; set; }
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
			GrimoireListLoader PLL = ( GrimoireListLoader ) ProcList.FirstOrDefault( x => x is GrimoireListLoader );

			if ( PLL == null )
			{
				throw new InvalidFIleException();
			}

			Banner = PLL.BannerSrc;
			Name = PLL.ZoneName;

			if ( string.IsNullOrEmpty( Name ) )
			{
				Name = "[ Untitled ]";
				GStrings.ZoneNameResolver.Instance.Resolve( ZoneId, x =>
				{
					Name = x;
					NotifyChanged( "Name" );
				} );
			}

			NotifyChanged( "Name", "Banner" );
		}

		public void Reload()
		{
			try
			{
				Open( Resources.Shared.Storage.GetStream( MetaLocation ) );
			}
			catch ( Exception ) { }
		}

		public ZSFeedbackLoader<BookItem> CreateLoader()
		{
			return new ZSFeedbackLoader<BookItem>( PM.CreateSpider() );
		}

		public bool Open( Stream s )
		{
			IsLoading = true;

			bool LoadSuccess = false;
			using ( s )
			{
				PM = ProcManager.Load( s );
				if ( PM != null )
				{
					LoadSuccess = true;
					NotifyChanged( "ProcList" );
					SetBanner();
				}
			}

			IsLoading = false;
			return LoadSuccess;
		}

		private class InvalidFIleException : Exception { }

	}
}