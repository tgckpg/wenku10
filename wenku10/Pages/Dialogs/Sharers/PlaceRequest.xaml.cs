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

using wenku8.Model.ListItem;
using RSAManager = wenku8.System.RSAManager;

namespace wenku10.Pages.Dialogs.Sharers
{
    sealed partial class PlaceRequest : ContentDialog
    {
        private HubScriptItem BindItem;

        public bool Canceled { get; private set; }
        public string PubKey { get; private set; }
        public string Remarks { get; private set; }

        private RSAManager RSA;

        public PlaceRequest( HubScriptItem HSI )
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            RSA = new RSAManager();
            Canceled = true;
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
        }

        private void PreSelectKey( object sender, RoutedEventArgs e ) { Keys.SelectedItem = RSA.SelectedItem; }
        private void AddKey( object sender, RoutedEventArgs e ) { RSA.NewAuth(); }
    }
}
