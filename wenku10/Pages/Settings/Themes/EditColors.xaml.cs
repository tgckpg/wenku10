using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;

using wenku8.Settings.Theme;

namespace wenku10.Pages.Settings.Themes
{
    public sealed partial class EditColors : Page
    {
        public static readonly string ID = typeof( EditColors ).Name;

        private ThemeSet CurrentSet;

        public EditColors()
        {
            this.InitializeComponent();
            NavigationHandler.InsertHandlerOnNavigatedBack( InnerFrameGoBack );
        }

        ~EditColors()
        {
            NavigationHandler.OnNavigatedBack -= InnerFrameGoBack;
        }

        private void InnerFrameGoBack( object sender, XBackRequestedEventArgs e )
        {
            NavigationHandler.OnNavigatedBack -= InnerFrameGoBack;
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
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );

            InitTemplate( CurrentSet = e.Parameter as ThemeSet );
        }

        private void InitTemplate( ThemeSet ColorSet )
        {
            List<ColorItem> Items = new List<ColorItem>();
            foreach( KeyValuePair<string, string> s in ThemeSet.ParamMap )
            {
                Items.Add( new ColorItem( s.Value, ColorSet.ColorDefs[ s.Key ] ) );
            }
            ColorList.ItemsSource = Items;
        }

        private async void ColorList_ItemClick( object sender, ItemClickEventArgs e )
        {
            ColorItem C = e.ClickedItem as ColorItem;
            Dialogs.ColorPicker Picker = new Dialogs.ColorPicker( C );
            await Popups.ShowDialog( Picker );

            if ( Picker.Canceled ) return;

            C.ChangeColor( Picker.UserChoice );

            CurrentSet.SetColor( C );

            global::wenku8.System.ThemeManager Mgr = new global::wenku8.System.ThemeManager();
            Mgr.Remove( CurrentSet.Name );
            Mgr.Save( CurrentSet );
        }
    }
}
