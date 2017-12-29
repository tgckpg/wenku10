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
				Existings = Data.Remap( x => ( ( SpiderBook ) x ).aid );
			}

			Action<string, SpiderBook> ProcessSpider = ( Id, LB ) =>
			{
				if ( Existings.Contains( Id ) ) return;

				Loading = LoadText + ": " + Id;
				if ( LB.aid != Id )
				{
					try
					{
						Logger.Log( ID, "Fixing misplaced spider book" );
						Shared.Storage.MoveDir( FileLinks.ROOT_SPIDER_VOL + Id, LB.MetaLocation );
					}
					catch ( Exception ex )
					{
						Logger.Log( ID
							, string.Format(
								"Unable to move script: {0} => {1}, {2} "
								, Id, LB.aid, ex.Message )
								, LogType.WARNING );
					}
				}

				if ( LB.ProcessSuccess || LB.CanProcess )
				{
					Items.Add( LB );
					LB.IsFav = favs.Contains( Id );
				}
				else
				{
					try
					{
						Logger.Log( ID, "Removing invalid script: " + Id, LogType.INFO );
						Shared.Storage.RemoveDir( LB.MetaRoot );
					}
					catch ( Exception ex )
					{
						Logger.Log( ID, "Cannot remove invalid script: " + ex.Message, LogType.WARNING );
					}
				}
			};

			BookIds = Shared.Storage.ListDirs( FileLinks.ROOT_SPIDER_VOL );
			foreach ( string Id in BookIds )
			{
				if ( Id[ 0 ] == AppKeys.SP_ZONE_PFX )
				{
					IEnumerable<string> ZoneItems = Shared.Storage.ListDirs( FileLinks.ROOT_SPIDER_VOL + Id + "/" );
					foreach ( string SId in ZoneItems )
					{
						/**
						 * This code is a mess. I'll explain a bit more in here
						 *   First, the location of the Book.MetaLocation for ZoneItems
						 *   can only be retrived from BookInstruction
						 *   However ZoneId and Id are assinged by Spider on the fly,
						 *   restoring this information is a bit tricky
						 */

						// Create BookIntstruction just to retrieve the correct id pattern
						BookInstruction BInst = new BookInstruction( Id, SId );

						/**
						 * After 2 hours of investigations...
						 * Welp, just outsmarted by myself, The CreateAsyncSpide works because:
						 *   Inside the TestProcessed method, the BookInstruction are created
						 *   using BoockInstruction( Id, Setings ) overload
						 *   the provided id is "this.aid" here BUT the full id is restored again
						 *   in InitProcMan() method
						 *   Fortunately, ssid will be set correctly inside the ReadInfo method
						 */
						ProcessSpider( BInst.ZItemId, await SpiderBook.CreateAsyncSpider( BInst.ZItemId ) );
					}
				}
				else
				{
					ProcessSpider( Id, await SpiderBook.CreateAsyncSpider( Id ) );
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
				if ( NData.Any( x => x.aid == Book.aid ) )
				{
					Logger.Log( ID, "Already in collection, updating the data", LogType.DEBUG );
					NData.Remove( NData.First( x => x.aid == Book.aid ) );
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