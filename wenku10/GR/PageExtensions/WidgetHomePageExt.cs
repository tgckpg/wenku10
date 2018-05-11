using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku10.Pages.Dialogs;
using wenku10.Pages.Explorer;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Model.Section;

	sealed class WidgetsHomePageExt : HighlightsHomePageExt
	{
		AppBarButton AddWidgetBtn;
		GShortcuts GRShortcuts;

		public WidgetsHomePageExt( GShortcuts GSH )
		{
			GRShortcuts = GSH;
		}

		protected override void SetTemplate()
		{
			base.SetTemplate();
			InitAppBar();
		}

		private void InitAppBar()
		{
			StringResources stx = StringResources.Load( "AppBar" );
			AddWidgetBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Text( "AddWidget" ) );
			AddWidgetBtn.Click += AddWidgetBtn_Click;

			MajorControls = MajorControls.Concat( new ICommandBarElement[] { AddWidgetBtn } ).ToArray();
		}

		private async void AddWidgetBtn_Click( object sender, RoutedEventArgs e )
		{
			AddWidget AddWidgetDialog = new AddWidget( GRShortcuts.AvailableWidgets );
			await Popups.ShowDialog( AddWidgetDialog );

			if( AddWidgetDialog.SelectedWidget != null )
			{
				GRShortcuts.AddWidget( AddWidgetDialog.SelectedWidget );
			}
		}

	}
}