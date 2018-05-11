using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;

using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace GR.Model.Loaders
{
	using AdvDM;
	using ListItem;
	using ListItem.Sharers;
	using Resources;
	using REST;

	class SHSearchLoader : ILoader<HubScriptItem>
	{
		public static readonly string ID = typeof( SHSearchLoader ).Name;

		public Action<IList<HubScriptItem>> Connector { get; set; }

		public bool PageEnded { get; private set; }
		public int CurrentPage { get; private set; }

		private string Query;
		private IEnumerable<string> AccessTokens;

		private RuntimeCache RCache = new RuntimeCache();

		public SHSearchLoader( string Query, IEnumerable<string> AccessTokens )
		{
			this.AccessTokens = AccessTokens;
			this.Query = Query;
		}

		public async Task<IList<HubScriptItem>> NextPage( uint ExpectedCount = 0 )
		{
			TaskCompletionSource<HubScriptItem[]> HSItems = new TaskCompletionSource<HubScriptItem[]>();

			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.Search( Query, CurrentPage, ExpectedCount, AccessTokens )
				, ( e, Id ) =>
				{
					try
					{
						JsonObject JResponse = JsonStatus.Parse( e.ResponseString );
						JsonArray JHS = JResponse.GetNamedArray( "data" );

						int LoadedCount = JHS.Count();

						PageEnded = LoadedCount < ExpectedCount;
						CurrentPage += LoadedCount;

						HSItems.SetResult( JHS.Remap( x => HubScriptItem.Create( x.GetObject() ) ).ToArray() );
					}
					catch ( Exception ex )
					{
						Logger.Log( ID, ex.Message, LogType.WARNING );
						PageEnded = true;
						HSItems.TrySetResult( new HubScriptItem[ 0 ] );
					}
				}
				, ( cacheName, Id, ex ) =>
				{
					Logger.Log( ID, ex.Message, LogType.WARNING );
					PageEnded = true;
					HSItems.TrySetResult( new HubScriptItem[ 0 ] );
				}
				, false
			);

			return await HSItems.Task;
		}
	}

	sealed class SHSLActiveItem : SHSearchLoader, ILoader<ActiveItem>
	{
		public SHSLActiveItem( string Query, IEnumerable<string> AccessTokens )
			: base( Query, AccessTokens ) { }

		Action<IList<ActiveItem>> ILoader<ActiveItem>.Connector
		{
			get
			{
				return ( Action<IList<ActiveItem>> ) Connector;
			}

			set
			{
				base.Connector = ( x ) => ( x ).Cast<ActiveItem>();
			}
		}

		async Task<IList<ActiveItem>> ILoader<ActiveItem>.NextPage( uint count )
		{
			return ( await base.NextPage( count ) ).Cast<ActiveItem>().ToList();
		}
	}

}