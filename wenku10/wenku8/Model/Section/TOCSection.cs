using System.Collections.Generic;
using System.Linq;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;

namespace wenku8.Model.Section
{
    using System;
    using Book;
    using Storage;
    using Windows.Foundation.Collections;
    using Windows.UI.Xaml.Data;

    class TOCSection : ActiveData
    {
        public bool AnchorAvailable
        {
            get { return AutoAnchor != null; }
        }

        public Volume[] Volumes { get; private set; }
        public Chapter[] Chapters { get; private set; }
        public Chapter AutoAnchor { get; private set; }

        public Converters.TOCTemplateSelector TemplateSelector { get; set; }
        public IObservableVector<object> VolumeCollections { get; private set; }

        private BookItem CurrentBook;

        public TOCSection( BookItem b )
        {
            TemplateSelector = new Converters.TOCTemplateSelector();

            CurrentBook = b;
            Volumes = b.GetVolumes();

            SetAutoAnchor();
        }

        public void SelectVolume( Volume v )
        {
            Chapters = v.ChapterList;
            NotifyChanged( "Chapters" );
        }

        public void SetViewSource( CollectionViewSource ViewSource )
        {
            if ( TemplateSelector.IsHorizontal ) return;

            ViewSource.Source = Volumes.Remap( x => new ChapterGroup( x ) );

            VolumeCollections = ViewSource.View.CollectionGroups;
            NotifyChanged( "VolumeCollections" );
        }

        public void SetAutoAnchor()
        {
            // Set the autoanchor
            string AnchorId = new AutoAnchor().GetBookmark( CurrentBook.Id );

            foreach( Volume V in Volumes )
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
                :base( V.ChapterList )
            {
                Vol = V;
            }
        }
    }
}
