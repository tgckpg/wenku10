using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using GStrings;
	using Model.Interfaces;
	using Model.ListItem;
	using Resources;
	using Settings;

	sealed class BookSpiderDisplayData : GRDataSource, IHelpContext
	{
		private readonly string ID = typeof( BookSpiderDisplayData ).Name;

		public override string ConfigId => "NPSpider";

		public bool Help_Show => !_Items.Any();
		public string Help_Title { get; private set; }
		public string Help_Desc { get; private set; }
		public Uri Help_Uri { get; private set; }

		public override bool IsLoading
		{
			get => base.IsLoading;
			protected set
			{
				base.IsLoading = value;
				NotifyChanged( "Help_Show" );
			}
		}

		private GRTable<IBookProcess> PsTable;
		public override IGRTable Table => PsTable;

		private ObservableCollection<GRRow<IBookProcess>> _Items = new ObservableCollection<GRRow<IBookProcess>>();

		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Name", Width = 335 },
			new ColumnConfig() { Name = "Zone", Width = 100 },
			new ColumnConfig() { Name = "Desc", Width = 355 },
		};

		public BookSpiderDisplayData()
		{
			StringResources stx = StringResources.Load( "AppResources" );
			Help_Title = stx.Text( "BookSpider" );
			Help_Desc = stx.Text( "Desc_BookSpider" );
			Help_Uri = new Uri( "https://github.com/tgckpg/wenku10-samples" );
		}

		public override string ColumnName( IGRCell CellProp ) => ColumnNameResolver.IBookProcess( CellProp.Property.Name );

		public override async void Reload()
		{
			lock ( this )
			{
				if ( IsLoading ) return;
				IsLoading = true;
			}

			StringResources stx = StringResources.Load( "LoadingMessage" );
			string LoadText = stx.Str( "ProgressIndicator_Message" );

			PsTable.Items = _Items;

			// Store existing items
			GRRow<IBookProcess>[] Items = _Items.ToArray();
			Worker.UIInvoke( () => _Items.Clear() );

			foreach ( var Bk in Shared.BooksDb.Books.Where( x => x.Script != null ).Select( x => new { x.ZoneId, x.ZItemId } ) )
			{
				string ZoneId = Bk.ZoneId;
				string ZItemId = Bk.ZItemId;

				Message = LoadText + ": " + ZoneId;
				if ( FindRow( Items, ZoneId, ZItemId, out GRRow<IBookProcess> Row ) )
				{
					await AddRowAsync( Row );
				}
				else
				{
					SpiderBook LB = await SpiderBook.CreateSAsync( ZoneId, ZItemId, null );
					if ( LB.ProcessSuccess || LB.CanProcess )
					{
						ZoneNameResolver.Instance.Resolve( LB.ZoneId, x => LB.Zone = x );
						await AddRowAsync( new GRRow<IBookProcess>( PsTable ) { Source = LB } );
					}
				}
			}

			IsLoading = false;
		}

		public void ImportItem( SpiderBook Item )
		{
			IsLoading = true;
			if ( FindRow( _Items, Item.ZoneId, Item.ZItemId, out GRRow<IBookProcess> Existing ) )
			{
				// TODO: Ask to replace
			}
			else
			{
				AddRowBg( new GRRow<IBookProcess>( PsTable ) { Source = Item } );
			}
			IsLoading = false;
		}

		public void Delete( GRRow<IBookProcess> Row )
		{
			SpiderBook SBk = ( SpiderBook ) Row.Source;
			Shared.Storage.DeleteFile( SBk.MetaLocation );
			Shared.BooksDb.Delete( BookType.S, SBk.ZoneId, SBk.ZItemId );
			Worker.UIInvoke( () => _Items.Remove( Row ) );
		}

		public IGRRow FindRow( string ZoneId, string ZItemId )
		{
			FindRow( _Items, ZoneId, ZItemId, out GRRow<IBookProcess> Item );
			return Item;
		}

		private bool FindRow( IEnumerable<GRRow<IBookProcess>> Source, string ZoneId, string ZItemId, out GRRow<IBookProcess> Item )
		{
			foreach ( GRRow<IBookProcess> P in Source )
			{
				SpiderBook x = ( SpiderBook ) P.Source;

				if ( x.ZoneId == ZoneId && x.ZItemId == ZItemId )
				{
					Item = P;
					return true;
				}
			}

			Item = null;
			return false;
		}

		private void AddRowBg( GRRow<IBookProcess> Row ) => Worker.UIInvoke( () => _Items.Add( Row ) );
		private Task AddRowAsync( GRRow<IBookProcess> Row ) => Worker.RunUIAsync( () => _Items.Add( Row ) );

		public override void StructTable()
		{
			if ( PsTable != null )
				return;

			List<IGRCell> PsProps = new List<IGRCell>();

			Type StringType = typeof( string );

			PsProps.AddRange(
				typeof( IBookProcess ).GetProperties()
					.Where( x => x.PropertyType == StringType )
					.Remap( p => new GRCell<IBookProcess>( p ) )
			);

			PsTable = new GRTable<IBookProcess>( PsProps );
			PsTable.Cell = ( i, x ) => PsTable.ColEnabled( i ) ? ColumnName( PsTable.CellProps[ i ] ) : "";
		}

		public override void Sort( int ColIndex, int Order ) { /* Not Supported */ }
		public override void ToggleSort( int ColIndex ) { /* Not Supported */ }
		protected override void ConfigureSort( string PropertyName, int Order ) { /* Not Supported */ }
	}
}