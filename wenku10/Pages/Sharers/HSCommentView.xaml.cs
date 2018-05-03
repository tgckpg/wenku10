using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;

using GR.AdvDM;
using GR.CompositeElement;
using GR.Effects;
using GR.Ext;
using GR.Model.Comments;
using GR.Model.Interfaces;
using GR.Model.ListItem.Sharers;
using GR.Model.REST;
using GR.Resources;

using CryptAES = GR.GSystem.CryptAES;

namespace wenku10.Pages.Sharers
{
	using SHTarget = SharersRequest.SHTarget;

	public sealed partial class HSCommentView : Page, ICmdControls
	{
		private HubScriptItem BindItem;

		private IMember Member;

		private SHTarget CCTarget = SHTarget.SCRIPT;

		private CryptAES Crypt;

		private volatile bool CommInit = false;

		private Observables<HSComment, HSComment> CommentsSource;

		private AppBarButton AddBtn;
		private AppBarButton SubmitBtn;
		private AppBarButton DiscardBtn;

		private AppBarButton[] CommentControls;

		private bool IsEditorOpened
		{
			set
			{
				TransitionDisplay.SetState( CommentEditor, value ? TransitionState.Active : TransitionState.Inactive );
				CommentInput.IsEnabled = value;
				if( value )
				{
					NavigationHandler.InsertHandlerOnNavigatedBack( DiscardComment );
				}
				else
				{
					NavigationHandler.OnNavigatedBack -= DiscardComment;
				}
			}
			get
			{
				return CommentInput.IsEnabled;
			}
		}

		private string CCId;

		private int LoadLevel = 0;

		public event ControlChangedEvent ControlChanged;

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private HSCommentView()
		{
			this.InitializeComponent();
		}

		internal HSCommentView( HubScriptItem HSI, CryptAES Crypt )
			:this()
		{
			this.Crypt = Crypt;
			BindItem = HSI;

			SetTemplate();
		}

		private void SetTemplate()
		{
			InitAppBar();

			Member = X.Singleton<IMember>( XProto.SHMember );

			CommentSection.Visibility = Visibility.Visible;

			if ( !CommInit )
			{
				CommInit = true;
				ReloadComments();
			}
		}

		private void InitAppBar()
		{
			StringResources stx = StringResources.Load( "AppBar", "AppResources" );

			AddBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Str( "AddComment" ) );
			AddBtn.Click += ( sender, e ) =>
			{
				CCTarget = SHTarget.SCRIPT;
				CCId = BindItem.Id;
				NewComment( AddBtn.Label );
			};

			SubmitBtn = UIAliases.CreateAppBarBtn( Symbol.Send, stx.Text( "Button_Post", "AppResources" ) );
			SubmitBtn.Click += ( s, e ) => SubmitComment();

			DiscardBtn = UIAliases.CreateAppBarBtn( Symbol.Delete, "Discard" );
			DiscardBtn.Click += ( sender, e ) => DiscardComment();

			CommentControls = new AppBarButton[] { AddBtn };

			MajorControls = CommentControls;
		}

		public async void OpenCommentStack( string CommId )
		{
			CommInit = true;

			DisplayControls( CommentControls );

			CommentSection.Visibility = Visibility.Visible;

			bool LoadFirst = true;
			Func<SHTarget, int, uint, string[], PostData> StackRequest = ( a, b, c, Ids ) =>
			{
				if ( LoadFirst )
				{
					LoadFirst = false;
					return Shared.ShRequest.GetCommentStack( Ids[ 0 ] );
				}

				return Shared.ShRequest.GetComments( a, b, c, Ids );
			};

			HSLoader<HSComment> CLoader = new HSLoader<HSComment>( CommId, SHTarget.COMMENT, StackRequest )
			{
				ConvertResult = ( x ) => x.Flattern( y => y.Replies )
			};

			IEnumerable<HSComment> FirstPage = await CLoader.NextPage();

			CommentsSource = new Observables<HSComment, HSComment>( FirstPage.ToArray() );

			CommentsSource.LoadStart += ( x, y ) => MarkLoading();
			CommentsSource.LoadEnd += ( x, y ) => MarkNotLoading();

			CommentsSource.ConnectLoader( CLoader );
			CommentList.ItemsSource = CommentsSource;
		}

		private async void ReloadComments()
		{
			if ( 0 < LoadLevel ) return;

			MarkLoading();
			HSLoader<HSComment> CLoader = new HSLoader<HSComment>( BindItem.Id, SHTarget.SCRIPT, Shared.ShRequest.GetComments )
			{
				ConvertResult = ( x ) => x.Flattern( y => y.Replies )
			};

			IList<HSComment> FirstPage = await CLoader.NextPage();
			MarkNotLoading();

			if ( BindItem.Encrypted )
			{
				if ( Crypt == null )
				{
					CommentsSource = new Observables<HSComment, HSComment>( CrippledComments( FirstPage ) );
					CommentsSource.ConnectLoader( CLoader, CrippledComments );
				}
				else
				{
					CommentsSource = new Observables<HSComment, HSComment>( DecryptComments( FirstPage ) );
					CommentsSource.ConnectLoader( CLoader, DecryptComments );
				}
			}
			else
			{
				CommentsSource = new Observables<HSComment, HSComment>( FirstPage );
				CommentsSource.ConnectLoader( CLoader );
			}

			CommentsSource.LoadStart += ( x, y ) => MarkLoading();
			CommentsSource.LoadEnd += ( x, y ) => MarkNotLoading();
			CommentList.ItemsSource = CommentsSource;
		}

