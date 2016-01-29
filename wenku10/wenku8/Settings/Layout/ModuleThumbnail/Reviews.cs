using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace wenku8.Settings.Layout.ModuleThumbnail
{
    using Config;
    public class Reviews : ThumbnailBase
    {
        override public bool DefaultValue { get { return false; } }

        public Reviews()
            :base( "Reviews", "CommentSection" )
        {
            Rectangle Rect = new Rectangle();
            Rect.Height = Height;
            Rect.Width = Width;
            Rect.Fill = new SolidColorBrush( Color.FromArgb( 0xFF, 0x99, 0x99, 0x99 ) );

            Rectangle RectB = new Rectangle();
            RectB.Height = Height;
            RectB.Width = 10;
            RectB.Fill = new SolidColorBrush( Properties.APPEARENCE_THEME_SHADES_90 );

            Children.Add( Rect );
            Children.Add( RectB );
        }
    }
}
