using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace wenku8.Settings.Layout.ModuleThumbnail
{
    using Config;

    public class TOCView : ThumbnailBase
    {
        public TOCView()
            : base( "TOCView", "TOCSection" )
        {
            Rectangle Rect = new Rectangle();
            Rect.Height = Height;
            Rect.Width = Width;
            Rect.Fill = new SolidColorBrush( Properties.APPEARENCE_THEME_SHADES_60 );

            Rectangle RectB = new Rectangle();
            RectB.Height = Height;
            RectB.Width = 10;
            RectB.Fill = new SolidColorBrush( Properties.APPEARENCE_THEME_SHADES_70 );

            Rectangle RectD = new Rectangle();
            RectD.Height = 0.5 * Height;
            RectD.Width = 60;
            RectD.Fill = new SolidColorBrush( Properties.APPEARENCE_THEME_SHADES_90 );
            SetLeft( RectD, 10 ); SetTop( RectD, 0.5 * Height );

            Children.Add( Rect );
            Children.Add( RectB );
            Children.Add( RectD );
        }
    }
}
