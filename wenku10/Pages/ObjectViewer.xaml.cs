using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using GFlow.Models.Procedure;

using GR.CompositeElement;
using GR.GFlow;
using GR.Model.Book.Spider;
using GR.Model.Interfaces;
using GR.Resources;

namespace wenku10.Pages
{
	public sealed partial class ObjectViewer : Page, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		public static readonly string ID = typeof( ObjectViewer ).Name;

		private object Target;

		public ObjectViewer()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public ObjectViewer( object Target )
			: this()
		{
			this.Target = Target;
			InspectObject();
		}

		private void SetTemplate()
		{
			StringResources stx = StringResources.Load( "AppBar" );

			AppBarButton ExportBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.Export, stx.Text( "Export" ) );
			ExportBtn.Click += ExportBtn_Click;

			MajorControls = new ICommandBarElement[] { ExportBtn };
		}

		private async void ExportBtn_Click( object sender, RoutedEventArgs e )
		{
			IStorageFile ExFile = await AppStorage.SaveFileAsync( "Text File", new string[] { ".log" } );
			if ( ExFile == null ) return;
			// await CurrentFile.CopyAndReplaceAsync( ExFile );
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

			Target = e.Parameter;
			InspectObject();
		}

		private void InspectObject()
		{
			if ( Target is Tuple<IStorageFile, string>
				|| Target is string
				|| Target is IEnumerable<string>
				|| Target is IEnumerable<IStorageFile>
				|| Target is IStorageFile ISF )
			{
				ObjectViewFrame.Navigate( typeof( CCSourceView ), Target );
				return;
			}

			if ( !( Target is ProcConvoy Convoy ) )
				return;

			switch ( Convoy.Payload )
			{
				case BookInstruction Bk:
					if ( Convoy.Dispatcher is GrimoireMarker )
					{
					}
					else
					{
						Target = Bk.PlainTextInfo;
						InspectObject();
					}
					break;
				case IEnumerable<BookInstruction> Bks:
					Target = Bks.Select( x => x.PlainTextInfo );
					InspectObject();
					break;
				default:
					Target = Convoy.Payload;
					InspectObject();
					break;
			}
		}

	}
}