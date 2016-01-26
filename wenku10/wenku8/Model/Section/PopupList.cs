using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.UI;

namespace wenku8.Model.Section
{
    using ListItem;

    class PopupList : ActiveData
    {
        public ControlState PageStatus { get; private set; }
        public Page FrameContent { get; private set; }
        public SubtleUpdateItem Item { get; set; }

        private Frame F;
        public PopupList( SubtleUpdateItem S, Frame F )
        {
            Item = S;
            this.F = F;

            PageStatus = ControlState.Reovia;
            FrameContent = Activator.CreateInstance( S.Nav, this ) as Page;

            NotifyChanged( "PageStatus" );
            NotifyChanged( "FrameContent" );
        }

        public void Close()
        {
            FrameContent = null;
            PageStatus = ControlState.Foreatii;
            NotifyChanged( "PageStatus" );
            NotifyChanged( "FrameContent" );
        }

        public void Navigate( Type p, object param )
        {
            F.Navigate( p, param );
        }
    }
}
