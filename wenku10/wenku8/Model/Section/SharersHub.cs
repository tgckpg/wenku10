using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

namespace wenku8.Section
{
    using AdvDM;
    using Ext;
    using Model.ListItem;
    using Model.Loaders;
    using Model.REST;
    using Resources;
    using Settings;
    using System;

    sealed class SharersHub : ActiveData
    {
        private readonly string ID = typeof( SharersHub ).Name;

        RuntimeCache RCache;

        public IMember Member;

        public ObservableCollection<NameValue<Action>> Activities { get; private set; }
        public Observables<HubScriptItem, HubScriptItem> SearchSet { get; private set; }
        public IEnumerable<SHGrant> Grants { get; private set; }

        private bool _Loading = false;
        public bool Loading
        {
            get { return _Loading; }
            private set
            {
                _Loading = value;
                NotifyChanged( "Loading" );
            }
        }

        private bool _Searching = false;
        public bool Searching
        {
            get { return _Searching; }
            private set
            {
                _Searching = value;
                NotifyChanged( "Searching" );
            }
        }

        public bool LoggedIn { get { return Member.IsLoggedIn; } }
        public string LLText { get; private set; }

        public SharersHub()
        {
            Activities = new ObservableCollection<NameValue<Action>>();
            SearchSet = new Observables<HubScriptItem, HubScriptItem>();

            RCache = new RuntimeCache();
            Member = X.Singleton<IMember>( XProto.SHMember );
            Member.OnStatusChanged += Member_OnStatusChanged;

            MessageBus.OnDelivery += MessageBus_OnDelivery;

            SearchSet.LoadStart += ( s, e ) => { Searching = true; };
            SearchSet.LoadEnd += ( s, e ) => { Searching = false; };

            UpdateLLText();
        }

        public async void CheckActivity( NameValue<Action> Activity )
        {
            Activity.Value();

            // Roughly wait a moment then remove it
            await Task.Delay( 500 );
            Activities.Remove( Activity );
            NotifyChanged( "Activities" );
        }

        ~SharersHub()
        {
            Member.OnStatusChanged -= Member_OnStatusChanged;
            MessageBus.OnDelivery -= MessageBus_OnDelivery;
        }

        private void Member_OnStatusChanged( object sender, MemberStatus args )
        {
            UpdateLLText();
            if( Member.IsLoggedIn )
            {
                GetMyRequests();
            }
        }

        public void GetMyRequests()
        {
            Loading = true;
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.MyRequests()
                , ( a, b ) =>
                {
                    RequestsStatus( a, b );
                    Loading = false;
                }
                , ( a, b, c ) => { Loading = false; }
                , false
            );
        }

        private void RequestsStatus( DRequestCompletedEventArgs e, string QId )
        {
            try
            {
                int NGrants = 0;
                int NScripts = 0;
                JsonObject JMesg = JsonStatus.Parse( e.ResponseString );
                JsonArray JData = JMesg.GetNamedArray( "data" );
                Grants = JData.Remap( x =>
                {
                    SHGrant G = new SHGrant( x.GetObject() );

                    int l = G.Grants.Length;
                    if ( 0 < l ) NScripts++;
                    NGrants += l;

                    return G;
                } );

                if ( 0 < NGrants )
                {
                    AddActivity( () =>
                    {
                        StringResources stx = new StringResources();
                        return string.Format( stx.Text( "GrantsReceived" ), NGrants, NScripts );
                    }, () => MessageBus.SendUI( new Message( typeof( SharersHub ), AppKeys.SH_SHOW_GRANTS ) ) );
                }
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.WARNING );
            }
        }

        private void AddActivity( Func<string> StxText, Action A )
        {
            Worker.UIInvoke( () =>
            {
                Activities.Add( new NameValue<Action>( StxText(), A ) );
                NotifyChanged( "Activities" );
            } );
        }

        public async void Search( string Query, IEnumerable<string> AccessTokens = null )
        {
            if ( AccessTokens == null )
                AccessTokens = new TokenManager().AuthList.Remap( x => x.Value );

            Searching = true;
            SHSearchLoader SHLoader = new SHSearchLoader( Query, AccessTokens );

            SearchSet.ConnectLoader( SHLoader );
            SearchSet.UpdateSource( await SHLoader.NextPage() );

            Searching = false;
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
                    , Shared.ShRequest.Search( "uuid: " + Id, 0, 1 )
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

        private bool TryNotGetId( string Id, out HubScriptItem Target )
        {
            Target = SearchSet.Cast<HubScriptItem>().FirstOrDefault( ( HubScriptItem HSI ) => HSI.Id == Id );

            if( Target == null )
            {
                Logger.Log( ID, "Target is gone after Status Report returned", LogType.WARNING );
                return true;
            }

            return false;
        }

        private void UpdateLLText()
        {
            StringResources stx = new StringResources( "AppResources", "Settings" );
            LLText = LoggedIn ? stx.Text( "Account_Logout", "Settings" ) : stx.Text( "Login" );
            NotifyChanged( "LLText", "LoggedIn" );
        }

        private void MessageBus_OnDelivery( Message Mesg )
        {
            if ( Mesg.Content != AppKeys.SP_PROCESS_COMP ) return;

            LocalBook SBook = ( LocalBook ) Mesg.Payload;
            HubScriptItem HSC = SearchSet.Cast<HubScriptItem>().FirstOrDefault( x => x.Id == SBook.aid );

            if ( HSC == null )
            {
                Logger.Log( ID, "Book is not from Sharer's hub", LogType.DEBUG );
                return;
            }

            HSC.InCollection = SBook.ProcessSuccess;
        }
    }
}