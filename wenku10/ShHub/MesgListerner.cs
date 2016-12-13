using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using wenku8.AdvDM;
using wenku8.Model.Book.Spider;
using wenku8.Model.ListItem;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.REST;
using wenku8.Resources;
using wenku8.Settings;

using StatusType = wenku8.Model.REST.SharersRequest.StatusType;
using SHTarget = wenku8.Model.REST.SharersRequest.SHTarget;

namespace wenku10.ShHub
{
    using Pages;
    using Pages.Sharers;

    sealed class MesgListerner
    {
        public MesgListerner()
        {
            MessageBus.OnDelivery += MessageBus_OnDelivery;
        }

        private async void MessageBus_OnDelivery( Message Mesg )
        {
            switch ( Mesg.Content )
            {
                case AppKeys.SH_SCRIPT_DATA:
                    HubScriptItem HSI = ( HubScriptItem ) Mesg.Payload;

                    if ( ( HSI.Scope & SpiderScope.ZONE ) != 0 )
                    {
                        ControlFrame.Instance.NavigateTo(
                            PageId.ZONE_SPIDER_VIEW
                            , () => new ZoneSpidersView()
                            , View => ( ( ZoneSpidersView ) View ).OpenZone( HSI ) );
                        break;
                    }

                    // This will save the book
                    SpiderBook SBook = await SpiderBook.ImportFile( await HSI.ScriptFile.ReadString(), true );

                    if ( SBook.CanProcess )
                    {
                        ConfirmScriptParse( SBook );
                    }
                    else
                    {
                        ConfirmErrorReport( HSI.Id, StatusType.HS_INVALID );
                    }
                    break;

                case AppKeys.HS_DECRYPT_FAIL:
                    StringResources stx = new StringResources( "Message", "ContextMenu" );
                    MessageDialog MsgBox = new MessageDialog( stx.Str( "Desc_DecryptionFailed" ), stx.Str( "DecryptionFailed" ) );

                    HSI = ( HubScriptItem ) Mesg.Payload;
                    bool Place = false;

                    MsgBox.Commands.Add( new UICommand( stx.Text( "PlaceRequest", "ContextMenu" ), ( x ) => { Place = true; } ) );
                    MsgBox.Commands.Add( new UICommand( stx.Str( "OK" ) ) );

                    await Popups.ShowDialog( MsgBox );

                    if ( Place ) TransferRequest( SHTarget.KEY, HSI );
                    break;

                case AppKeys.HS_OPEN_COMMENT:
                    InboxMessage BoxMessage = ( InboxMessage ) Mesg.Payload;
                    ControlFrame.Instance.NavigateTo( PageId.SCRIPT_DETAILS
                        , () => new ScriptDetails( BoxMessage.HubScript )
                        , P => ( ( ScriptDetails ) P ).OpenCommentStack( BoxMessage.CommId )
                    );
                    break;

                case AppKeys.HS_NO_VOLDATA:
                    ConfirmErrorReport( ( ( BookInstruction ) Mesg.Payload ).Id, StatusType.HS_NO_VOLDATA );
                    break;
            }
        }

        private void TransferRequest( SHTarget Target, HubScriptItem HSI )
        {
            ControlFrame.Instance.NavigateTo(
                PageId.SCRIPT_DETAILS
                , () => new ScriptDetails( HSI )
                , ( View ) => {
                    ScriptDetails SD = ( ScriptDetails ) View;
                    SD.UpdateTemplate( HSI );
                    SD.PlaceRequest( Target );
                }
            );
        }

        private async void ConfirmScriptParse( SpiderBook Book )
        {
            StringResources stx = new StringResources( "Message" );
            MessageDialog MsgBox = new MessageDialog( stx.Str( "ConfirmScriptParse" ) );

            bool Parse = false;

            MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { Parse = true; } ) );
            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

            await Popups.ShowDialog( MsgBox );

            if ( Parse )
            {
                ControlFrame.Instance.NavigateTo(
                    PageId.BOOK_SPIDER_VIEW, () => new BookSpidersView()
                    , ( View ) => ( ( BookSpidersView ) View ).Parse( Book )
                );
            }
        }

        private async void ConfirmErrorReport( string Id, StatusType ErrorType )
        {
            StringResources stx = new StringResources( "Message", "Error" );
            MessageDialog MsgBox = new MessageDialog(
                string.Format( stx.Str( "ReportError" ), stx.Str( ErrorType.ToString(), "Error" ) )
            );

            bool Report = false;

            MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { Report = true; } ) );
            MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

            await Popups.ShowDialog( MsgBox );

            if ( Report ) ReportStatus( Id, ErrorType );
        }

        public void ReportStatus( string Id, StatusType SType, string Desc = "" )
        {
            new RuntimeCache().POST(
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
            try
            {
                JsonStatus.Parse( e.ResponseString );
                new RuntimeCache().POST(
                    Shared.ShRequest.Server
                    , Shared.ShRequest.Search( "uuid: " + Id, 0, 1 )
                    // Pass the uuid instead of the query id
                    , ( re, q ) => MessageBus.Send( GetType(), AppKeys.HS_REPORT_SUCCESS, new Tuple<string, DRequestCompletedEventArgs>( Id, re ) )
                    , wenku8.System.Utils.DoNothing
                    , true
                );
            }
            catch( Exception ex )
            {
                MessageBus.Send( GetType(), AppKeys.HS_REPORT_FAILED, new Tuple<string, string>( Id, ex.Message ) );
            }
        }

    }
}