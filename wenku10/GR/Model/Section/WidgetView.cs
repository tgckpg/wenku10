using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.Model.Section
{
	using Data;
	using DataSources;

	sealed class WidgetView
	{
		public string Name { get; set; }

		private GRViewSource ViewSource;
		public GRDataSource DataSource => ViewSource?.DataSource;
		public IGRTable Table => DataSource?.Table;

		public string TemplateName { get; private set; }

		public WidgetView( GRViewSource ViewSource )
		{
			this.ViewSource = ViewSource;
			Name = ViewSource.ItemTitle;

			TemplateName = "HorzThumbnailList";

			if ( ViewSource.DataSourceType == typeof( HistoryData ) )
			{
				TemplateName = "Banner";
			}
		}

		public async Task ConfigureAsync()
		{
			try
			{
				DataSource.StructTable();
				await DataSource.ConfigureAsync();
				DataSource.Reload();
			}
			catch ( EmptySearchQueryException )
			{
			}
		}
	}
}