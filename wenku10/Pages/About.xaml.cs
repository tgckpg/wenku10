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

using wenku8.Model.Interfaces;

namespace wenku10.Pages
{
    using Scenes;
    public sealed partial class About : Page, ICmdControls
    {
        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get { return true; } }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get ; private set; }

        private CanvasStage CStage;

        public About()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            CStage = new CanvasStage( Stage );
            CStage.Add( new Fireworks() );

            Unloaded += About_Unloaded;
        }

        private void About_Unloaded( object sender, RoutedEventArgs e )
        {
            Stage.RemoveFromVisualTree();
            Stage = null;

            CStage.Dispose();
            CStage = null;
        }

    }
}