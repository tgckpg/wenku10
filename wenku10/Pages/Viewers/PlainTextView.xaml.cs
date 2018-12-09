using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku10.Pages.Viewers
{
	public sealed partial class PlainTextView : Page
	{
		private static readonly string ID = typeof( PlainTextView ).Name;

		private Observables<string, Paragraph> CodeOSF;

		ConcurrentQueue<Paragraph> StagedTexts = new ConcurrentQueue<Paragraph>();

		public PlainTextView()
		{
			this.InitializeComponent();
		}

		public PlainTextView( object Target )
			: base()
		{
			ViewTarget( Target );
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );
			ViewTarget( e.Parameter );
		}

		private void ViewTarget( object Target )
		{
			switch ( Target )
			{
				case Tuple<IStorageFile, string> TypedFile:
					ViewFile( TypedFile.Item1 );
					break;
				case string Text:
					ViewText( new string[] { Text } );
					break;
				case IEnumerable<string> Texts:
					ViewText( Texts );
					break;
				case IEnumerable<IStorageFile> ISFs:
					ViewFile( ISFs.FirstOrDefault() );
					break;
				case IStorageFile ISF:
					ViewFile( ISF );
					break;
			}

		}

		private IList<Paragraph> S2P( IEnumerable<string> s )
			=> s.Select( S2P ).ToArray();

		private Paragraph S2P( string s )
		{
			if ( 500 < s.Length )
			{
				s = s.Substring( 0, 500 ) + $"[and {s.Length} characters] ...";
			}

			Paragraph p = new Paragraph();
			p.Inlines.Add( new Run() { Text = s } );

			StagedTexts.Enqueue( p );
			return p;
		}

		private async void ViewFile( IStorageFile file )
		{
			CodeView.Blocks.Clear();

			if ( file == null )
				return;

			StorageFileStreamer SFS = new StorageFileStreamer( file );
			SFS.PadSpace = false;

			IList<string> FirstRead = await SFS.NextPage( 200 );

			CodeOSF = new Observables<string, Paragraph>( S2P( FirstRead ) );
			CodeOSF.ConnectLoader( SFS, S2P );

			foreach ( Paragraph p in CodeOSF )
			{
				CodeView.Blocks.Add( p );
			}

			CodeOSF.CollectionChanged += CodeOSF_CollectionChanged;
			SV.ViewChanged += ScrollViewer_ViewChanged;
		}

		private void ViewText( IEnumerable<string> Texts )
		{
			CodeView.Blocks.Clear();
			S2P( Texts.Select( x => "\t" + x.Replace( "\n", "\n\t" ) ) )
				.ExecEach( ( x, i ) =>
				{
					CodeView.Blocks.Add( S2P( $"Text[{i}]:" ) );
					CodeView.Blocks.Add( x );
				} );

			CodeOSF = null;
			SV.ViewChanged -= ScrollViewer_ViewChanged;
		}

		private void CodeOSF_CollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
		{
			foreach ( Paragraph p in e.NewItems )
			{
				CodeView.Blocks.Add( p );
			}
		}

		bool LoadingMore = false;

		private async void ScrollViewer_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
		{
			if ( LoadingMore )
				return;

			double Pos = SV.VerticalOffset;
			double TriggerLen = SV.ScrollableHeight - 100;

			if ( TriggerLen < Pos && CodeOSF.HasMoreItems )
			{
				LoadingMore = true;
				await CodeOSF.LoadMoreItemsAsync( 100 );
				LoadingMore = false;
			}
		}

	}
}