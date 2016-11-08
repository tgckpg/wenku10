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

using Net.Astropenguin.Helpers;

using wenku8.Settings.Theme;

namespace wenku10.Pages.Settings.Themes
{
    sealed partial class EditColors : Page
    {
        private ThemeSet CurrentSet;

        private EditColors()
        {
            this.InitializeComponent();
        }

        public EditColors( ThemeSet ThemeColors )
            : this()
        {
            InitTemplate( CurrentSet = ThemeColors );
        }

        private void InitTemplate( ThemeSet ColorSet )
        {
            List<ColorItem> Items = new List<ColorItem>();
            foreach ( KeyValuePair<string, string> s in ThemeSet.ParamMap )
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