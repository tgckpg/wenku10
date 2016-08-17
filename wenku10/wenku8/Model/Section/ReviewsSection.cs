using System;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.UI;
using Net.Astropenguin.UI.Icons;

namespace wenku8.Model.Section
{
    using Book;
    using Ext;
    using Comments;
    using ListItem;

    sealed class ReviewsSection : ActiveData
    {
        public static readonly string ID = typeof( ReviewsSection ).Name;

        private BookItem ThisBook;
        private Review CurrentReview;

        public List<PaneNavButton> Controls { get; private set; }

        private bool _IsLoading = false;
        public bool IsLoading
        {
            get { return _IsLoading; }
            private set
            {
                _IsLoading = value;
                NotifyChanged( "IsLoading" );
            }
        }

        private Page _SubListView;
        public Page SubListView
        {
            get { return _SubListView; }
            private set
            {
                _SubListView = value;
                NotifyChanged( "SubListView" );
            }
        }

        private wenku10.Pages.ReviewsInput _ReviewsInput;
        public wenku10.Pages.ReviewsInput ReviewsInput
        {
            get { return _ReviewsInput; }
            private set
            {
                _ReviewsInput = value;
                NotifyChanged( "ReviewsInput" );
            }
        }

        public Observables<Comment, Comment> Comments { get; private set; }

        public ReviewsSection( BookItem b )
        {
            IsLoading = true;
            ThisBook = b;
        }

        public async Task Load()
        {
            IsLoading = true;
            CommentLoader CL = new CommentLoader(
                ThisBook.Id
                , X.Call<XKey[]>( "wenku8.Settings.WRequest, wenku8-protocol", "GetComments", ThisBook.Id )
                , new CommentLoader.CommentXMLParser( GetReviews )
            );
            IList<Comment> FirstLoad = await CL.NextPage();
            IsLoading = false;

            Comments = new Observables<Comment, Comment>( FirstLoad );
            Comments.LoadStart += ( s, e ) => IsLoading = true;
            Comments.LoadEnd += ( se, e ) => IsLoading = false;

            Comments.ConnectLoader( CL );

            NotifyChanged( "Comments" );
            SetControls( "NewComment", "Reload" );
        }

        public async Task OpenReview( Review R )
        {
            CurrentReview = R;
            wenku10.Pages.BookInfoControls.ReplyList RView = new wenku10.Pages.BookInfoControls.ReplyList();
            await RView.OpenReview( R );

            SubListView = RView;

            SetControls( "NewComment", "Reload", "Back" );
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

        private void SetControls( params string[] Acq )
        {
            Controls = new List<PaneNavButton>();
            foreach( string Ctrl in Acq )
            {
                switch ( Ctrl )
                {

                    case "NewComment":
                        Controls.Add(
                            new PaneNavButton(
                                new IconComment() { AutoScale = true }
                                , CC_WriteReview
                            )
                        );
                        break;

                    case "Back":
                        Controls.Add(
                            new PaneNavButton(
                                new IconLogout() { AutoScale = true }
                                , CC_CloseReview
                            )
                        );
                        break;

                    case "Reload":
                        Controls.Add(
                            new PaneNavButton(
                                new IconReload() { AutoScale = true }
                                , CC_Reload
                            )
                        );
                        break;

                    case "Submit":
                        Controls.Add(
                            new PaneNavButton(
                                new IconTick() { AutoScale = true }
                                , CC_Submit
                            )
                        );
                        break;

                    case "Cancel":
                        Controls.Add(
                            new PaneNavButton(
                                new IconCross() { AutoScale = true }
                                , CC_Cancel
                            )
                        );
                        break;
                }
            }

            NotifyChanged( "Controls" );
        }

        public async void CC_Submit()
        {
            if ( !await ReviewsInput.Validate() ) return;

            IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, true );
            if( ReviewsInput.IsReview )
            {
                wCache.InitDownload(
                    "POSTREPLY"
                    , X.Call<XKey[]>(
                        XProto.WRequest
                        , "GetPostReply"
                        , CurrentReview.Id
                        , ReviewsInput.RContent
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
                        XProto.WRequest
                        , "GetPostReview"
                        , ThisBook.Id
                        , ReviewsInput.RTitle
                        , ReviewsInput.RContent
                    )
                    , PostSuccess, PostFailed
                    , false
                );
            }
        }

        private void PostSuccess( DRequestCompletedEventArgs e, string id )
        {
            CC_Cancel();
            CC_Reload();
        }

        private async void PostFailed( string arg1, string arg2, Exception ex )
        {
            if ( ex.XTest( XProto.WException ) )
            {
                if ( ex.XProp<Enum>( "WCode" ).Equals( X.Const<Enum>( XProto.WCode, "LOGON_REQUIRED" ) ) )
                {
                    // Prompt login
                    wenku10.Pages.Dialogs.Login Login = new wenku10.Pages.Dialogs.Login(
                        X.Singleton<IMember>( XProto.Member )
                    );
                    await Popups.ShowDialog( Login );
                }
            }
        }

        public void CC_Cancel()
        {
            ReviewsInput = null;
            NotifyChanged( "ReviewsInput", "RInputState" );

            if ( CurrentReview == null )
            {
                SetControls( "NewComment", "Reload" );
            }
            else
            {
                SetControls( "NewComment", "Reload", "Back" );
            }
        }

        public void ControlAction( PaneNavButton Control )
        {
            Control.Action();
        }

        public void CC_WriteReview()
        {
            if( SubListView == null )
            {
                ReviewsInput = new wenku10.Pages.ReviewsInput( ThisBook );
            }
            else
            {
                ReviewsInput = new wenku10.Pages.ReviewsInput( CurrentReview );
            }

            NotifyChanged( "ReviewsInput", "RInputState" );
            SetControls( "Submit", "Cancel" );
        }

        private void CC_CloseReview()
        {
            SubListView = null;
            CurrentReview = null;
            NotifyChanged( "ReplyPageOpened" );
            SetControls( "NewComment", "Reload" );
        }

        private async void CC_Reload()
        {
            if( CurrentReview == null )
            {
                await Load();
            }
            else
            {
                await OpenReview( CurrentReview );
            }
        }

    }
}