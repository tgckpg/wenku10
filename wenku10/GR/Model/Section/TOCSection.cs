using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;

namespace GR.Model.Section
{
	using Config;
	using Database.Models;
	using Model.Book;
	using Resources;
	using Settings;
	using Storage;

	using BookItem = Book.BookItem;
	using VirtualVolume = Book.VirtualVolume;

	sealed class TOCSection : ActiveData
	{
		public bool AnchorAvailable
		{
			get { return AutoAnchor != null; }
		}

		public Chapter FirstChapter { get { return Volumes.FirstOrDefault()?.Chapters.FirstOrDefault(); } }

		public Volume[] Volumes { get; private set; }
		public ChapterVModel[] Chapters { get; private set; }
		public Chapter AutoAnchor { get; private set; }

		public IObservableVector<object> VolumeCollections { get; private set; }

		private BookItem CurrentBook;

		public TOCSection( BookItem b )
		{
			CurrentBook = b;
			Volumes = b.GetVolumes();

			if ( GRConfig.System.ChunkSingleVol )
				VirtualizeVolumes();

			SetAutoAnchor();
		}

		// This groups 30+ ChapterList to virtual volumes for easier navigation
		private void VirtualizeVolumes()
		{
			int l = Volumes.Count();
			if ( l == 0 || !( l == 1 && 30 < Volumes.First().Chapters.Count() ) ) return;
			Volumes = VirtualVolume.Create( Volumes.First() );
		}

		public void SelectVolume( Volume v )
		{
			Chapters = Shared.BooksDb.SafeRun( Db => v.Chapters.Select( x => new ChapterVModel( x ) ).ToArray() );
			NotifyChanged( "Chapters" );
		}

		public void SetViewSource( CollectionViewSource ViewSource )
		{
			ViewSource.Source = Volumes.Select( x => new ChapterGroup( x ) );

			VolumeCollections = ViewSource.View.CollectionGroups;
			NotifyChanged( "VolumeCollections" );
		}

		public void SetAutoAnchor()
		{
			// Set the autoanchor
			string AnchorId = new AutoAnchor( CurrentBook ).GetAutoVolAnc();

			foreach ( Volume V in Volumes )
			{
				foreach ( Chapter C in V.Chapters )
				{
					if ( C.Meta[ AppKeys.GLOBAL_CID ] == AnchorId )
					{
						AutoAnchor = C;
						goto EndLoop;
					}
				}
			}

			EndLoop:
			NotifyChanged( "AnchorAvailable" );
		}

		internal class ChapterGroup : List<ChapterVModel>
		{
			public Volume Vol { get; set; }

			public ChapterGroup( Volume V )
				: base( V.Chapters.Select( x => new ChapterVModel( x ) ) )
			{
				Vol = V;
			}
		}

	}
}