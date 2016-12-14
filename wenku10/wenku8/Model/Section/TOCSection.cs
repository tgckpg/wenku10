using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;

namespace wenku8.Model.Section
{
    using Book;
    using Config;
    using Storage;

    sealed class TOCSection : ActiveData
    {
        public bool AnchorAvailable
        {
            get { return AutoAnchor != null; }
        }

        public Volume[] Volumes { get; private set; }
        public Chapter[] Chapters { get; private set; }
        public Chapter AutoAnchor { get; private set; }

        public IObservableVector<object> VolumeCollections { get; private set; }

        private BookItem CurrentBook;

        public TOCSection( BookItem b )
        {
            CurrentBook = b;
            Volumes = b.GetVolumes();

            if ( Properties.MISC_CHUNK_SINGLE_VOL )
                VirtualizeVolumes();

            SetAutoAnchor();
        }

        // This groups 30+ ChapterList to virtual volumes for easier navigation
        private void VirtualizeVolumes()
        {
            int l = Volumes.Count();
            if ( l == 0 || !( l == 1 && 30 < Volumes.First().ChapterList.Count() ) ) return;
            Volumes = VirtualVolume.Create( Volumes.First() );
        }

        public void SelectVolume( Volume v )
        {
            Chapters = v.ChapterList;
            NotifyChanged( "Chapters" );
        }

        public void SetViewSource( CollectionViewSource ViewSource )
        {
            ViewSource.Source = Volumes.Remap( x => new ChapterGroup( x ) );

            VolumeCollections = ViewSource.View.CollectionGroups;
            NotifyChanged( "VolumeCollections" );
        }

        public void SetAutoAnchor()
        {
            // Set the autoanchor
            string AnchorId = new AutoAnchor( CurrentBook ).GetAutoVolAnc();

            foreach ( Volume V in Volumes )
            {
                foreach ( Chapter C in V.ChapterList )
                {
                    if ( C.cid == AnchorId )
                    {
                        AutoAnchor = C;
                        goto EndLoop;
                    }
                }
            }
            EndLoop:

            NotifyChanged( "AnchorAvailable" );
        }

        internal class ChapterGroup : List<Chapter>
        {
            public Volume Vol { get; set; }

            public ChapterGroup( Volume V )
                : base( V.ChapterList )
            {
                Vol = V;
            }
        }
    }
}