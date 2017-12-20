using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using wenku10.Pages;
using wenku10.Pages.Sharers;

using Tasks;

namespace GR.Model.Pages
{
	using Book;
	using Book.Spider;
	using CompositeElement;
	using Ext;
	using ListItem;
	using ListItem.Sharers;
	using Loaders;
	using Model.Section;
	using Storage;

	sealed class PageProcessor
	{
		public static async Task<HubScriptItem> GetScriptFromHub( string Id, string Token )
		{
			SHSearchLoader SHLoader = new SHSearchLoader( "uuid: " + Id, new string[] { Token } );
			IEnumerable<HubScriptItem> HSIs = await SHLoader.NextPage( 1 );

			return HSIs.FirstOrDefault();
		}

		public static NameValue<Func<Page>> GetPageHandler( object Item )
		{
			if ( Item is HubScriptItem )
			{
				HubScriptItem HSI = ( HubScriptItem ) Item;
				return new NameValue<Func<Page>>( PageId.SCRIPT_DETAILS, () => new ScriptDetails( HSI ) );
			}

			else if ( Item is BookInfoItem )
			{
				BookInfoItem BItem = ( BookInfoItem ) Item;
				return new NameValue<Func<Page>>(
					PageId.BOOK_INFO_VIEW
					, () => new BookInfoView( X.Instance<BookItem>( XProto.BookItemEx, BItem.Payload ) )
				);
			}

			return new NameValue<Func<Page>>( PageId.NULL, () => null );
		}

		public static Task<string> PinToStart( BookItem Book )
		{
			if ( Book.IsSpider() )
			{
				return CreateSecondaryTile( Book );
			}
			else if ( Book.IsLocal() )
			{
				// TODO
			}
			else if ( X.Exists )
			{
				Task<string> PinTask = ( Task<string> ) X.Method( XProto.ItemProcessorEx, "CreateTile" ).Invoke( null, new BookItem[] { Book } );
				return PinTask;
			}

			return null;
		}

		public static void ReadSecondaryTile( BookItem Book )
		{
			if( Book.IsSpider() )
			{
				BackgroundProcessor.Instance.ClearTileStatus( Book.Id );
			}
		}

		private static async Task<string> CreateSecondaryTile( BookItem Book )
		{
			string TilePath = await Resources.Image.CreateTileImage( Book );
			string TileId = "ShellTile.grimoire." + GSystem.Utils.Md5( Book.Id );

			SecondaryTile S = new SecondaryTile()
			{
				TileId = TileId
				, DisplayName = Book.Title
				, Arguments = "spider|" + Book.Id
			};

			S.VisualElements.Square150x150Logo = new Uri( TilePath );
			S.VisualElements.ShowNameOnSquare150x150Logo = true;

			if ( await S.RequestCreateAsync() ) return TileId;

			return null;
		}

		public static async Task RegLiveSpider( SpiderBook SBook, BookInstruction Book, string TileId )
		{
			if ( !SBook.HasChakra )
			{
				StringResources stx = new StringResources( "Message" );

				bool Confirmed = false;

				await Popups.ShowDialog( UIAliases.CreateDialog(
					stx.Str( "TileUpdateSupport" ), stx.Str( "ShellTile" )
					, () => Confirmed = true
					, stx.Str( "Yes" ), stx.Str( "No" )
				) );

				if ( Confirmed ) BackgroundProcessor.Instance.CreateTileUpdateForBookSpider( Book.Id, TileId );
			}
		}

		public static async Task<AsyncTryOut<Chapter>> TryGetAutoAnchor( BookItem Book, bool Sync = true )
		{
			StringResources stx = new StringResources( "LoadingMessage" );
			if ( Sync )
			{
				MessageBus.SendUI( typeof( PageProcessor ), stx.Str( "SyncingAnchors" ), Book.Id );
				await new AutoAnchor( Book ).SyncSettings();
			}

			MessageBus.SendUI( typeof( PageProcessor ), stx.Str( "ProgressIndicator_Message" ), Book.Id );

			TaskCompletionSource<TOCSection> TCS = new TaskCompletionSource<TOCSection>();
			BookLoader BLoader = new BookLoader( b =>
			{
				if ( b == null )
				{
					TCS.TrySetResult( null );
				}
				else
				{
					MessageBus.SendUI( typeof( PageProcessor ), stx.Str( "LoadingVolumes" ), Book.Id );
					new VolumeLoader( b2 =>
					{
						if ( b2 == null )
						{
							TCS.TrySetResult( null );
						}
						else
						{
							TCS.TrySetResult( new TOCSection( b2 ) );
						}
					} ).Load( b );
				}
			} );

			BLoader.Load( Book, true );

			TOCSection TOCData = await TCS.Task;

			if ( TOCData == null )
			{
				return new AsyncTryOut<Chapter>( false, null );
			}

			if ( TOCData.AnchorAvailable )
			{
				return new AsyncTryOut<Chapter>( true, TOCData.AutoAnchor );
			}
			else
			{
				return new AsyncTryOut<Chapter>( false, TOCData.FirstChapter );
			}
		}

		private static string PrepareReader( BookItem Book )
		{
			ControlFrame.Instance.BackStack.Remove( PageId.CONTENT_READER_H );
			ControlFrame.Instance.BackStack.Remove( PageId.CONTENT_READER_V );
			// TODO: Guess the Orientation

			return PageId.CONTENT_READER_H;
		}

		public static void NavigateToReader( BookItem Book, Chapter C )
		{
			string PageName = PrepareReader( Book );

			if ( PageName == PageId.CONTENT_READER_H )
			{
				ControlFrame.Instance.NavigateTo( PageName, () => new ContentReaderHorz( Book, C ) );
			}
			else
			{
				ControlFrame.Instance.NavigateTo( PageName, () => new ContentReaderVert( Book, C ) );
			}
		}

	}
}