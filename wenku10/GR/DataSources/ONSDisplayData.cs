using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using GSystem;
	using Model.Loaders;
	using Model.ListItem.Sharers;
	using Settings;

	sealed class ONSDisplayData : GRDataSource
	{
		public override IGRTable Table => HSTable;
		public override string SearchExample => "zone: <Zone> type: <Type> tags: <Tag>";

		protected override string ConfigId => "ONS";
		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Name", Width = 200 },
			new ColumnConfig() { Name = "Description", Width = 265 },
			new ColumnConfig() { Name = "Author", Width = 100 },
			new ColumnConfig() { Name = "Status", Width = 100 },
		};

		private GRTable<HSDisplay> HSTable;

		public override string ColumnName( IGRCell CellProp ) => HSDisplay.PropertyName( CellProp.Property );

		public ONSDisplayData()
			: base()
		{
			MessageBus.Subscribe( this, MessageBus_OnDelivery );
		}

		private void MessageBus_OnDelivery( Message Mesg )
		{
			if ( Mesg.Content == AppKeys.SH_SCRIPT_REMOVE && Mesg.Payload is HubScriptItem HSI )
			{
				if ( HSTable.Items is ObservableCollection<GRRow<HSDisplay>> Collection )
				{
					GRRow<HSDisplay> Row = Collection.FirstOrDefault( x => x.Source.Item == HSI );
					if ( Row != null )
					{
						Collection.Remove( Row );
					}
				}
			}
		}

		public override void Reload()
		{
			IsLoading = true;

			StringResources stx = new StringResBg( "LoadingMessage" );
			Message = stx.Str( "ProgressIndicator_Message" );

			IEnumerable<string> AccessTokens = new TokenManager().AuthList.Remap( x => ( string ) x.Value );

			SHSearchLoader SHLoader = new SHSearchLoader( Search, AccessTokens );

			Observables<HubScriptItem, GRRow<HSDisplay>> OHS = new Observables<HubScriptItem, GRRow<HSDisplay>>();
			OHS.ConnectLoader( SHLoader, x => x.Remap( ToGRRow ) );

			OHS.LoadEnd += ( s, e ) => IsLoading = false;
			OHS.LoadStart += ( s, e ) => IsLoading = true;

			HSTable.Items = OHS;
			IsLoading = false;
		}

		private GRRow<HSDisplay> ToGRRow( HubScriptItem HSItem )
		{
			return new GRRow<HSDisplay>( HSTable ) { Source = new HSDisplay( HSItem ) };
		}

		public override void StructTable()
		{
			if ( HSTable != null )
				return;

			List<IGRCell> HSProps = new List<IGRCell>();

			Type StringType = typeof( string );

			HSProps.AddRange(
				typeof( HSDisplay ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<HSDisplay>( p ) )
			);

			HSTable = new GRTable<HSDisplay>( HSProps );
			HSTable.Cell = ( i, x ) => HSTable.ColEnabled( i ) ? ColumnName( HSTable.CellProps[ i ] ) : "";
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }

		protected override void ConfigureSort( string Name, int Order ) { /* Not Supported */ }
	}

}