using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Loaders;

using GR.DataSources;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.PageExtensions;
using GR.Resources;

namespace wenku10.Pages.Settings.Advanced
{
	public sealed partial class TRTableEditor : Page, INavPage, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => false;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		public TRTableEditor()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private List<NameValue<string>> Tables;
		private Dictionary<string, ConvViewSource> ViewSources;

		public void SoftClose( bool NavForward )
		{
			LayoutRoot.Children.Remove( TableView );
		}

		public void SoftOpen( bool NavForward )
		{
		}

		private void SetTemplate()
		{
			Tables = new List<NameValue<string>>();
			ViewSources = new Dictionary<string, ConvViewSource>();

			StringResources stx = StringResources.Load( "Settings" );

			if ( Shared.Conv.DoTraditional )
			{
				Tables.Add( new NameValue<string>( stx.Text( "Conv_NTW_ws2t" ), "ntw_ws2t" ) );
				Tables.Add( new NameValue<string>( stx.Text( "Conv_NTW_ps2t" ), "ntw_ps2t" ) );
			}

			if( Shared.Conv.DoSyntaxPatch )
			{
				Tables.Add( new NameValue<string>( stx.Text( "Conv_Custom" ), "synpatch" ) );
			}

			Tables.Add( new NameValue<string>( stx.Text( "Conv_Vertical" ), "vertical" ) );

			TableTypes.ItemsSource = Tables;

			if ( Tables.Any() )
			{
				TableTypes.SelectedIndex = 0;
				SwitchVS( Tables.First().Value );
			}
		}

		private ConvPageExt PageExt;

		private async void SwitchVS( string Name )
		{
			if ( !ViewSources.TryGetValue( Name, out ConvViewSource VS ) )
			{
				VS = new ConvViewSource( Name );
				ViewSources[ Name ] = VS;
			}

			if ( PageExt != null )
			{
				MajorControls = new ICommandBarElement[ 0 ];
				MinorControls = new ICommandBarElement[ 0 ];

				PageExt.ControlChanged -= PageExt_ControlChanged;
				PageExt.Unload();
				PageExt = null;
			}

			await TableView.View( VS );

			PageExt = ( ConvPageExt ) VS.Extension;

			PageExt.Initialize( this );
			MajorControls = PageExt.MajorControls;
			MinorControls = PageExt.MinorControls;

			PageExt.ControlChanged += PageExt_ControlChanged;
			PageExt_ControlChanged( PageExt );
		}

		private void PageExt_ControlChanged( object sender )
		{
			ControlChanged?.Invoke( this );
		}

		private void TableTypes_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			SwitchVS( ( ( NameValue<string> ) e.AddedItems.First() ).Value );
		}

	}
}