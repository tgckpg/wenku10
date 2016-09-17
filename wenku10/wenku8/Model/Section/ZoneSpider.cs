using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using libtaotu.Controls;
using libtaotu.Models.Procedure;
using static libtaotu.Pages.ProceduresPanel;

namespace wenku8.Model.Section
{
    using Book;
    using Loaders;
    using Taotu;
    using Settings;

    sealed class ZoneSpider : ActiveData
    {
        public static readonly string ID = typeof( ZoneSpider ).Name;

        public string ZoneId { get { return PM.GUID; } }
        public string MetaLocation { get { return FileLinks.ROOT_ZSPIDER + ZoneId + ".xml"; } }

        public bool DataReady { get; private set; }
        public Observables<BookItem, BookItem> Data { get; private set; }

        public ObservableCollection<Procedure> ProcList { get { return PM?.ProcList; } }
        public Uri Banner { get; private set; }

        private ProcManager PM;

        public string Message { get; private set; }

        private int loadLevel = 0;
        public bool IsLoading
        {
            get { return 0 < loadLevel; }
            private set
            {
                loadLevel += value ? 1 : -1;
                NotifyChanged( "IsLoading" );
            }
        }

        public ZoneSpider()
        {
            MessageBus.OnDelivery += MessageBus_OnDelivery;
        }

        ~ZoneSpider()
        {
            MessageBus.OnDelivery -= MessageBus_OnDelivery;
        }

        private void MessageBus_OnDelivery( Message Mesg )
        {
            if ( Mesg.Payload is PanelLog )
            {
                PanelLog PLog = ( PanelLog ) Mesg.Payload;
                Message = Mesg.Content;
                NotifyChanged( "Message" );
            }
        }

        private void SetBanner()
        {
            WenkuListLoader PLL = ( WenkuListLoader ) ProcList.FirstOrDefault( x => x is WenkuListLoader );

            if ( PLL == null )
            {
                throw new InvalidFIleException();
            }

            Banner = PLL.BannerSrc;
            NotifyChanged( "Banner" );
        }

        public void Reset()
        {
            if ( Data != null )
            {
                Data.DisconnectLoaders();
                Data.Clear();
            }

            DataReady = false;
            NotifyChanged( "Data", "DataReady" );
        }

        public async Task Init()
        {
            if ( DataReady ) return;

            IsLoading = true;
            try
            {
                ZSFeedbackLoader<BookItem> ZSF = new ZSFeedbackLoader<BookItem>( PM.CreateSpider() );
                Data = new Observables<BookItem, BookItem>( await ZSF.NextPage() );
                Data.ConnectLoader( ZSF );

                Data.LoadStart += ( s, e ) => IsLoading = true;
                Data.LoadEnd += ( s, e ) => IsLoading = false;

                DataReady = true;
                NotifyChanged( "Data", "DataReady" );
            }
            finally
            {
                IsLoading = false;
            }
        }

        public bool Open( XRegistry ZDef )
        {
            IsLoading = true;

            try
            {
                XParameter Param = ZDef.Parameter( "Procedures" );
                PM = new ProcManager( Param );
                NotifyChanged( "ProcList" );

                SetBanner();

                return true;
            }
            catch( InvalidFIleException )
            {
                ProcManager.PanelMessage( ID, () => Res.RSTR( "InvalidXML" ), LogType.ERROR );
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }
            finally
            {
                IsLoading = false;
            }

            return false;
        }

        private class InvalidFIleException : Exception { }

    }
}