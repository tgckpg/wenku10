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

using wenku8.Model.Book;
using wenku8.Model.Comments;
using wenku8.Model.ListItem;
using wenku8.Model.Section;

namespace wenku10.Pages.BookInfoControls
{
    public sealed partial class Comments : Page
    {
        private ReviewsSection ReviewsSection;

        private Comments()
        {
            this.InitializeComponent();
        }

        internal Comments(  BookItem Book )
            :this()
        {
            SetTemplate( Book );
        }

        private async void OpenComment( object sender, ItemClickEventArgs e )
        {
            await ReviewsSection.OpenReview( e.ClickedItem as Review );
        }

        private void ControlClick( object sender, ItemClickEventArgs e )
        {
            ReviewsSection.ControlAction( e.ClickedItem as PaneNavButton );
        }

        private async void SetTemplate( BookItem b )
        {
            ReviewsSection = new ReviewsSection( b );
            // Let's try the async method this time
            await ReviewsSection.Load();
            DataContext = ReviewsSection;
        }

    }
}