using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Section
{
    using AdvDM;
    using Model;
    using Model.ListItem;
    using Model.REST;
    using Resources;
    using System;

    sealed class SharersHub : SearchableContext
    {
        private readonly string ID = typeof( SharersHub ).Name;

        RuntimeCache RCache;

        private string _mesg;
        public string Message
        {
            get { return _mesg; }
            private set
            {
                _mesg = value;
                NotifyChanged( "Message" );
            }
        }

        private string Query;

        override public string SearchTerm
        {
            get { return Query; }
            set
            {
                Query = value;
                Search( Query );
            }
        }

        public SharersHub()
        {
            RCache = new RuntimeCache();
        }

        private void Search( string Query )
        {
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.Search( Query )
                , SearchResponse
                , Utils.DoNothing
                , false
            );
        }

        private void SearchResponse( DRequestCompletedEventArgs e, string id )
        {
            try
            {
                JsonObject JResponse = JsonStatus.Parse( e.ResponseString );
                Data = JResponse.GetNamedArray( "data" ).Remap( x => new HubScriptItem( x.GetObject() ) );
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.INFO );
                Message = ex.Message;
            }

            NotifyChanged( "SearchSet" );
        }

        private void SearchItemUpdate( DRequestCompletedEventArgs e, string Id )
        {
            HubScriptItem Target;
            if ( TryNotGetId( Id, out Target ) ) return;

            try
            {
                JsonObject JResponse = JsonStatus.Parse( e.ResponseString );
                Target.Update( JResponse.GetNamedArray( "data" ).First().GetObject() );
            }
            catch ( Exception ex )
            {
                Target.ErrorMessage = ex.Message;
            }
        }

        public void ReportStatus( string Id, SharersRequest.StatusType SType, string Desc = "" )
        {
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.StatusReport( Id, ( ( int ) SType ).ToString(), Desc )
                , ReportSuccess
                , ReportFailed
                , false
            );
        }

        private void ReportFailed( string CacheName, string Id, Exception e )
        {
            global::System.Diagnostics.Debugger.Break();
        }

        private void ReportSuccess( DRequestCompletedEventArgs e, string Id )
        {
            HubScriptItem Target;
            if ( TryNotGetId( Id, out Target ) ) return;

            try
            {
                JsonStatus.Parse( e.ResponseString );
                RCache.POST(
                    Shared.ShRequest.Server
                    , Shared.ShRequest.Search( "uuid: " + Id )
                    // Pass the uuid instead of the query id
                    , ( re, q ) => SearchItemUpdate( re, Id )
                    , Utils.DoNothing
                    , false
                );
            }
            catch( Exception ex )
            {
                Target.ErrorMessage = ex.Message;
            }
        }

        protected override IEnumerable<ActiveItem> Filter( IEnumerable<ActiveItem> Items )
        {
            return Items;
        }

        private bool TryNotGetId( string Id, out HubScriptItem Target )
        {
            Target = Data.Cast<HubScriptItem>().FirstOrDefault( ( HubScriptItem HSI ) => HSI.Id == Id );

            if( Target == null )
            {
                Logger.Log( ID, "Target is gone after Status Report returned", LogType.WARNING );
                return true;
            }

            return false;
        }
    }
}