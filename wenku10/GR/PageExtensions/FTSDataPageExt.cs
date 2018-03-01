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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Data;
	using Database.Models;
	using DataSources;
	using GR.Model.Pages;
	using GSystem;
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
		public bool MajorNav => true;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private FTSViewSource ViewSource;

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
			TryBuildIndex();
		}

		private async void TryBuildIndex()
		{
			if ( !ViewSource.FTSData.IsBuilt )
			{
				string EstSize = Utils.AutoByteUnit( ( ulong ) ( 3.77 * ( await Shared.Storage.FileSize( "books.db" ) ) ) );

				bool BuildIndex = false;

				StringResources stx = new StringResources( "Message" );
				await Popups.ShowDialog( UIAliases.CreateDialog(
					string.Format( stx.Str( "ConfirmBuildFTS" ), EstSize )
					, () => BuildIndex = true
					, stx.Str( "Yes" ), stx.Str( "No" )
				) );

				if ( BuildIndex )
				{
					await ViewSource.FTSData.Rebuild();
				}
			}
		}

		public async void OpenItem( object DataContext )
		{
			if ( DataContext is GRRow<FTSResult> RsRow )
			{
				Chapter Ch = Shared.BooksDb.Chapters.Find( RsRow.Source.ChapterId );
				if ( Ch == null )
				{
					StringResources stx = new StringResources( "Message" );
					await Popups.ShowDialog( UIAliases.CreateDialog( string.Format( stx.Str( "FTSNeedsRebuild" ) ) ) );
					return;
				}

				Ch.Book = Shared.BooksDb.QueryBook( x => x.Id == Ch.BookId ).FirstOrDefault();

				// Chapter is hard-linked to Volume. So we can load it confidently
				await Shared.BooksDb.LoadCollectionAsync( Ch.Book, x => x.Volumes, x => x.Index );
				foreach( Volume V in Ch.Book.Volumes )
				{
					await Shared.BooksDb.LoadCollectionAsync( V, x => x.Chapters, x => x.Index );
				}

				BookItem BkItem = ItemProcessor.GetBookItem( Ch.Book );
				PageProcessor.NavigateToReader( BkItem, Ch );
			}
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

	}
}