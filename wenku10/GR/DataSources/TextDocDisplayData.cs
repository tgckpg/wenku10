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

namespace GR.DataSources
{
	using Data;
	using Database.Models;
	using Model.Interfaces;
	using Model.ListItem;
	using Resources;

	sealed class TextDocDisplayData : GRDataSource
	{
		private readonly string ID = typeof( TextDocDisplayData ).Name;

		protected override string ConfigId => "TextDoc";

		private GRTable<IBookProcess> PsTable;
		public override IGRTable Table => PsTable;

		private ObservableCollection<GRRow<IBookProcess>> _Items = new ObservableCollection<GRRow<IBookProcess>>();

		protected override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Name", Width = 335 },
			new ColumnConfig() { Name = "Desc", Width = 355 },
			new ColumnConfig() { Name = "Desc2", Width = 200 },
		};

		public override string ColumnName( IGRCell CellProp ) => GStrings.ColumnNameResolver.IBookProcess( CellProp.Property.Name );

		public override void Reload()
		{
			lock ( this )
			{
				if ( IsLoading ) return;
				IsLoading = true;
			}

			StringResources stx = new StringResBg( "LoadingMessage" );
			Message = stx.Str( "ProgressIndicator_Message" );

			PsTable.Items = _Items;
			// _Items.Clear();

			Shared.BooksDb.QueryBook( x => x.Type == BookType.L )
				.ExecEach( x => ImportItem( new LocalBook( x ) ) );

			IsLoading = false;
		}
		
		public async void OpenDirectory()
		{
			IsLoading = true;

			await Shared.Storage.GetLocalText( async ( x, i, l ) =>
			{
				if ( i % 20 == 0 )
				{
					await Task.Delay( 15 );
				}

				Message = string.Format( "{0}/{1}", i, l );
				ImportItem( new LocalBook( x ) );
			} );

			IsLoading = false;
		}

		public void ImportItem( LocalBook Item )
		{
			GRRow<IBookProcess> Existing = _Items.FirstOrDefault( x =>
			{
				LocalBook b = ( LocalBook ) x.Source;
				return b.ZoneId == Item.ZoneId && b.ZItemId == Item.ZItemId;
			} );

			if ( Existing == null )
			{
				GRRow<IBookProcess> Row = new GRRow<IBookProcess>( PsTable ) { Source = Item };
				Worker.UIInvoke( () => _Items.Add( Row ) );
			}
			else
			{
				LocalBook Bk = ( LocalBook ) Existing.Source;

				if ( Item.File != null )
				{
					Bk.SetSource( Item.File );
				}
			}
		}

		public void Delete( GRRow<IBookProcess> Row )
		{
			LocalBook Bk = ( LocalBook ) Row.Source;
			Bk.RemoveSource();

			if( !Bk.CanProcess )
			{
				Worker.UIInvoke( () => _Items.Remove( Row ) );
			}
		}

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