using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Storage;

using Net.Astropenguin.Loaders;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Data;
	using DataSources;
	using Model.Book;
	using Model.Interfaces;
	using Resources;

	sealed class FTSDataPageExt : PageExtension, ICmdControls
	{
		public readonly string ID = typeof( TextDocPageExt ).Name;

#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => false;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private FTSViewSource ViewSource;

		private MenuFlyout ContextMenu;

		AppBarButton Rebuild;

		public FTSDataPageExt( FTSViewSource ViewSource )
			: base()
		{
			this.ViewSource = ViewSource;
		}

		public override void Unload()
		{
		}

		protected override void SetTemplate()
		{
			InitAppBar();

			StringResources stx = new StringResources( "ContextMenu" );
			ContextMenu = new MenuFlyout();
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppBar" );

			Rebuild = UIAliases.CreateAppBarBtn( SegoeMDL2.ResetDrive, stx.Text( "RebuildIndex" ) );
			Rebuild.Click += Rebuild_Click;

			MajorControls = new ICommandBarElement[] { Rebuild };
		}

		private async void Rebuild_Click( object sender, RoutedEventArgs e )
		{
			Rebuild.IsEnabled = false;
			await ViewSource.FTSData.Rebuild();
			Rebuild.IsEnabled = true;
		}

		public override FlyoutBase GetContextMenu( FrameworkElement elem )
		{
			if ( elem.DataContext is GRRow<FTSResult> Row )
			{
				return ContextMenu;
			}
			return null;
		}
	}
}