using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku10.Pages;

namespace wenku8.Model.Section
{
    using AdvDM;
    using Book.Spider;
    using ListItem;
    using Resources;
    using Settings;
    using Storage;

    sealed partial class LocalFileList : SearchableContext
    {
        public const char ZONE_PFX = 'Z';

        public bool FavOnly { get; private set; }

        private string _loading = null;
        public string Loading
        {
            get { return _loading; }
            private set
            {
                _loading = value;
                NotifyChanged( "Loading" );
            }
        }

        public LocalFileList()
        {
            ProcessVols();
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

        public void LoadUrl( LocalModeTxtList.DownloadBookContext Context )
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

        private async void SaveTemp( DRequestCompletedEventArgs e, LocalModeTxtList.DownloadBookContext Context )
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

        public bool Processing { get; private set; }

        private bool _term = true;
        public bool Terminate
        {
            get { return _term; }
            set
            {
                _term = value;
                NotifyChanged( "Terminate" );
            }
        }

        public async void ProcessAll()
        {
            if ( Processing || SearchSet == null ) return;
            Processing = true;
            Terminate = false;
            NotifyChanged( "Processing" );
            ActiveItem[] Books = SearchSet.ToArray();
            foreach ( LocalBook b in Books )
            {
                await b.Process();
                if ( Terminate ) break;
            }
            Terminate = true;
            Processing = false;
            NotifyChanged( "Processing" );
        }

        public LocalBook GetById( string Id )
        {
            return Data?.Cast<LocalBook>().FirstOrDefault( x => x.aid == Id );
        }

        protected override IEnumerable<ActiveItem> Filter( IEnumerable<ActiveItem> Items )
        {
            if ( Items != null && FavOnly )
            {
                string[] ids = new BookStorage().GetIdList();
                Items = Items.Where( x => ids.Contains( ( x as LocalBook ).aid ) );
            }

            return base.Filter( Items );
        }

        public void CleanUp()
        {
            Data = Data.Where( x =>
            {
                LocalBook b = x as LocalBook;
                return b.CanProcess || ( FavOnly && b.IsFav ) || ( b.Processed && b.ProcessSuccess );
            } );

            NotifyChanged( "SearchSet" );
        }

        public async Task ToggleFavs()
        {
            if ( !FavOnly )
            {
                BookStorage BS = new BookStorage();
                string[] BookIds = BS.GetIdList();

                List<ActiveItem> SS = new List<ActiveItem>();

                foreach ( string Id in BookIds )
                {
                    if ( Data != null && Data.Any( x => ( x as LocalBook ).aid == Id ) )
                    {
                        continue;
                    }

                    LocalBook LB = await LocalBook.CreateAsync( Id );
                    if ( !( LB.CanProcess || LB.ProcessSuccess ) )
                    {
                        XParameter Param = BS.GetBook( Id );
                        LB.Name = Param.GetValue( AppKeys.GLOBAL_NAME );
                        LB.Desc = "Source is unavailable";
                        LB.CanProcess = false;
                    }

                    LB.IsFav = true;
                    SS.Add( LB );
                }

                if ( 0 < SS.Count )
                {
                    if ( Data == null ) Data = SS;
                    else Data = Data.Concat( SS );
                }

                FavOnly = true;
            }
            else
            {
                FavOnly = false;
                if ( Data != null )
                {
                    Data = Data.Where( x =>
                    {
                        LocalBook LB = x as LocalBook;
                        if ( LB.IsFav ) return LB.ProcessSuccess || LB.Processing || LB.CanProcess;

                        return true;
                    } );
                }
            }

            NotifyChanged( "SearchSet" );
        }

        public void Add( params LocalBook[] Book )
        {
            List<LocalBook> NData = new List<LocalBook>();

            if ( Data != null )
                NData.AddRange( Data.Cast<LocalBook>() );

            NData.AddRange( Book );
            Data = NData;

            NotifyChanged( "SearchSet" );
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

            Action<string, SpiderBook> ProcessSpider = ( Id, LB ) =>
             {
                 Loading = LoadText + ": " + Id;
                 if ( LB.aid != Id )
                 {
                     try
                     {
                         Logger.Log( ID, "Fixing misplaced spider book" );
                         Shared.Storage.MoveDir( FileLinks.ROOT_SPIDER_VOL + Id, LB.MetaLocation );
                     }
                     catch ( Exception ex )
                     {
                         Logger.Log( ID
                             , string.Format(
                                 "Unable to move script: {0} => {1}, {2} "
                                 , Id, LB.aid, ex.Message )
                                 , LogType.WARNING );
                     }
                 }

                 if ( LB.ProcessSuccess || LB.CanProcess )
                 {
                     Items.Add( LB );
                     LB.IsFav = favs.Contains( Id );
                 }
                 else
                 {
                     try
                     {
                         Logger.Log( ID, "Removing invalid script: " + Id, LogType.INFO );
                         Shared.Storage.RemoveDir( LB.MetaRoot );
                     }
                     catch ( Exception ex )
                     {
                         Logger.Log( ID, "Cannot remove invalid script: " + ex.Message, LogType.WARNING );
                     }
                 }
             };

            BookIds = Shared.Storage.ListDirs( FileLinks.ROOT_SPIDER_VOL );
            foreach ( string Id in BookIds )
            {
                if ( Id[ 0 ] == ZONE_PFX )
                {
                    IEnumerable<string> ZoneItems = Shared.Storage.ListDirs( FileLinks.ROOT_SPIDER_VOL + Id + "/" );
                    foreach ( string SId in ZoneItems )
                    {
                        /**
                         * This code is a mess. I'll explain a bit more in here
                         *   First, the location of the Book.MetaLocation for ZoneItems
                         *   can only be retrived from BookInstruction
                         *   However ZoneId and Id are assinged by Spider on the fly,
                         *   restoring this information is a bit tricky
                         */

                        // Create BookIntstruction just to retrieve the correct id pattern
                        BookInstruction BInst = new BookInstruction( Id, SId );

                        /**
                         * After 2 hours of investigations...
                         * Welp, just outsmarted by myself, The CreateAsyncSpide works because:
                         *   Inside the TestProcessed method, the BookInstruction are created
                         *   using BoockInstruction( Id, Setings ) overload
                         *   the provided id is "this.aid" here BUT the full id is restored again
                         *   in InitProcMan() method
                         *   Fortunately, ssid will be set correctly inside the ReadInfo method
                         */
                        ProcessSpider( BInst.Id, await SpiderBook.CreateAsyncSpider( BInst.Id ) );
                    }
                }
                else
                {
                    ProcessSpider( Id, await SpiderBook.CreateAsyncSpider( Id ) );
                }
            }

            if ( 0 < Items.Count ) SearchSet = Items;
            Loading = null;
        }
    }
}