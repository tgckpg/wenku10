using System;
using System.Linq;
using System.Collections.Generic;

using Net.Astropenguin.DataModel;

namespace wenku8.Model.Section
{
	using Book;
	using ListItem;
	using Interfaces;

	class TOCPane : ActiveData, ISearchableSection<TOCItem>
	{
		public IList<TOCItem> TOCItems { get; private set; }

		private string Terms;

		public IEnumerable<TOCItem> SearchSet
		{
			get
			{
				return Filter( TOCItems );
			}

			set
			{
				TOCItems = ( IList<TOCItem> ) value;
				NotifyChanged( "SearchSet" );
			}
		}

		public string SearchTerm
		{
			get
			{
				return Terms;
			}
			set
			{
				Terms = value;
				NotifyChanged( "SearchSet" );
			}
		}

		public TOCItem CurrentIndex;

		public TOCPane( Volume[] Vols, Chapter CurrentCh = null )
		{
			List<TOCItem> Items = new List<TOCItem>();
			foreach( Volume V in Vols )
			{
				Items.Add( new TOCItem( V ) );
				foreach( Chapter C in V.ChapterList )
				{
					TOCItem Item = new TOCItem( C );
					Items.Add( Item );

					if( C.Equals( CurrentCh ) )
					{
						CurrentIndex = Item;
					}
				}			   
			}

			SearchSet = Items;
		}

		public TOCItem GetItem( Chapter C )
		{
			foreach( TOCItem Item in TOCItems )
			{
				if ( Item.IsItem( C ) )
					return Item;
			}
			return null;
		}

		private IEnumerable<TOCItem> Filter( IEnumerable<TOCItem> Items )
		{
			if ( string.IsNullOrEmpty( SearchTerm ) ) return Items;

			return Items.Where( ( TOCItem e ) =>
			 {
				 return e.TreeLevel == 0 || e.ItemTitle.IndexOf( SearchTerm, StringComparison.CurrentCultureIgnoreCase ) != -1;
			 } );
		}
	}
}
