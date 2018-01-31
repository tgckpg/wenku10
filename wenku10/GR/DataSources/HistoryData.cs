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
		public override ColumnConfig[] DefaultColumns => new ColumnConfig[]
		{
			new ColumnConfig() { Name = "Title", Width = 390 },
			new ColumnConfig() { Name = "Author", Width = 100 },
			new ColumnConfig() { Name = "Status", Width = 100 },
			new ColumnConfig() { Name = "LastAccess", Width = 200, Order = -1 },
		};

		protected override IQueryable<Book> QuerySet( IQueryable<Book> Context )
			=> Context.Where( x => x.LastAccess != null );

	}
}