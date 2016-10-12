using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace wenku10.Pages
{
    public sealed partial class About : Page
    {
        private Scenes.Fireworks Fireworks;

        public About()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            Fireworks = new Scenes.Fireworks( Stage );
            Fireworks.Start();

            Unloaded += About_Unloaded;
        }

        private void About_Unloaded( object sender, RoutedEventArgs e )
        {
            Stage.RemoveFromVisualTree();
            Stage = null;

            Fireworks.Dispose();
            Fireworks = null;
        }
    }
}