using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Messaging;

namespace GR.Model.Section
{
	using Data;
	using Database.Models;
	using DataSources;
	using Model.Interfaces;
	using Settings;

	sealed class WidgetView : ActiveData, INamable
	{
		public string Name
		{
			get => Conf.Name ?? ViewSource.ItemTitle;
			set
			{
				Conf.Name = value;
				NotifyChanged( "Name" );
			}
		}

		public GRViewSource RefViewSource { get; private set; }
		public GRViewSource ViewSource { get; private set; }

		public GRDataSource DataSource => ViewSource?.DataSource;
		public IGSWidget VSWidget => ( IGSWidget ) ViewSource;
		public IGRTable Table => DataSource?.Table;

		public string ConfigId { get; set; }
		public WidgetConfig Conf { get; private set; }

		public string TemplateName => Conf.Template;

		public bool SearchRequired { get; private set; }

		public WidgetView( GRViewSource ViewSource )
		{
			if ( !( ViewSource is IGSWidget ) )
				throw new ArgumentException( "ViewSource must implement IGSWidget interface" );

			RefViewSource = ViewSource;

			if ( ViewSource.DataSource.Searchable )
			{
				this.ViewSource = ViewSource.Clone();
			}
			else
			{
				this.ViewSource = ViewSource;
			}
		}

		public async Task ConfigureAsync()
		{
			if( Conf == null )
			{
				Conf = VSWidget.DefaultWidgetConfig() ?? new WidgetConfig() { Enable = false, Template = "HorzThumbnailList" };
			}

			Conf.TargetType = DataSource.ConfigId;

			try
			{
				DataSource.StructTable();
				await DataSource.ConfigureAsync();

				if ( !string.IsNullOrEmpty( Conf.Query ) )
				{
					DataSource.Search = Conf.Query;
				}
				else
				{
					DataSource.Reload();
				}
			}
			catch ( EmptySearchQueryException )
			{
				SearchRequired = true;
			}
		}

		public void OpenViewSource()
		{
			// Determine whether we need to update the reference view source's search term
			if ( RefViewSource != ViewSource && ViewSource.DataSource.Searchable && !string.IsNullOrEmpty( ViewSource.DataSource.Search ) )
			{
				RefViewSource.DataSource.Search = ViewSource.DataSource.Search;
			}

			MessageBus.SendUI( GetType(), AppKeys.OPEN_VIEWSOURCE, RefViewSource );
		}

		public Task ConfigureAsync( WidgetConfig WdConf )
		{
			Conf = WdConf;
			return ConfigureAsync();
		}

	}
}