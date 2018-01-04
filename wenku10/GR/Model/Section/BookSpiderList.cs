using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace GR.Model.Section
{
	using Book.Spider;
	using ListItem;
	using Net.Astropenguin.Linq;
	using Resources;
	using Settings;
	using Storage;

	sealed partial class BookSpiderList : LocalListBase
	{
		public static readonly string ID = typeof( BookSpiderList ).Name;

		public BookSpiderList()
		{
			ProcessVols();
		}

		public void Reload() { ProcessVols(); }

		public void Add( params LocalBook[] Book )
		{
			List<LocalBook> NData = new List<LocalBook>();

			if ( Data != null )
				NData.AddRange( Data.Cast<LocalBook>() );

			NData.AddRange( Book );
			Data = NData;

			NotifyChanged( "SearchSet" );
		}

		private async void ProcessVols()
		{
			StringResources stx = new StringResources( "LoadingMessage" );
			string LoadText = stx.Str( "ProgressIndicator_Message" );

			IEnumerable<string> BookIds = Shared.Storage.ListDirs( FileLinks.ROOT_LOCAL_VOL );
			string[] favs = new BookStorage().GetIdList();

			List<LocalBook> Items = new List<LocalBook>();

			IEnumerable<string> Existings = new string[ 0 ];
			if ( Data != null )
			{
				Items.AddRange( Data.Cast<LocalBook>() );
				Existings = Data.Remap( x => ( ( SpiderBook ) x ).ZItemId );
			}

			foreach ( Database.Models.Book Bk in Shared.QueryBooks( Database.Models.BookType.S )  )
			{
				if ( Existings.Contains( Bk.ZItemId ) )
					continue;

				Loading = LoadText + ": " + Bk.ZItemId;
				SpiderBook LB = await SpiderBook.CreateSAsync( Bk.ZItemId );

				if ( LB.ProcessSuccess || LB.CanProcess )
				{
					Items.Add( LB );
					LB.IsFav = favs.Contains( Bk.ZItemId );
				}
				else
				{
					try
					{
						Logger.Log( ID, "Removing invalid script: " + Bk.ZItemId, LogType.INFO );
						Shared.Storage.RemoveDir( LB.MetaRoot );
					}
					catch ( Exception ex )
					{
						Logger.Log( ID, "Cannot remove invalid script: " + ex.Message, LogType.WARNING );
					}
				}
			}

			if ( 0 < Items.Count ) SearchSet = Items;
			Loading = null;
		}

		public async Task<bool> OpenSpider( IStorageFile ISF )
		{
			try
			{
				SpiderBook SBook = await SpiderBook.ImportFile( await ISF.ReadString(), true );
				_ImportItem( SBook );

				return SBook.CanProcess || SBook.ProcessSuccess;
			}
			catch( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.ERROR );
			}

			return false;
		}

		public void ImportItem( SpiderBook Book )
		{
			try
			{
				_ImportItem( Book );
			}
			catch( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.ERROR );
			}
		}

		private void _ImportItem( SpiderBook Book )
		{
			List<LocalBook> NData;
			if ( Data != null )
			{
				NData = new List<LocalBook>( Data.Cast<LocalBook>() );
				if ( NData.Any( x => x.ZItemId == Book.ZItemId ) )
				{
					Logger.Log( ID, "Already in collection, updating the data", LogType.DEBUG );
					NData.Remove( NData.First( x => x.ZItemId == Book.ZItemId ) );
				}
			}
			else
			{
				NData = new List<LocalBook>();
			}

			NData.Add( Book );
			Data = NData;
			NotifyChanged( "SearchSet" );
		}

		public async void OpenSpider()
		{
			IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
			if ( ISF == null ) return;

			var j = OpenSpider( ISF );
		}
	}
}