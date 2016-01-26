using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace wenku8.Settings.Layout.ModuleThumbnail
{
    using Config;

    public class InfoView : ThumbnailBase
    {
        public InfoView()
            :base( "InfoView", "BookInfoSection" )
        {
            Rectangle Rect = new Rectangle();
            Rect.Height = Height;
            Rect.Width = Width;
            Rect.Fill = new SolidColorBrush( Properties.APPEARENCE_THEME_MINOR_BACKGROUND_COLOR );
            Children.Add( Rect );
        }
    }
}
