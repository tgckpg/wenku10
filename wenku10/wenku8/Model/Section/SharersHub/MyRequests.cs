using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using wenku10.SHHub;

namespace wenku8.Model.Section.SharersHub
{
	using AdvDM;
	using Ext;
	using Interfaces;
	using ListItem.Sharers;
	using REST;
	using Resources;
	using Settings;

	sealed class MyRequests : ISHActivity
	{
		public static readonly string ID = typeof( MyRequests ).Name;

		SHMember Member;
		RuntimeCache RCache;

		public IEnumerable<SHGrant> Grants { get; private set; }

		public MyRequests()
		{
			Grants = new SHGrant[ 0 ];
			Member = X.Singleton<SHMember>( XProto.SHMember );
			RCache = new RuntimeCache();
		}

		public Task<bool> Get()
		{
			TaskCompletionSource<bool> TCS = new TaskCompletionSource<bool>();

			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.MyRequests()
				, ( a, b ) =>
				{
					RequestsStatus( a, b );
					TCS.TrySetResult( true );
				}
				, ( a, b, c ) => TCS.TrySetResult( false )
				, false
			);

			return TCS.Task;
		}

		private void RequestsStatus( DRequestCompletedEventArgs e, string QId )
		{
			try
			{
				int NGrants = 0;
				int NScripts = 0;
				JsonObject JMesg = JsonStatus.Parse( e.ResponseString );
				JsonArray JData = JMesg.GetNamedArray( "data" );

				if ( 0 < Grants.Count() )
				{
					List<SHGrant> CurrGrants = new List<SHGrant>( Grants );
					foreach( JsonValue JValue in JData )
					{
						SHGrant G = new SHGrant( JValue.GetObject() );
						if ( Grants.Any( x => x.Id == G.Id ) ) continue;
						CurrGrants.Add( G );
					}
					Grants = CurrGrants.ToArray();
				}
				else
				{
					Grants = JData.Remap( x =>
					{
						SHGrant G = new SHGrant( x.GetObject() );

						int l = G.Grants.Length;
						if ( !G.SourceRemoved )
						{
							if ( 0 < l ) NScripts++;
							NGrants += l;
						}

						return G;
					} );
				}

				if ( 0 < NGrants )
				{
					Member.Activities.Add( () =>
					{
						StringResources stx = new StringResources();
						return string.Format( stx.Text( "GrantsReceived" ), NGrants, NScripts );
					}, () => MessageBus.SendUI( typeof( MyRequests ), AppKeys.SH_SHOW_GRANTS ) );
				}
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.WARNING );
			}
		}

	}
}