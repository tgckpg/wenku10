using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace wenku10.ShHub
{
    public sealed partial class ScriptUpload : Page
    {
        public ScriptUpload()
        {
            this.InitializeComponent();
        }

        private void Anon_Checked( object sender, RoutedEventArgs e )
        {
            AnonWarning.Visibility = Anon.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
