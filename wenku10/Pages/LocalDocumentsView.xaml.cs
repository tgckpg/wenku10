using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

using Net.Astropenguin.Loaders;
using Net.Astropenguin.Helpers;

using GR.CompositeElement;
using GR.Effects;
using GR.Model.Book;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.Pages;
using GR.Model.Section;
using GR.Resources;

using BInfConfig = GR.Settings.Layout.BookInfoView;

namespace wenku10.Pages
{
	public sealed partial class LocalDocumentsView : Page, ICmdControls, IAnimaPage
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private DocumentList FileListContext;
		private LocalBook SelectedBook;

		private AppBarButton ProcessBtn;

		public LocalDocumentsView()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 0, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30, 350, 0, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		private void SetTemplate()
		{
			InitAppBar();
			FileListContext = new DocumentList();

			LayoutRoot.RenderTransform = new TranslateTransform();
			LayoutRoot.DataContext = FileListContext;

			FileListContext.PropertyChanged += FileListContext_PropertyChanged;
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppBar" );

			SecondaryIconButton OpenFolder = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.OpenLocal, stx.Text( "OpenFolder" ) );
			OpenFolder.Click += ( s, e ) => FileListContext.OpenDirectory();

			SecondaryIconButton OpenUrl = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Link, stx.Text( "OpenUrl" ) );
			OpenUrl.Click += OpenUrl_Click;

			ProcessBtn = UIAliases.CreateAppBarBtn( Symbol.Play, stx.Text( "ProcessAll" ) );
			ProcessBtn.Click += ProcessAll;

			Major2ndControls = new ICommandBarElement[] { OpenFolder, OpenUrl };
			MinorControls = new ICommandBarElement[] { ProcessBtn };
		}

		private void FileListContext_PropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "Terminate" )
			{
				StringResources stx = new StringResources( "AppBar" );

				if ( FileListContext.Terminate )
				{
					ProcessBtn.Label = stx.Text( "ProcessAll" );
					ProcessBtn.Icon = new SymbolIcon( Symbol.Play );
				}
				else
				{
					ProcessBtn.Label = stx.Text( "Pause" );
					ProcessBtn.Icon = new SymbolIcon( Symbol.Pause );
				}
			}
		}

		private void ProcessAll( object sender, RoutedEventArgs e )
		{
			FileListContext.Terminate = !FileListContext.Terminate;
			FileListContext.ProcessAll();
		}

		private async void OpenUrl_Click( object sender, RoutedEventArgs e )
		{
			StringResources stx = new StringResources( "AdvDM" );

			LocalListBase.DownloadBookContext UrlC = new LocalListBase.DownloadBookContext();
			Dialogs.Rename UrlBox = new Dialogs.Rename( UrlC, stx.Text( "Download_Location" ) );
			UrlBox.Placeholder = "http://example.com/NN. XXXX.txt";

			await Popups.ShowDialog( UrlBox );

			if ( UrlBox.Canceled ) return;

			FileListContext.LoadUrl( UrlC );
		}

		private void ShowBookAction( object sender, RightTappedRoutedEventArgs e )
		{
			LSBookItem G = ( LSBookItem ) sender;
			FlyoutBase.ShowAttachedFlyout( G );

			SelectedBook = ( LocalBook ) G.DataContext;
		}

		private void RemoveSource( object sender, RoutedEventArgs e )
		{
			try
			{
				SelectedBook.RemoveSource();
			}
			catch ( Exception ) { }

			FileListContext.CleanUp();
		}

		private void ViewRaw( object sender, RoutedEventArgs e )
		{
			if ( SelectedBook.File != null )
			{
				ControlFrame.Instance.SubNavigateTo( this, () => new DirectTextViewer( SelectedBook.File ) );
			}
		}

		private async void Reanalyze( object sender, RoutedEventArgs e )
		{
			await SelectedBook.Reload();
			await ItemProcessor.ProcessLocal( SelectedBook );
		}

		private void FileList_ItemClick( object sender, ItemClickEventArgs e )
		{
			LocalBook Item = ( LocalBook ) e.ClickedItem;

			// Prevent double processing on the already processed item
			if ( !Item.ProcessSuccess && Item.CanProcess )
			{
				// Skip awaiting because ProcessSuccess will skip
				var j = ItemProcessor.ProcessLocal( Item );
			}

			if ( Item.ProcessSuccess )
			{
				BookItem Doc = new LocalTextDocument( Item.ZItemId );
				PageProcessor.NavigateToTOC( this, Doc );
			}
			else if ( !Item.Processing && Item.File != null )
			{
				ControlFrame.Instance.SubNavigateTo( this, () => new DirectTextViewer( Item.File ) );
			}
		}

		private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
		{
			FileListContext.SearchTerm = sender.Text.Trim();
		}
	}
}
