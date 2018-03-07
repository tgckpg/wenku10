using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Net.Astropenguin.Messaging;

namespace GR.Model.Loaders
{
	using Book;
	using Database.Models;

	sealed class AutoCache
	{
		private static List<int> ActiveLoaders = new List<int>();

		internal static void DownloadVolume( BookItem Book, Volume Vol )
		{
			var j = DownloadVolumeAsync( Book, Vol );
		}

		internal static async Task DownloadVolumeAsync( BookItem Book, Volume Vol )
		{
			lock ( ActiveLoaders )
			{
				if ( ActiveLoaders.Contains( Vol.Id ) ) return;
				ActiveLoaders.Add( Vol.Id );
			}

			foreach ( Chapter C in Vol.Chapters )
			{
				TaskCompletionSource<bool> ChLoaded = new TaskCompletionSource<bool>();
				new ChapterLoader( Book, x => { ChLoaded.SetResult( true ); } ).Load( C );
				if ( await ChLoaded.Task )
				{
					ChapterVModel.ChapterLoaded.Deliver( new Message( typeof( AutoCache ), null, C ) );
				}
			}

			ActiveLoaders.Remove( Vol.Id );
		}

	}
}