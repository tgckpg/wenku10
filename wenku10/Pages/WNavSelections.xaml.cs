using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;

namespace wenku10.Pages
{
    sealed partial class WNavSelections : Page, IAnimaPage
    {
        private WNavSelections()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        public WNavSelections( INavSelections Sel )
            :this()
        {
            NavList.DataContext = Sel;
            Sel.Load();
        }

        #region Anima
        Storyboard AnimaStory = new Storyboard();

        public async Task EnterAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

            AnimaStory.Begin();
            await Task.Delay( 350 );
        }

        public async Task ExitAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0 );
            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30 );

            AnimaStory.Begin();
            await Task.Delay( 350 );
        }
        #endregion

        private void SetTemplate()
        {
            LayoutRoot.RenderTransform = new TranslateTransform();
        }

        private void GotoNavigation( object sender, ItemClickEventArgs e )
        {
            SubtleUpdateItem s = e.ClickedItem as SubtleUpdateItem;
            if ( s.Nav == typeof( WCateList ) )
            {
                ControlFrame.Instance.SubNavigateTo( this, () => new WCateList( s ) );
            }
            else
            {
                ControlFrame.Instance.NavigateTo( PageId.W_NAV_LIST + s.Name, () => new WNavList( s ) );
            }
        }

    }
}