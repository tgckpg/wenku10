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
    using Storage;

    partial class LocalFileList 
    {
        public static readonly string ID = typeof( LocalFileList ).Name;

        public async void OpenSpider()
        {
            IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
            if ( ISF == null ) return;

            try
            {
                SpiderBook SBook = new SpiderBook( await ISF.ReadString(), true );

                List<ActiveItem> NData;
                if( Data != null )
                {
                    NData = new List<ActiveItem>( Data );
                }
                else
                {
                    NData = new List<ActiveItem>();
                }

                NData.Add( SBook );
                Data = NData;
                NotifyChanged( "SearchSet" );
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }
        }
    }
}
