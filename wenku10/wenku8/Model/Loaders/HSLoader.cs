using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

using Net.Astropenguin.Logging;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Linq;

namespace wenku8.Model.Comments
{
	using AdvDM;
	using Resources;
	using REST;

	sealed class HSLoader<T> : ILoader<T>
	{
		public static readonly string ID = typeof( HSLoader<T> ).Name;

		public Action<IList<T>> Connector { get; set; }
		public Func<IList<T>, T[]> ConvertResult = ( x ) => { return x.ToArray(); };

		public int CurrentPage { get; set; }

		public bool PageEnded { get; private set; }
		public Func<SharersRequest.SHTarget, int, uint, string[], PostData> PostArgs;

		private string Id;
		private SharersRequest.SHTarget Target;

		private RuntimeCache RCache;
		private Type TType = typeof( T );

		public HSLoader( string Id, SharersRequest.SHTarget Target, Func<SharersRequest.SHTarget, int, uint, string[], PostData> PostArgs )
		{
			RCache = new RuntimeCache();
			this.Id = Id;
			this.Target = Target;
			this.PostArgs = PostArgs;
		}

		public async Task<IList<T>> NextPage( uint ExpectedCount = 30 )
		{
			TaskCompletionSource<T[]> Ts = new TaskCompletionSource<T[]>();

			RCache.POST(
				Shared.ShRequest.Server
				, PostArgs( Target, CurrentPage, ExpectedCount, new string[] { Id } )
				, ( e, Id ) =>
				{
					try
					{
						JsonObject JObj = JsonStatus.Parse( e.ResponseString );
						JsonArray JData = JObj.GetNamedArray( "data" );

						int LoadedCount = JData.Count();

						List<T> HSI = new List<T>( LoadedCount );
						foreach( JsonValue ItemDef in JData )
						{
							HSI.Add( ( T ) Activator.CreateInstance( TType, ItemDef.GetObject() ) );
						}

						PageEnded = LoadedCount < ExpectedCount;
						CurrentPage += LoadedCount;

						Ts.SetResult( ConvertResult( HSI ) );
					}
					catch ( Exception ex )
					{
						Logger.Log( ID, ex.Message, LogType.WARNING );
						PageEnded = true;
						Ts.TrySetResult( new T[ 0 ] );
					}
				}
				, ( cacheName, Id, ex ) =>
				{
					Logger.Log( ID, ex.Message, LogType.WARNING );
					PageEnded = true;
					Ts.TrySetResult( new T[ 0 ] );
				}
				, false
			);

			T[] Cs = await Ts.Task;
			return Cs;
		}
	}
}