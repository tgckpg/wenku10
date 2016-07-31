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
                SpiderBook SBook = await SpiderBook.ImportFile( await ISF.ReadString() );

                List<LocalBook> NData;
                if( Data != null )
                {
                    NData = new List<LocalBook>( Data.Cast<LocalBook>() );
                    if ( NData.Any( x => x.aid == SBook.aid ) )
                    {
                        Logger.Log( ID, "Already in collection, updating the data", LogType.DEBUG );
                        NData.Remove( NData.First( x => x.aid == SBook.aid ) );
                    }
                }
                else
                {
                    NData = new List<LocalBook>();
                }

                NData.Add( SBook );
                Data = NData;
                NotifyChanged( "SearchSet" );
                return SBook.CanProcess || SBook.ProcessSuccess;
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