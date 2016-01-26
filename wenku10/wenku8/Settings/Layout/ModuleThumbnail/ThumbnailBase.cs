using System;
using Windows.UI.Xaml.Controls;

namespace wenku8.Settings.Layout.ModuleThumbnail
{
    public class ThumbnailBase : Canvas
    {
        virtual public string ModName { get; set; }
        virtual public string ViewName { get; set; }

        public ThumbnailBase( string ModName, string ViewName )
            :base()
        {
            this.ModName = ModName;
            this.ViewName = ViewName;
            Width = 70;
            Height = 100;
        }
    }

}
