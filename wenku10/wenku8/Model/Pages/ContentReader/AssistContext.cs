using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace wenku8.Model.Pages.ContentReader
{
    using Config;
    sealed class AssistContext
    {
        private Settings.Layout.ContentReader Settings;
        public SolidColorBrush AssistBG
        {
            get
            {
                return new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_ASSISTBG );
            }
        }

        public double? H
        {
            get
            {
                if ( Settings.IsHorizontal ) return 10;
                return null;
            }
        }

        public double? W
        {
            get
            {
                if( Settings.IsHorizontal ) return null;
                return 10;
            }
        }

        public HorizontalAlignment HALeft
        {
            get { return Settings.IsHorizontal ? HorizontalAlignment.Stretch : HorizontalAlignment.Left; }
        }
        public HorizontalAlignment HARight
        {
            get { return Settings.IsHorizontal ? HorizontalAlignment.Stretch : HorizontalAlignment.Right; }
        }
        public VerticalAlignment VATop
        {
            get { return Settings.IsHorizontal ? VerticalAlignment.Top : VerticalAlignment.Stretch; }
        }
        public VerticalAlignment VABottom
        {
            get { return Settings.IsHorizontal ? VerticalAlignment.Bottom : VerticalAlignment.Stretch; }
        }

        public AssistContext()
        {
            Settings = new Settings.Layout.ContentReader();
        }
    }
}