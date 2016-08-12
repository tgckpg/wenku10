using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using wenku8.Model;
using wenku8.Model.Comments;
using wenku8.Settings;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.IO;
using Net.Astropenguin.DataModel;
using wenku8.Ext;

namespace wenku10.Pages.InfoViews
{
    sealed partial class ReplyList : Page
    {
        private Observables<Comment, Comment> Replies;

        public ReplyList()
        {
            this.InitializeComponent();
            Replies = new Observables<Comment, Comment>();
            RepliesView.ItemsSource = Replies;
        }

        public async Task OpenReview( Review R )
        {
            CommentLoader CL = new CommentLoader(
                R.Id
                , X.Call<XKey[]>( XProto.WRequest, "GetReplies", R.Id )
                , new CommentLoader.CommentXMLParser( GetReplies )
            );

            IList<Comment> FirstLoad = await CL.NextPage();

            Replies.ConnectLoader( CL );
            Replies.UpdateSource( FirstLoad );
        }

        private Comment[] GetReplies( string xml, out int PageCount )
        {
            Comment[] Comments = null;
            XDocument p = XDocument.Parse( xml );
            IEnumerable<XElement> CPreviews = p.Descendants( "item" );

            // Set pagelimit
            int.TryParse( p.Descendants( "page" ).ElementAt( 0 ).Attribute( "num" ).Value, out PageCount );

            int l = CPreviews.Count();
            Comments = new Comment[ l ];
            for ( int i = 0; i < l; i++ )
            {
                XElement xe = CPreviews.ElementAt( i );
                XElement xu = xe.Descendants( "user" ).ElementAt( 0 );

                Comments[ i ] = new Comment()
                {
                    Username = xu.Value
                    , Title = xe.Descendants( "content" ).ElementAt( 0 ).Value
                    , UserId = xu.Attribute( "uid" ).Value
                    , PostTime = xe.Attribute( "timestamp" ).Value
                };
            }

            return Comments;
        }
    }
}
