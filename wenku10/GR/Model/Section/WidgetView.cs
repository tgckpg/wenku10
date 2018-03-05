using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.Model.Section
{
	using Data;
	using Database.Models;
	using DataSources;
	using Model.Interfaces;

	sealed class WidgetView
	{
		public string Name => Conf.Name ?? ViewSource.ItemTitle;

		public GRViewSource ViewSource { get; private set; }

		public GRDataSource DataSource => ViewSource?.DataSource;
		public IGSWidget VSWidget => ( IGSWidget ) ViewSource;
		public IGRTable Table => DataSource?.Table;

		public string ConfigId { get; set; }
		public WidgetConfig Conf { get; private set; }

		public string TemplateName => Conf.Template;

		public WidgetView( GRViewSource ViewSource )
		{
			if ( !( ViewSource is IGSWidget ) )
				throw new ArgumentException( "ViewSource must implement IGSWidget interface" );

			this.ViewSource = ViewSource;
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

			Conf = VSWidget.DefaultWidgetConfig();

			if( Conf == null )
			{
				Conf = new WidgetConfig() { Enable = false, Template = "HorzThumbnailList" };
			}
		}

	}
}