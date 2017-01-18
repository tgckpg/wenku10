using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Controls;

using wenku10.Pages;
using wenku10.Pages.Sharers;

using Tasks;

namespace wenku8.Model.Pages
{
    using Book;
    using Ext;
    using ListItem;
    using ListItem.Sharers;
    using Loaders;

    sealed class PageProcessor
    {
        public static async Task<HubScriptItem> GetScriptFromHub( string Id, string Token )
        {
            SHSearchLoader SHLoader = new SHSearchLoader( "uuid: " + Id, new string[] { Token } );
            IEnumerable<HubScriptItem> HSIs = await SHLoader.NextPage( 1 );

            return HSIs.FirstOrDefault();
        }

        public static NameValue<Func<Page>> GetPageHandler( object Item )
        {
            if ( Item is HubScriptItem )
            {
                HubScriptItem HSI = ( HubScriptItem ) Item;
                return new NameValue<Func<Page>>( PageId.SCRIPT_DETAILS, () => new ScriptDetails( HSI ) );
            }

            else if ( Item is BookInfoItem )
            {
                BookInfoItem BItem = ( BookInfoItem ) Item;
                return new NameValue<Func<Page>>(
                    PageId.BOOK_INFO_VIEW
                    , () => new BookInfoView( X.Instance<BookItem>( XProto.BookItemEx, BItem.Payload ) )
                );
            }

            return new NameValue<Func<Page>>( PageId.NULL, () => null );
        }

        public static Task<string> PinToStart( BookItem Book )
        {
            if ( Book.IsSpider() )
            {
                return CreateSecondaryTile( Book );
            }
            else if ( Book.IsLocal() )
            {
                // TODO
            }
            else if ( X.Exists )
            {
                Task<string> PinTask = ( Task<string> ) X.Method( XProto.ItemProcessorEx, "CreateTile" ).Invoke( null, new BookItem[] { Book } );
                return PinTask;
            }

            return null;
        }

        public static void ReadSecondaryTile( BookItem Book )
        {
            if( Book.IsSpider() )
            {
                BackgroundProcessor.Instance.ClearTileStatus( Book.Id );
            }
        }

        private static async Task<string> CreateSecondaryTile( BookItem Book )
        {
            string TilePath = await Resources.Image.CreateTileImage( Book );
            string TileId = "ShellTile.grimoire." + System.Utils.Md5( Book.Id );

            SecondaryTile S = new SecondaryTile()
            {
                TileId = TileId
                , DisplayName = Book.Title
                , Arguments = "spider|" + Book.Id
            };

            S.VisualElements.Square150x150Logo = new Uri( TilePath );
            S.VisualElements.ShowNameOnSquare150x150Logo = true;

            if ( await S.RequestCreateAsync() ) return TileId;

            return null;
        }

    }
}