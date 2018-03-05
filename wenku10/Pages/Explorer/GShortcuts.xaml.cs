using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using GR.Converters;
using GR.DataSources;
using GR.Model.Interfaces;
using GR.Model.Section;

namespace wenku10.Pages.Explorer
{
	public sealed partial class GShortcuts : Page, IAnimaPage, IDisposable
	{
		private IEnumerable<GRViewSource> AvailableWidgets;

		public GShortcuts()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			MainContents.ItemTemplateSelector = new TemplateSel() { Resources = Resources };
		}

		public async void LoadWidgets()
		{
			List<WidgetView> Widgets = new List<WidgetView>();
			foreach ( GRViewSource GVS in AvailableWidgets )
			{
				WidgetView WView = new WidgetView( GVS );
				await WView.ConfigureAsync();
				Widgets.Add( WView );
			}

			MainContents.ItemsSource = Widgets;
		}

		public void RegisterWidgets( IEnumerable<GRViewSource> GVSs )
		{
			AvailableWidgets = GVSs;
		}

		public void Dispose()
		{
			MainContents.ItemsSource = null;
		}

		public async Task ExitAnima()
		{
		}

		public async Task EnterAnima()
		{
		}

		private class TemplateSel: DataTemplateSelector
		{
			public ResourceDictionary Resources { get; set; }

			protected override DataTemplate SelectTemplateCore( object Item, DependencyObject container )
			{
				if ( Item is WidgetView WItem )
				{
					return ( DataTemplate ) Resources[ WItem.TemplateName ];
				}

				return null;
			}

		}
	}
}