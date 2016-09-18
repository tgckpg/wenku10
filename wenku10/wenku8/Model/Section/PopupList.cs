using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.DataModel;

namespace wenku8.Model.Section
{
    using ListItem;

    sealed class PopupList : ActiveData
    {
        public Page FrameContent { get; private set; }
        public SubtleUpdateItem Item { get; set; }

        private Frame F;
        public PopupList( SubtleUpdateItem S, Frame F )
        {
            Item = S;
            this.F = F;

            FrameContent = Activator.CreateInstance( S.Nav, this ) as Page;

            NotifyChanged( "FrameContent" );
        }

        public void Close()
        {
            FrameContent = null;
            NotifyChanged( "FrameContent" );
        }

        public void Navigate( Type p, object param )
        {
            F.Navigate( p, param );
        }
    }
}