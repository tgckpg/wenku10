using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using GR.AdvDM;
using GR.Ext;
using GR.CompositeElement;
using GR.DataSources;
using GR.Model.Book;
using GR.Model.Book.Spider;
using GR.Model.ListItem;
using GR.Model.ListItem.Sharers;
using GR.Model.Loaders;
using GR.Model.Pages;
using GR.Model.REST;
using GR.Model.Section;
using GR.Storage;
using GR.Resources;
using GR.Settings;

using StatusType = GR.Model.REST.SharersRequest.StatusType;
using SHTarget = GR.Model.REST.SharersRequest.SHTarget;

namespace wenku10.SHHub
{
	using Pages;
	using Pages.Dialogs;
	using Pages.Sharers;

	sealed class MesgListerner
	{
		private bool PinErrored = false;

		public MesgListerner()
		{
			MessageBus.Subscribe( this, MessageBus_OnDelivery );
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
							PageId.MASTER_EXPLORER, () => new MasterExplorer()
							//  Open ZoneSpider Manager
							, P => ( ( MasterExplorer ) P ).NavigateToDataSource(
								typeof( ZSMDisplayData )
								, async ( ZSVS ) =>
								{
									// Using the manager, import this script
									ZSMDisplayData DisplayData = ( ( ZSManagerVS ) ZSVS ).ZSMData;
									if ( await DisplayData.OpenFile( HSI.ScriptFile ) )
									{
										// Open the imported script
										ZoneSpider ZSpider = DisplayData.ZSTable.Items.Select( x => ( ZoneSpider ) x.Source ).FirstOrDefault( x => x.ZoneId == HSI.Id );
										( ( MasterExplorer ) P ).NavigateToZone( ZSpider );
									}
								}
							) );
						break;
					}

					// This will save the book
					SpiderBook SBook = await SpiderBook.ImportFile( HSI.ScriptFile, true );

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
					StringResources stx = StringResources.Load( "Message", "ContextMenu" );

					bool Place = false;

					await Popups.ShowDialog( UIAliases.CreateDialog(
						stx.Str( "Desc_DecryptionFailed" ), stx.Str( "DecryptionFailed" )
						, () => Place = true
						, stx.Text( "PlaceRequest", "ContextMenu" ), stx.Str( "OK" )
					) );

					if ( Place )
					{
						HSI = ( HubScriptItem ) Mesg.Payload;
						TransferRequest( SHTarget.KEY, HSI );
					}

					break;

				case AppKeys.HS_OPEN_COMMENT:
					InboxMessage BoxMessage = ( InboxMessage ) Mesg.Payload;
					ControlFrame.Instance.NavigateTo( PageId.SCRIPT_DETAILS
						, () => new ScriptDetails( BoxMessage.HubScript )
						, P => ( ( ScriptDetails ) P ).OpenCommentStack( BoxMessage.CommId )
					);
					break;

				case AppKeys.HS_NO_VOLDATA:
					ConfirmErrorReport( ( ( BookInstruction ) Mesg.Payload ).ZItemId, StatusType.HS_NO_VOLDATA );
					break;

				case AppKeys.EX_DEATHBLOW:
					stx = StringResources.Load( "Message", "ContextMenu" );

					bool UseDeathblow = false;

					await Popups.ShowDialog( UIAliases.CreateDialog(
						stx.Str( "ConfirmDeathblow" )
						, () => UseDeathblow = true
						, stx.Str( "Yes" ), stx.Str( "No" )
					) );

					if( UseDeathblow )
					{
						IDeathblow Deathblow = ( IDeathblow ) Mesg.Payload;
						ControlFrame.Instance.NavigateTo( PageId.W_DEATHBLOW, () => new WDeathblow( Deathblow ), P => ( ( WDeathblow ) P ).Blow() );
					}

					break;

				case AppKeys.PM_CHECK_TILES:
					CheckTiles();
					break;

