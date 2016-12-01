using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Loaders;

namespace wenku8.Model.Loaders
{
    using AdvDM;
    using Interfaces;
    using ListItem;
    using Settings;

    sealed class ContentIllusLoader
    {
        public static ContentIllusLoader Instance { get; private set; }

        private WBackgroundTransfer Transfer;
        private List<IIllusUpdate> ImgThumbs;

        public ContentIllusLoader()
        {
            Transfer = new WBackgroundTransfer();
            Transfer.OnThreadComplete += Transfer_OnThreadComplete;

            ImgThumbs = new List<IIllusUpdate>();
        }

        public static void Initialize()
        {
            if ( Instance == null )
                Instance = new ContentIllusLoader();
        }


        public async void RegisterImage( IIllusUpdate Item )
        {
            lock ( ImgThumbs )
            {
                if ( ImgThumbs.Contains( Item ) ) return;
                ImgThumbs.Add( Item );
            }

            ImageThumb Thumb;
            if ( Item.ImgThumb == null )
            {
                string url = Item.SrcUrl;

                // Use filename as <id>.<format> since format maybe <id>.png or <id>.jpg
                string fileName = url.Substring( url.LastIndexOf( '/' ) + 1 );
                string imageLocation = FileLinks.ROOT_IMAGE + fileName;

                Thumb = new ImageThumb( imageLocation, 200, null );
                Thumb.Reference = url;

                Item.ImgThumb = Thumb;
            }
            else
            {
                Thumb = Item.ImgThumb;
            }

            await Thumb.Set();
            if ( Thumb.IsDownloadNeeded )
            {
                Guid G = await Transfer.RegisterImage( Thumb.Reference, Thumb.Location );
                Thumb.Id = G;
            }
            else
            {
                lock ( ImgThumbs ) ImgThumbs.Remove( Item );
                Item.Update();
            }
        }

        private void Transfer_OnThreadComplete( DTheradCompleteArgs DArgs )
        {
            ClearItem( DArgs.Id );
        }

        private void ClearItem( Guid Id )
        {
            lock ( ImgThumbs )
            {
                IIllusUpdate Item = ImgThumbs.FirstOrDefault( x => x.ImgThumb.Id.Equals( Id ) );
                if( Item != null )
                {
                    Item.Update();
                    ImgThumbs.Remove( Item );
                }
            }
        }

    }
}