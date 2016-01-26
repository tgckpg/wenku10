using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;

namespace wenku8.Model.Section
{
    using ListItem;
    using Resources;
    using Settings;
    using Storage;

    partial class LocalFileList : SearchableContext
    {
        private string _loading = null;

        public bool FavOnly { get; private set; }

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
            IEnumerable<string> ids = Shared.Storage.ListDirs( FileLinks.ROOT_LOCAL_VOL );
            string[] favs = new BookStorage().GetIdList();

            List<LocalBook> Items = new List<LocalBook>();
            foreach( string id in ids )
            {
                LocalBook LB = new LocalBook( id );
                if( LB.ProcessSuccess )
                {
                    Items.Add( LB );
                    LB.IsFav = favs.Contains( id );
                }
            }

            ids = Shared.Storage.ListDirs( FileLinks.ROOT_SPIDER_VOL );
            foreach( string id in ids )
            {
                SpiderBook LB = new SpiderBook( id );
                if( LB.ProcessSuccess )
                {
                    Items.Add( LB );
                    LB.IsFav = favs.Contains( id );
                }
            }

            if( 0 < Items.Count ) SearchSet = Items;
        }

        public async void Load()
        {
            BookStorage BS = new BookStorage();
            string[] ids = BS.GetIdList();
            IEnumerable<LocalBook> Items = await Shared.Storage.GetLocalText( async ( x, i, l ) =>
            {
                if( i % 20 == 0 )
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
            foreach( LocalBook b in SearchSet )
            {
                await b.Process();
                if( Terminate ) break;
            }
            Terminate = true;
            Processing = false;
            NotifyChanged( "Processing" );
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

        public void ToggleFavs()
        {
            if ( !FavOnly )
            {
                BookStorage BS = new BookStorage();
                string[] ids = BS.GetIdList();

                List<ActiveItem> SS = new List<ActiveItem>();

                foreach ( string id in ids )
                {
                    if ( Data != null && Data.Any( x => ( x as LocalBook ).aid == id ) )
                    {
                        continue;
                    }

                    LocalBook LB = new LocalBook( id );
                    if ( !( LB.CanProcess || LB.ProcessSuccess ) )
                    {
                        XParameter Param = BS.GetBook( id );
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
    }
}