				case AppKeys.OPEN_VIEWSOURCE:
					ControlFrame.Instance.NavigateTo( PageId.MASTER_EXPLORER, () => new MasterExplorer(), P => ( ( MasterExplorer ) P ).NavigateToViewSource( ( GRViewSource ) Mesg.Payload ) );
					break;

				case AppKeys.OPEN_ZONE:
					ControlFrame.Instance.NavigateTo( PageId.MASTER_EXPLORER, () => new MasterExplorer(), P => ( ( MasterExplorer ) P ).NavigateToZone( ( ZoneSpider ) Mesg.Payload ) );
					break;

				case AppKeys.PROMPT_LOGIN:
					(IMember Member, Action DialogClosed) = ( Tuple<IMember, Action> ) Mesg.Payload;
					if ( !Member.IsLoggedIn )
					{
						Login LoginDialog = new Login( Member );
						await Popups.ShowDialog( LoginDialog );
					}
					DialogClosed();
					break;
			}
		}

		private async void CheckTiles()
		{
			PinErrored = false;
			PinManager PM = new PinManager();

			await PM.SyncSettings();
			if ( PM.Policy == PinPolicy.DO_NOTHING ) return;

			ActiveItem[] MissingPins = PM.GetLocalPins().Where(
				x => !Windows.UI.StartScreen.SecondaryTile.Exists( x.Payload )
			).ToArray();

			if ( 0 < MissingPins.Length )
			{
				switch ( PM.Policy )
				{
					case PinPolicy.ASK:
						bool RemoveRecord = true;
						StringResources stx = StringResources.Load( "Message", "AppBar", "ContextMenu" );
						await Popups.ShowDialog( UIAliases.CreateDialog(
							string.Format( stx.Str( "MissingPins" ), MissingPins.Length )
							, () => RemoveRecord = false
							, stx.Text( "PinToStart", "ContextMenu" ), stx.Text( "PinPolicy_RemoveMissing", "AppBar" )
						) );

						if ( RemoveRecord ) goto case PinPolicy.REMOVE_MISSING;
						goto case PinPolicy.PIN_MISSING;

					case PinPolicy.PIN_MISSING:
						foreach ( ActiveItem Item in MissingPins )
						{
							BookItem Book = await ItemProcessor.GetBookFromId( Item.Desc );
							if ( Book == null )
							{
								PinError();
							}
							else
							{
								TaskCompletionSource<string> TileId = new TaskCompletionSource<string>();

								BookLoader Loader = new BookLoader( async ( b ) =>
								{
									TileId.SetResult( b == null ? null : await PageProcessor.PinToStart( b ) );
								} );

								Loader.Load( Book, true );
								await TileId.Task;
							}
						}
						break;

					case PinPolicy.REMOVE_MISSING:
						PM.RemovePin( MissingPins.Remap( x => x.Desc ) );
						break;
				}
			}
		}

		private void PinError()
		{
			if ( !PinErrored )
			{
				PinErrored = true;
				GR.GSystem.ActionCenter.Instance.ShowError( "PM_SourceMissing" );
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
			StringResources stx = StringResources.Load( "Message" );
			MessageDialog MsgBox = new MessageDialog( stx.Str( "ConfirmScriptParse" ) );

			bool Parse = false;

			MsgBox.Commands.Add( new UICommand( stx.Str( "Yes" ), x => { Parse = true; } ) );
			MsgBox.Commands.Add( new UICommand( stx.Str( "No" ) ) );

			await Popups.ShowDialog( MsgBox );

			if ( Parse )
			{
				ControlFrame.Instance.NavigateTo(
					PageId.MASTER_EXPLORER, () => new MasterExplorer()
					, P => ( ( MasterExplorer ) P ).NavigateToDataSource(
						typeof( BookSpiderDisplayData )
						, VS => ( ( BookSpiderVS ) VS ).Process( Book.ZoneId, Book.ZItemId )
					)
				);
			}
		}

		private async void ConfirmErrorReport( string Id, StatusType ErrorType )
		{
			StringResources stx = StringResources.Load( "Message", "Error" );
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
					, GR.GSystem.Utils.DoNothing
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