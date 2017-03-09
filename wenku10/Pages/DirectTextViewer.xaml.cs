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

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.Model.Interfaces;
using wenku8.CompositeElement;
using wenku8.Resources;

namespace wenku10.Pages
{
	public sealed partial class DirectTextViewer : Page, ICmdControls 
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		public static readonly string ID = typeof( DirectTextViewer ).Name;

		private IStorageFile CurrentFile;

		public DirectTextViewer()
		{
			this.InitializeComponent();
		}

		public DirectTextViewer( StorageFile ISF )
			:this()
		{
			CurrentFile = ISF;

			InitAppBar();
			ViewFile( ISF );
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppBar" );

			AppBarButton ExportBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.Export, stx.Text( "Export" ) );
			ExportBtn.Click += ExportBtn_Click;

			MajorControls = new ICommandBarElement[] { ExportBtn };
		}

		private async void ExportBtn_Click( object sender, RoutedEventArgs e )
		{
			IStorageFile ExFile = await AppStorage.SaveFileAsync( "Text File", new string[] { ".log" } );
			if ( ExFile == null ) return;
			await CurrentFile.CopyAndReplaceAsync( ExFile );
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

			ViewFile( e.Parameter as StorageFile );
		}

		private async void ViewFile( StorageFile file )
		{
			StorageFileStreamer SFS = new StorageFileStreamer( file );
			IList<string> FirstRead = await SFS.NextPage( 50 );

			Observables<string, string> OSF = new Observables<string, string>( FirstRead );
			OSF.ConnectLoader( SFS );

			TextContent.ItemsSource = OSF;
		}

	}
}