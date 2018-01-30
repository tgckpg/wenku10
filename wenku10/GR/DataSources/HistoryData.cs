using GR.Data;
using GR.Database.Models;
using GR.Model.Book;
using GR.Resources;
using Net.Astropenguin.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.DataSources
{
	sealed class HistoryData : BookDisplayData
	{
		public override string Name => "Hisotry";

		protected override IQueryable<Book> QuerySet( IQueryable<Book> Context )
			=> Context.Where( x => x.LastAccess != null );

		public override void ItemAction( IGRRow Row )
		{
		}
	}
}