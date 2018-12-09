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

using Net.Astropenguin.Logging;

using GR.Database.Models;
using GR.Model.ListItem;
using GR.Model.Section;

namespace wenku10.Pages.ContentReaderPane
{
	sealed partial class TableOfContents : Page
	{
		public static readonly string ID = typeof( TableOfContents ).Name;

		private ContentReaderBase Reader;

		private TOCPane TOC;
		private Action<Chapter> OpenChapter;

		public TableOfContents()
		{
			InitializeComponent();
		}

		public TableOfContents( ContentReaderBase MainReader )
			: this()
		{
			Reader = MainReader;

			if ( Reader.CurrentBook == null )
			{
				Logger.Log( ID, "Cannot init TOC: CurrentBook is null... is pages unloaded ?", LogType.WARNING );
				return;
			}

			SetTOC( Reader.CurrentBook.GetVolumes(), x => Reader.OpenBook( x ) );
			UpdateDisplay();
		}

		public void UpdateDisplay()
		{
			TOCList.SelectedItem = TOC.OpenChapter( Reader.CurrentChapter );
		}

		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			base.OnNavigatedTo( e );
			Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

			if ( e.Parameter is Tuple<Volume[], Action<Chapter>> Args )
			{
				SetTOC( Args.Item1, Args.Item2 );
			}
		}

		private void SetTOC( Volume[] Vols, Action<Chapter> OpenCh )
		{
			TOC = new TOCPane( Vols );
			TOCContext.DataContext = TOC;
			OpenChapter = OpenCh;
		}

		private void TOCListLoaded( object sender, RoutedEventArgs e )
		{
			TOCList.ScrollIntoView( TOCList.SelectedItem );
		}

		private void SearchSet_ItemClick( object sender, ItemClickEventArgs e )
		{
			if ( e.ClickedItem is TOCItem Item )
			{
				if ( Item.Ch == null )
				{
					TOC.SearchSet.Toggle( Item );
				}
				else
				{
					OpenChapter?.Invoke( Item.Ch );
				}
			}
		}

		private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
		{
			TOC.SearchSet.Filter( sender.Text.Trim() );
		}
	}
}
