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

using wenku8.Config;

namespace wenku10.Pages.Settings.Advanced
{
    public sealed partial class Misc : Page
    {
        public Misc()
        {
            this.InitializeComponent();
            SyntaxPatchToggle.IsOn = Properties.MISC_TEXT_PATCH_SYNTAX;
        }

        private void ToggleSynPatch( object sender, RoutedEventArgs e )
        {
            wenku8.Model.Text.Manipulation.DoSyntaxPatch = SyntaxPatchToggle.IsOn;
            Properties.MISC_TEXT_PATCH_SYNTAX = SyntaxPatchToggle.IsOn;
        }
    }
}
