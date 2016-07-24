using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Section
{
    using ListItem;

    sealed partial class LocalFileList 
    {
        public static readonly string ID = typeof( LocalFileList ).Name;

        public async Task<bool> OpenSpider( IStorageFile ISF )
        {
            try
            {
                SpiderBook SBook = await SpiderBook.CreateAsnyc( await ISF.ReadString(), true );

                List<ActiveItem> NData;
                if( Data != null )
                {
                    if ( Data.Cast<LocalBook>().Any( x => x.aid == SBook.aid ) )
                    {
                        Logger.Log( ID, "Already in collection", LogType.DEBUG );
                        return true;
                    }

                    NData = new List<ActiveItem>( Data );
                }
                else
                {
                    NData = new List<ActiveItem>();
                }

                NData.Add( SBook );
                Data = NData;
                NotifyChanged( "SearchSet" );
                return true;
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }

            return false;
        }

        public async void OpenSpider()
        {
            IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
            if ( ISF == null ) return;

            var j = OpenSpider( ISF );
        }
    }
}