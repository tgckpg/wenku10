using System;
using System.Linq;
using System.Collections.Generic;

using Net.Astropenguin.DataModel;

namespace GR.Model.Section
{
	using Database.Models;
	using ListItem;

	sealed class TOCPane : ActiveData
	{
		private List<TOCItem> Volumes;
		private List<TOCItem> Chapters;

		public TreeList SearchSet { get; private set; }

		public string SearchTerm;

		public TOCPane( Volume[] Vols )
		{
			Volumes = new List<TOCItem>();
			Chapters = new List<TOCItem>();

			foreach( Volume V in Vols )
			{
				IEnumerable<TOCItem> Chs = V.Chapters.Select( C => new TOCItem( C ) ).ToArray();
				Chapters.AddRange( Chs );

				TOCItem VItem = new TOCItem( V ) { Children = Chs.ToList() };
				Volumes.Add( VItem );
			}

			SearchSet = new TreeList( Volumes.ToArray() );
		}

		public TOCItem OpenChapter( Chapter C )
		{
			foreach( TOCItem Item in Chapters )
			{
				if ( Item.Ch == C )
				{
					SearchSet.Open( Item );
					return Item;
				}
			}
			return null;
		}

	}
}
