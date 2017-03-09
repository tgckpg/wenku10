using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace wenku8.Model.Loaders
{
	using Comments;
	using Ext;

	sealed class CommentLoader : ILoader<Comment>
	{
		public delegate Comment[] CommentXMLParser( string s, out int p );

		public Action<IList<Comment>> Connector { get; set; }

		public int CurrentPage { get; set; }

		public bool PageEnded
		{
			get
			{
				return PageCount < CurrentPage;
			}
		}

		private string Id;
		private int PageCount = 1;
		private XKey[] RequestKey;
		private CommentXMLParser XParser;

		private IRuntimeCache wCache;

		public CommentLoader( string Id, XKey[] RequestKey, CommentXMLParser Parser )
		{
			wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
			this.Id = Id;
			this.RequestKey = RequestKey;
			XParser = Parser;
		}

		public async Task<IList<Comment>> NextPage( uint count = 0 )
		{
			TaskCompletionSource<Comment[]> Comments = new TaskCompletionSource<Comment[]>();

			if ( CurrentPage++ < PageCount )
			{
				RequestKey[ 3 ].KeyValue = CurrentPage.ToString();

				wCache.InitDownload(
					Id, RequestKey
					, ( DRequestCompletedEventArgs e, string id ) =>
					{
						Comment[] Result = XParser( e.ResponseString, out PageCount );
						Comments.SetResult( Result );
					}
					, ( string id, string uri, Exception ex ) =>
					{
						Comments.SetResult( new Comment[ 0 ] );
					}
					, true
				);
			}
			else
			{
				Comments.SetResult( new Comment[ 0 ] );
			}

			Comment[] Cs = await Comments.Task;
			return Cs;
		}
	}
}