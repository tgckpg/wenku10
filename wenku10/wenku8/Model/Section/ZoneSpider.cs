using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

using libtaotu.Controls;

namespace wenku8.Model.Section
{
    using Book;
    using Loaders;
    using Book.Spider;

    sealed class ZoneSpider : ActiveData
    {
        public static readonly string ID = typeof( ZoneSpider ).Name;

        public string ZoneId { get { return PM.GUID; } }

        public Observables<BookItem, BookItem> Data { get; private set; }

        private ProcManager PM;

        private bool _IsLoading = false;
        public bool IsLoading
        {
            get { return _IsLoading; }
            private set
            {
                _IsLoading = value;
                NotifyChanged( "IsLoading" );
            }
        }

        public async void OpenFile()
        {
            try
            {
                IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
                if ( ISF == null ) return;

                IsLoading = true;

                XParameter Param = new XRegistry( await ISF.ReadString(), null, false ).Parameter( "Procedures" );
                PM = new ProcManager( Param );

                ZSFeedbackLoader<BookItem> ZSF = new ZSFeedbackLoader<BookItem>( PM.CreateSpider() );
                Data = new Observables<BookItem, BookItem>( await ZSF.NextPage() );
                Data.ConnectLoader( ZSF );

                IsLoading = false;
                Data.LoadStart += ( s, e ) => IsLoading = true;
                Data.LoadEnd += ( s, e ) => IsLoading = false;

                NotifyChanged( "Data" );
            }
            catch( Exception ex )
            {
                IsLoading = false;
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }
        }

    }
}