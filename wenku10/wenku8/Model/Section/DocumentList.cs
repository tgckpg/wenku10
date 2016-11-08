using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Section
{
    using AdvDM;
    using ListItem;
    using Resources;
    using Storage;
    using Settings;

    sealed class DocumentList : LocalListBase 
    {
        public static readonly string ID = typeof( DocumentList ).Name;

        public DocumentList()
        {
            ProcessVols();
        }

        private async void ProcessVols()
        {
            StringResources stx = new StringResources( "LoadingMessage" );
            string LoadText = stx.Str( "ProgressIndicator_Message" );

            IEnumerable<string> BookIds = Shared.Storage.ListDirs( FileLinks.ROOT_LOCAL_VOL );
            string[] favs = new BookStorage().GetIdList();

            List<LocalBook> Items = new List<LocalBook>();
            foreach ( string Id in BookIds )
            {
                Loading = LoadText + ": " + Id;
                LocalBook LB = await LocalBook.CreateAsync( Id );
                if ( LB.ProcessSuccess )
                {
                    Items.Add( LB );
                    LB.IsFav = favs.Contains( Id );
                }
            }

            if ( 0 < Items.Count ) SearchSet = Items;
            Loading = null;
        }
        
        public async void OpenDirectory()
        {
            BookStorage BS = new BookStorage();
            string[] ids = BS.GetIdList();
            IEnumerable<LocalBook> Items = await Shared.Storage.GetLocalText( async ( x, i, l ) =>
            {
                if ( i % 20 == 0 )
                {
                    Worker.UIInvoke( () => Loading = string.Format( "{0}/{1}", i, l ) );
                    await Task.Delay( 15 );
                }

                LocalBook LB = new LocalBook( x );
                LB.IsFav = ids.Contains( LB.aid );
                return LB;
            } );

            Loading = null;
            if ( Items != null && 0 < Items.Count() ) SearchSet = Items;
        }

        public void LoadUrl( DownloadBookContext Context )
        {
            RuntimeCache rCache = new RuntimeCache();
            Logger.Log( ID, Context.Id, LogType.DEBUG );
            Logger.Log( ID, Context.Title, LogType.DEBUG );

            Worker.UIInvoke( () =>
            {
                StringResources stx = new StringResources( "AdvDM" );
                Loading = stx.Text( "Active" );
            } );

            rCache.GET( Context.Url, ( e, url ) =>
            {
                Loading = null;
                SaveTemp( e, Context );
            }
            , ( id, url, ex ) =>
            {
                Logger.Log( ID, "Cannot download: " + id, LogType.WARNING );
                Logger.Log( ID, ex.Message, LogType.WARNING );

            }, false );
        }

        private async void SaveTemp( DRequestCompletedEventArgs e, DownloadBookContext Context )
        {
            StorageFile ISF = await AppStorage.MkTemp(
                Context.Id
                + ". "
                + ( string.IsNullOrEmpty( Context.Title ) ? "{ Parse Needed }" : Context.Title )
                + ".txt"
            );

            await ISF.WriteBytes( e.ResponseBytes );

            SearchSet = new LocalBook[] { new LocalBook( ISF ) };
        }

    }
}