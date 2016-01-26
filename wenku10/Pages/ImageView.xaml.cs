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

using Net.Astropenguin.Controls;
using Net.Astropenguin.Logging;

using wenku8.Model.ListItem;

namespace wenku10.Pages
{
    public sealed partial class ImageView : Page
    {
        public static readonly string ID = typeof( ImageView ).Name;

        private ImageThumb Img;

        public ImageView()
        {
            this.InitializeComponent();

            // Override the Left Pane
            NavigationHandler.InsertHandlerOnNavigatedBack( GoBack );
        }

        private void GoBack( object sender, XBackRequestedEventArgs e )
        {
            NavigationHandler.OnNavigatedBack -= GoBack;
            if( Frame.CanGoBack )
            {
                Frame.GoBack();
                e.Handled = true;
            }
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            base.OnNavigatedFrom( e );
            Logger.Log( ID, string.Format( "OnNavigatedFrom: {0}", e.SourcePageType.Name ), LogType.INFO );
            if ( Img != null )
            {
                var j = Img.Set();
            }
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            Img = e.Parameter as ImageThumb;
            SetImage();
        }

        private async void SetImage()
        {
            MainImage.Source = await Img.GetFull();
        }

    }
}
