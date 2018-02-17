using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Linq;

namespace GR.GStrings
{
	using Database.Models;
	using Database.Schema;
	using GSystem;
	using Model.ListItem.Sharers;
	using Model.Loaders;
	using Resources;

	class ZoneNameResolver
	{
		private string CacheId = "ZoneNames";
		private DbDictionary ZoneMap;

		private static ZoneNameResolver _Instance;
		public static ZoneNameResolver Instance => _Instance ?? ( _Instance = new ZoneNameResolver() );

		private ConcurrentDictionary<string, ConcurrentQueue<Action<string>>> ResolvQs = new ConcurrentDictionary<string, ConcurrentQueue<Action<string>>>();

		private void ReadCache()
		{
			ZCache Cache = Shared.ZCacheDb.GetCache( CacheId );
			if ( Cache != null )
			{
				ZoneMap = new DbDictionary();
				ZoneMap.Data = Cache.Data.StringValue;
			}
			else if ( ZoneMap == null )
			{
				ZoneMap = new DbDictionary();
			}
		}

		public async void Resolve( string uuid, Action<string> DisplayName )
		{
			if ( !Guid.TryParse( uuid, out Guid NOP_0 ) )
				return;

			ReadCache();

			// Find zone name from cache
			if ( ZoneMap.ContainsKey( uuid ) )
			{
				DisplayName( ZoneMap[ uuid ] );
				return;
			}

			if ( ResolvQs.TryGetValue( uuid, out ConcurrentQueue<Action<string>> RQ ) )
			{
				RQ.Enqueue( DisplayName );
				return;
			}
			else
			{
				ResolvQs.TryAdd( uuid, new ConcurrentQueue<Action<string>>() );
			}

			// Find zone name from online directory
			IEnumerable<string> AccessTokens = new TokenManager().AuthList.Remap( x => ( string ) x.Value );
			SHSearchLoader SHSL = new SHSearchLoader( "uuid: " + uuid, AccessTokens );

			IList<HubScriptItem> HSIs = await SHSL.NextPage();
			if ( HSIs.Any() )
			{
				string ZName = HSIs.First().Name;
				DisplayName( ZName );
				ZoneMap[ uuid ] = ZName;

				Shared.ZCacheDb.Write( CacheId, ZoneMap.Data );

				if ( ResolvQs.TryRemove( uuid, out ConcurrentQueue<Action<string>> ResolvQ ) )
				{
					while ( ResolvQ.TryDequeue( out Action<string> Resolved ) )
						Resolved( ZName );
				}
			}
			else
			{
				// Drop the entire waiting queue as we cannot resolve the name
				ResolvQs.TryRemove( uuid, out ConcurrentQueue<Action<string>> NOP_1 );
			}
		}

		public void Register( string uuid, string Name )
		{
			ReadCache();
			ZoneMap[ uuid ] = Name;
			Shared.ZCacheDb.Write( CacheId, ZoneMap.Data );
		}

	}
}