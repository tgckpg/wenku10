using System;
using System.Collections;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku10;

namespace GR.Model.Loaders
{
	using Book;
	using Ext;

	sealed class AutoCache : ActiveData, IAutoCache
	{
		public static readonly string ID = typeof( AutoCache ).Name;

		private const int AutoLimit = 2;
		private static int CurrentCount = 1;

		BookItem ThisBook;
		Action<BookItem> OnComplete;

		public string StatusText { get; private set; }

		public AutoCache( BookItem b, Action<BookItem> Handler )
		{
			ThisBook = b;
			OnComplete = Handler;

			StatusText = "Ready";
			if ( CurrentCount < AutoLimit )
			{
				// XXX
			}
		}

		private void DispLog( string p )
		{
			Logger.Log( ID, p, LogType.DEBUG );
			Worker.UIInvoke( () =>
			{
				StatusText = p;
				NotifyChanged( "StatusText" );
			} );
		}

		internal static async void DownloadVolume( BookItem Book, Database.Models.Volume Vol )
		{
			foreach ( Database.Models.Chapter C in Vol.Chapters )
			{
				TaskCompletionSource<bool> ChLoaded = new TaskCompletionSource<bool>();
				new ChapterLoader( x => { ChLoaded.SetResult( true ); } ).Load( C );
				await ChLoaded.Task;
			}
		}

	}
}