		private IList<HSComment> DecryptComments( IList<HSComment> Comments )
		{
			foreach( HSComment HSC in Comments )
			{
				try
				{
					HSC.Title = Crypt.Decrypt( HSC.Title );
				}
				catch ( Exception )
				{
					HSC.DecFailed = true;
					HSC.Title = CryptAES.RawBytes( HSC.Title );
				}
			}

			return Comments;
		}

		private IList<HSComment> CrippledComments( IList<HSComment> Comments )
		{
			foreach( HSComment HSC in Comments )
			{
				HSC.DecFailed = true;
				HSC.Title = CryptAES.RawBytes( HSC.Title );
			}

			return Comments;
		}

		private void CommentList_ItemClick( object sender, ItemClickEventArgs e )
		{
			HSComment HSC = ( HSComment ) e.ClickedItem;

			if ( HSC.Folded )
			{
				if ( HSC == HSComment.ActiveInstance ) return;
				int i = CommentsSource.IndexOf( HSC );

				// The Load more always appeared in the next level
				// i.e. previous item is always a parent
				HSComment ParentHSC = CommentsSource[ i - 1 ];

				HSLoader<HSComment> CLoader = new HSLoader<HSComment>( ParentHSC.Id, SHTarget.COMMENT, Shared.ShRequest.GetComments )
				{
					ConvertResult = ( x ) =>
						x.Flattern( y =>
						{
							y.Level += HSC.Level;
							return y.Replies;
						} )
				};

				CommentsSource.LoadStart += ( x, y ) => MarkLoading();
				CommentsSource.LoadEnd += ( x, y ) => MarkNotLoading();

				// Remove the LoadMore thing
				CommentsSource.RemoveAt( i );
				CommentsSource.InsertLoader( i, CLoader );

				// Load it or else will only be triggered when pgae reads bottom
				var j = CommentsSource.LoadMoreItemsAsync( 20 );
			}

			HSC.MarkSelect();
		}

		private void NewReply( object sender, RoutedEventArgs e )
		{
			HSComment HSC = ( HSComment ) ( ( FrameworkElement ) sender ).DataContext;
			StringResources stx = StringResources.Load( "AppBar" );

			CCTarget = SHTarget.COMMENT;
			CCId = HSC.Id;
			NewComment( stx.Text( "Reply" ) );
		}

		private void NewComment( string Label )
		{
			IsEditorOpened = true;
			CommentModeLabel.Text = Label;

			if( BindItem.ForceEncryption && Crypt == null )
			{
				DisplayControls( DiscardBtn );

				StringResources stx = StringResources.Load();
				CommentError.Text = stx.Text( "CommentsEncrypted" );
			}
			else
			{
				DisplayControls( DiscardBtn, SubmitBtn );
				CommentError.Text = "";
			}
		}

		private async void SubmitComment()
		{
			string Data;
			CommentInput.Document.GetText( Windows.UI.Text.TextGetOptions.None, out Data );
			Data = Data.Trim();

			if ( string.IsNullOrEmpty( Data ) )
			{
				CommentInput.Focus( FocusState.Keyboard );
				return;
			}

			if ( !Member.IsLoggedIn )
			{
				Dialogs.Login LoginBox = new Dialogs.Login( Member );
				await Popups.ShowDialog( LoginBox );
				if ( !Member.IsLoggedIn ) return;
			}

			if ( Crypt != null ) Data = Crypt.Encrypt( Data );

			new RuntimeCache() { EN_UI_Thead = true }.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.Comment( CCTarget, CCId, Data, Crypt != null )
				, CommentSuccess
				, CommentFailed 
				, false
			);
		}

		private void CommentFailed( string CacheName, string Id, Exception ex )
		{
			CommentError.Text = ex.Message;
		}

		private void CommentSuccess( DRequestCompletedEventArgs e, string Id )
		{
			try
			{
				JsonStatus.Parse( e.ResponseString );
				CommentInput.Document.SetText( Windows.UI.Text.TextSetOptions.None, "" );
				DropComment();
				ReloadComments();
			}
			catch( Exception ex )
			{
				CommentError.Text = ex.Message;
			}
		}

		private void DiscardComment( object sender, XBackRequestedEventArgs e )
		{
			e.Handled = true;
			DiscardComment();
		}

		private async void DiscardComment()
		{
			string Data;
			CommentInput.Document.GetText( Windows.UI.Text.TextGetOptions.None, out Data );
			Data = Data.Trim();

			if ( !string.IsNullOrEmpty( Data ) )
			{
				StringResources stx = StringResources.Load( "Message" );
				MessageDialog ConfirmDialog = new MessageDialog( "Are you sure you want to discard your message?" );

				bool No = true;
				ConfirmDialog.Commands.Add( new UICommand( stx.Str( "Yes" ), x => No = false ) );
				ConfirmDialog.Commands.Add( new UICommand( stx.Str( "No" ) ) );

				await Popups.ShowDialog( ConfirmDialog );

				if ( No ) return;
			}

			DropComment();
		}

		private void DropComment()
		{
			CommentInput.Document.SetText( Windows.UI.Text.TextSetOptions.None, "" );

			DisplayControls( CommentControls );
			IsEditorOpened = false;
		}

		private void MarkLoading()
		{
			LoadLevel++;
		}

		private void MarkNotLoading()
		{
			LoadLevel--;
		}

		private void DisplayControls( params AppBarButton[] Btns )
		{
			MajorControls = Btns;
			ControlChanged?.Invoke( this );
		}
	}
}