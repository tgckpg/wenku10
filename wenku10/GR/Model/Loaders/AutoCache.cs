using System;
using System.Collections;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Logging;

namespace GR.Model.Loaders
{
	using Book;
	using Database.Models;
	using Ext;

	sealed class AutoCache : ActiveData, IAutoCache
	{
		private const int AutoLimit = 2;
		private static int CurrentCount = 1;

		BookItem ThisBook;
		Action<BookItem> OnComplete;

		private string _StatusText;
		public string StatusText
		{
			get => _StatusText;
			private set
			{
				_StatusText = value;
				NotifyChanged( "StatusText" );
			}
		}

		public AutoCache( BookItem b, Action<BookItem> Handler )
		{
			ThisBook = b;
			OnComplete = Handler;

			StatusText = "Ready";
			if ( CurrentCount < AutoLimit )
			{
				new VolumeLoader( LoadAllVolumes ).Load( b );
			}
		}

		private async void LoadAllVolumes( BookItem b )
		{
			foreach( Volume Vol in b.GetVolumes() )
			{
				foreach ( Chapter Ch in Vol.Chapters )
				{
					StatusText = Vol.Title + "[" + Ch.Title + "]";
					TaskCompletionSource<bool> ChLoaded = new TaskCompletionSource<bool>();
					new ChapterLoader( b, x => { ChLoaded.SetResult( true ); } ).Load( Ch );
					await ChLoaded.Task;
				}
			}
			OnComplete( b );
		}

		internal static async void DownloadVolume( BookItem Book, Volume Vol )
		{
			foreach ( Chapter C in Vol.Chapters )
			{
				TaskCompletionSource<bool> ChLoaded = new TaskCompletionSource<bool>();
				new ChapterLoader( Book, x => { ChLoaded.SetResult( true ); } ).Load( C );
				await ChLoaded.Task;
			}
		}

	}
}