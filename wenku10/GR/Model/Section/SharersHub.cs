using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

namespace GR.Section
{
	using AdvDM;
	using Model.ListItem;
	using Model.ListItem.Sharers;
	using Model.Loaders;
	using Model.REST;
	using Settings;
	using GSystem;

	sealed class SharersHub : ActiveData
	{
		private readonly string ID = typeof( SharersHub ).Name;

		RuntimeCache RCache;

		public Observables<HubScriptItem, HubScriptItem> SearchSet { get; private set; }

		private int _Loading = 0;
		public bool Loading
		{
			get { return 0 < _Loading; }
			private set
			{
				_Loading += value ? 1 : -1;

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

		public SharersHub()
		{
			SearchSet = new Observables<HubScriptItem, HubScriptItem>();

			RCache = new RuntimeCache();

			MessageBus.OnDelivery += MessageBus_OnDelivery;

			SearchSet.LoadStart += ( s, e ) => { Searching = true; };
			SearchSet.LoadEnd += ( s, e ) => { Searching = false; };
		}

		~SharersHub()
		{
			MessageBus.OnDelivery -= MessageBus_OnDelivery;
		}

		public async void Search( string Query, IEnumerable<string> AccessTokens = null )
		{
			if ( AccessTokens == null )
				AccessTokens = new TokenManager().AuthList.Remap( x => ( string ) x.Value );

			Searching = true;
			SHSearchLoader SHLoader = new SHSearchLoader( Query, AccessTokens );

			SearchSet.Clear();
			SearchSet.ConnectLoader( SHLoader );
			await SearchSet.LoadMoreItemsAsync( 0 );

			Searching = false;
		}

		private void SearchItemUpdate( DRequestCompletedEventArgs e, string Id )
		{
			HubScriptItem Target;
			if ( TryGetId( Id, out Target ) )
			{
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
		}

		private bool TryGetId( string Id, out HubScriptItem Target )
		{
			Target = SearchSet.Cast<HubScriptItem>().FirstOrDefault( ( HubScriptItem HSI ) => HSI.Id == Id );

			if ( Target == null )
			{
				Logger.Log( ID, "Target is gone after Status Report returned", LogType.WARNING );
				return false;
			}

			return true;
		}

		private void MessageBus_OnDelivery( Message Mesg )
		{
			switch ( Mesg.Content )
			{
				case AppKeys.SP_PROCESS_COMP:

					LocalBook SBook = ( LocalBook ) Mesg.Payload;
					HubScriptItem HSC = SearchSet.Cast<HubScriptItem>().FirstOrDefault( x => x.Id == SBook.ZItemId );

					if ( HSC == null )
					{
						Logger.Log( ID, "Book is not from Sharer's hub", LogType.DEBUG );
					}
					else
					{
						HSC.InCollection = SBook.ProcessSuccess;
					}

					break;

				case AppKeys.HS_REPORT_SUCCESS:

					Tuple<string, DRequestCompletedEventArgs> Payload = ( Tuple<string, DRequestCompletedEventArgs> ) Mesg.Payload;
					SearchItemUpdate( Payload.Item2, Payload.Item1 );

					break;

				case AppKeys.HS_REPORT_FAILED:

					Tuple<string, string> ErrorPayload = ( Tuple<string, string> ) Mesg.Payload;

					HubScriptItem HSI;
					if ( TryGetId( ErrorPayload.Item1, out HSI ) )
					{
						HSI.ErrorMessage = ErrorPayload.Item2;
					}

					break;
			}
		}
	}
}