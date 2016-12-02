using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Book;
using wenku8.Model.Comments;
using wenku8.Model.Interfaces;
using wenku8.Model.Loaders;

namespace wenku10.Pages.BookInfoControls
{
    public sealed partial class Comments : Page, ICmdControls, INavPage
    {
#pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get; private set; }

        private BookItem ThisBook;

        AppBarButton AddBtn;
        AppBarButtonEx ReloadBtn;
        AppBarButton SubmitBtn;

        ButtonOperation ReloadOp;
        Review CurrentReview;

        private Comments()
        {
            this.InitializeComponent();
        }

        internal Comments( BookItem Book )
            : this()
        {
            ThisBook = Book;
            SetTemplate( Book );
        }

        public void SoftOpen() { NavigationHandler.InsertHandlerOnNavigatedBack( ClosePages ); }
        public void SoftClose() { NavigationHandler.OnNavigatedBack -= ClosePages; }

        private async void OpenComment( object sender, ItemClickEventArgs e )
        {
            await OpenReview( ( Review ) e.ClickedItem );
        }

        private async Task OpenReview( Review R )
        {
            CurrentReview = R;
            ReplyList RepliesView = new ReplyList();
            await RepliesView.OpenReview( R );

            SubListView.Content = RepliesView;
            TransitionDisplay.SetState( SubListView, TransitionState.Active );
        }

        private void SetTemplate( BookItem b )
        {
            InitAppBar();
        }

        private void ClosePages( object sender, XBackRequestedEventArgs e )
        {
            if ( ReviewsFrame.Content != null )
            {
                e.Handled = true;
                CloseFrame( ReviewsFrame );
                SetControls( ReloadBtn, AddBtn );
                return;
            }

            if ( SubListView.Content != null )
            {
                e.Handled = true;
                CloseFrame( SubListView );
                SetControls( ReloadBtn, AddBtn );
                return;
            }

            NavigationHandler.OnNavigatedBack -= ClosePages;
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "AppBar", "AppResources" );
            AddBtn = UIAliases.CreateAppBarBtn( Symbol.Add, stx.Str( "AddComment" ) );
            AddBtn.Click += WriteReview;

            ReloadBtn = UIAliases.CreateAppBarBtnEx( Symbol.Refresh, stx.Text( "Reload" ) );
            ReloadOp = new ButtonOperation( ReloadBtn );
            ReloadOp.SetOp( Reload );

            SubmitBtn = UIAliases.CreateAppBarBtn( Symbol.Send, stx.Text( "Button_Post", "AppResources" ) );
            SubmitBtn.Click += SubmitReview;

            MajorControls = new ICommandBarElement[] { ReloadBtn, AddBtn };
        }

        private async Task Reload()
        {
            if( SubListView.Content != null )
            {
                await ( ( ReplyList ) SubListView.Content ).OpenReview( CurrentReview );
            }
            else
            {
                await ReloadComments();
            }
        }

        private async Task ReloadComments()
        {
            CommentLoader CL = new CommentLoader(
                ThisBook.Id
                , X.Call<XKey[]>( XProto.WRequest, "GetComments", ThisBook.Id )
                , new CommentLoader.CommentXMLParser( GetReviews )
            );

            IList<Comment> FirstLoad = await CL.NextPage();

            Observables<Comment, Comment> CommentsLL = new Observables<Comment, Comment>( FirstLoad );
            CommentsLL.LoadStart += ( s, e ) => ReloadBtn.IsLoading = true;
            CommentsLL.LoadEnd += ( se, e ) => ReloadBtn.IsLoading = false;

            CommentsLL.ConnectLoader( CL );

            CommentsList.ItemsSource = CommentsLL;
        }

        private Review[] GetReviews( string xml, out int PageCount )
        {
            Review[] Comments = null;
            XDocument p = XDocument.Parse( xml );
            IEnumerable<XElement> CPreviews = p.Descendants( "item" );

            // Set pagelimit
            int.TryParse( p.Descendants( "page" ).ElementAt( 0 ).Attribute( "num" ).Value, out PageCount );
            int l;

            Comments = new Review[ l = CPreviews.Count() ];
            for ( int i = 0; i < l; i++ )
            {
                XElement xe = CPreviews.ElementAt( i );
                XElement xu = xe.Descendants( "user" ).ElementAt( 0 );

                Comments[ i ] = new Review()
                {
                    Id = xe.Attribute( "rid" ).Value
                    , Username = xu.Value
                    , Title = xe.Descendants( "content" ).ElementAt( 0 ).Value
                    , UserId = xu.Attribute( "uid" ).Value
                    , PostTime = xe.Attribute( "posttime" ).Value
                    , LastReply = xe.Attribute( "replytime" ).Value
                    , NumReplies = xe.Attribute( "replies" ).Value
                };
            }

            return Comments;
        }

        private void SetControls( params ICommandBarElement[] Btns )
        {
            MajorControls = Btns;
            ControlChanged?.Invoke( this );
        }

        private void SubmitReview( object sender, RoutedEventArgs e )
        {
            SubmitBtn.IsEnabled = false;
            SubmitReview();
        }

        private async void SubmitReview()
        {
            ReviewsInput Input = ( ReviewsInput ) ReviewsFrame.Content;
            if ( !await Input.Validate() ) return;

            IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, true );
            if ( Input.IsReview )
            {
                wCache.InitDownload(
                    "POSTREPLY"
                    , X.Call<XKey[]>(
                        XProto.WRequest, "GetPostReply"
                        , CurrentReview.Id, Input.RContent
                    )
                    , PostSuccess, PostFailed
                    , false
                );
            }
            else
            {
                wCache.InitDownload(
                    "POSTREVIEW"
                    , X.Call<XKey[]>(
                        XProto.WRequest, "GetPostReview"
                        , ThisBook.Id, Input.RTitle, Input.RContent
                    )
                    , PostSuccess, PostFailed
                    , false
                );
            }
        }

        private void PostSuccess( DRequestCompletedEventArgs e, string id )
        {
            CloseFrame( ReviewsFrame );
            if ( SubListView.Content == null )
            {
                var j = ReloadComments();
            }
            else
            {
                var j = ( ( ReplyList ) SubListView.Content ).OpenReview( CurrentReview );
            }

            SetControls( ReloadBtn, AddBtn );
        }

        private async void PostFailed( string arg1, string arg2, Exception ex )
        {
            if ( ex.XTest( XProto.WException ) )
            {
                if ( ex.XProp<Enum>( "WCode" ).Equals( X.Const<Enum>( XProto.WCode, "LOGON_REQUIRED" ) ) )
                {
                    // Prompt login
                    Dialogs.Login Login = new Dialogs.Login( X.Singleton<IMember>( XProto.Member ) );
                    await Popups.ShowDialog( Login );

                    // Auto submit
                    if ( !Login.Canceled ) SubmitReview();
                }
            }
        }

        private void WriteReview( object sender, RoutedEventArgs e )
        {
            if ( SubListView.Content == null )
            {
                ReviewsFrame.Content = new ReviewsInput( ThisBook );
            }
            else
            {
                ReviewsFrame.Content = new ReviewsInput( CurrentReview );
            }

            TransitionDisplay.SetState( ReviewsFrame, TransitionState.Active );
            SetControls( SubmitBtn );
        }

        private async void CloseFrame( Frame F )
        {
            TransitionDisplay.SetState( F, TransitionState.Inactive );
            await Task.Delay( 350 );
            F.Content = null;
        }

    }
}