using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

using Net.Astropenguin.Loaders;

using wenku10.SHHub;

namespace GR.Model.Section.SharersHub
{
	using AdvDM;
	using Ext;
	using Interfaces;
	using ListItem.Sharers;
	using REST;
	using Resources;

	sealed class MyInbox : ISHActivity
	{
		SHMember Member;
		RuntimeCache RCache;

		public MyInbox()
		{
			Member = X.Singleton<SHMember>( XProto.SHMember );
			RCache = new RuntimeCache();
		}

		public Task<bool> Get()
		{
			TaskCompletionSource<bool> TCS = new TaskCompletionSource<bool>();

			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.MyInbox()
				, ( a, b ) =>
				{
					ProcessInbox( a, b );
					TCS.TrySetResult( true );
				}
				, ( a, b, c ) => TCS.TrySetResult( false )
				, false
			);

			return TCS.Task;
		}

		private void ProcessInbox( DRequestCompletedEventArgs e, string QId )
		{
			try
			{
				JsonObject JDef = JsonStatus.Parse( e.ResponseString );
				JsonArray JData = JDef.GetNamedArray( "data" );
				foreach( JsonValue JItem in JData )
				{
					InboxMessage BoxMessage = new InboxMessage( JItem.GetObject() );
					Member.Activities.AddUI( new Activity( BoxMessage.Name, BoxMessage.OpenComment )
					{
						TimeStamp = BoxMessage.TimeStamp
					} );
				}
			}
			catch( Exception )
			{
			}
		}

	}
}