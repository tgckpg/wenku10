using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using wenku10.Pages;
using wenku10.Pages.Sharers;

namespace wenku8.Model.Pages
{
    using Book;
    using Book.Spider;
    using Ext;
    using ListItem;
    using ListItem.Sharers;
    using Loaders;
    using Section;

    sealed class ItemProcessor
    {
        public static NameValue<Func<Page>> GetPageHandler( object Item )
        {
            if ( Item is HubScriptItem )
            {
                HubScriptItem HSI = ( HubScriptItem ) Item;
                return new NameValue<Func<Page>>( PageId.SCRIPT_DETAILS, () => new ScriptDetails( HSI ) );
            }

            else if( Item is BookInfoItem )
            {
                BookInfoItem BItem = ( BookInfoItem ) Item;
                return new NameValue<Func<Page>>(
                    PageId.BOOK_INFO_VIEW
                    , () => new BookInfoView( X.Instance<BookItemEx>( XProto.BookItemEx, BItem.Payload ) )
                );
            }

            return new NameValue<Func<Page>>( PageId.NULL, () => null );
        }

        public static async Task ProcessLocal( LocalBook LB )
        {
            await LB.Process();
            if ( LB is SpiderBook )
            {
                SpiderBook SB = ( SpiderBook ) LB;
                BookInstruction BS = SB.GetBook();
                if ( BS.Packable )
                {
                    BS.PackVolumes( SB.GetPPConvoy() );
                }
            }
        }

        public static async Task<BookItem> GetBookFromId( string Id )
        {
            Guid _Guid;
            int _Id;

            bool IsBookSpider = false;
            IsBookSpider = Guid.TryParse( Id, out _Guid );

            if ( !IsBookSpider && Id.Contains( '/' ) )
            {
                string[] ZSId = Id.Split( '/' );
                IsBookSpider = ZSId.Length == 2 && ZSId[ 0 ][ 0 ] == BookSpiderList.ZONE_PFX;
            }

            if( IsBookSpider )
            {
                SpiderBook Book = await SpiderBook.CreateAsyncSpider( Id );
                if( Book.ProcessSuccess ) return Book.GetBook();
            }
            else if( int.TryParse( Id, out _Id ) )
            {
                // Try LocalDocument first
                LocalTextDocument Doc = new LocalTextDocument( Id );
                if ( Doc.IsValid ) return new BookItem( Doc );

                // Try for Ex function
                else if ( X.Exists ) return GetBookEx( Id );
            }

            return null;
        }

        public static BookItem GetBookEx( string Id )
        {
            BookItem B = X.Instance<BookItem>( XProto.BookItemEx, Id );
            B.XSetProp( "Mode", X.Const<string>( XProto.WProtocols, "ACTION_BOOK_META" ) );

            return B;
        }

        public static async Task<HubScriptItem> GetScriptFromHub( string Id, string Token )
        {
            SHSearchLoader SHLoader = new SHSearchLoader( "uuid: " + Id, new string[] { Token } );
            IEnumerable<HubScriptItem> HSIs = await SHLoader.NextPage( 1 );

            return HSIs.FirstOrDefault();
        }

    }
}