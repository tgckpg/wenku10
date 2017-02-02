using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

using Net.Astropenguin.Loaders;

using wenku8.Model.Book;
using wenku8.Resources;

namespace Tasks
{
    sealed class LiveTileService
    {
        internal static async Task UpdateTile( IDisposable CanvasDevice, BookItem Book, string TileId )
        {
            TileUpdater Updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile( TileId );
            Updater.EnableNotificationQueue( true );
            Updater.Clear();

            StringResBg stx = new StringResBg( "Message" );

            XmlDocument Template150 = TileUpdateManager.GetTemplateContent( TileTemplateType.TileSquare150x150Text01 );
            Template150.GetElementsByTagName( "text" ).First().AppendChild( Template150.CreateTextNode( stx.Str( "NewContent" ) ) );
            Updater.Update( new TileNotification( Template150 ) );

            XmlDocument Template71 = TileUpdateManager.GetTemplateContent( TileTemplateType.TileSquare71x71Image );
            IXmlNode ImgSrc = Template71.GetElementsByTagName( "image" )
                .FirstOrDefault()?.Attributes
                .FirstOrDefault( x => x.NodeName == "src" );

            string SmallTile = await Image.LiveTileBadgeImage( CanvasDevice, Book, 71, 71, "\uEDAD" );
            if ( !string.IsNullOrEmpty( SmallTile ) )
            {
                ImgSrc.NodeValue = SmallTile;
                Updater.Update( new TileNotification( Template71 ) );
            }

        }

    }
}