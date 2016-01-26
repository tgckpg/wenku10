using System.Collections.Generic;
using System.Linq;

using Net.Astropenguin.DataModel;

namespace wenku8.Model.Section
{
    using Book;
    using Storage;

    class TOCSection : ActiveData
    {
        public bool AnchorAvailable
        {
            get { return AutoAnchor != null; }
        }

        public Volume[] Volumes { get; private set; }
        public Chapter[] Chapters { get; private set; }
        public Chapter AutoAnchor { get; private set; }

        private BookItem CurrentBook;

        public TOCSection( BookItem b )
        {
            CurrentBook = b;
            Volumes = b.GetVolumes();
        }

        public void SelectVolume( Volume v )
        {
            Chapters = v.ChapterList;
            NotifyChanged( "Chapters" );

            // Set the autoanchor
            BookStorage BS = new BookStorage();
            string AnchorId = BS.GetBookmark( CurrentBook.Id );

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
    }
}
