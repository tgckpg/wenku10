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

            BindItem = HSI;
            SetTemplate();
        }

        private async void SetTemplate()
        {
            Canceled = true;
            MarkLoading();

            RSA = await RSAManager.CreateAsync();
            RSA.PropertyChanged += RSA_PropertyChanged;
            Keys.DataContext = RSA;
            PreSelectKey();

            Keys.IsEnabled = true;
            MarkIdle();
        }

        private void RSA_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( e.PropertyName == "SelectedItem" ) Keys.SelectedItem = RSA.SelectedItem;
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
        }

        private void PreSelectKey( object sender, RoutedEventArgs e ) { PreSelectKey(); }

        private void PreSelectKey()
        {
            if ( !( RSA == null || Keys == null ) ) Keys.SelectedItem = RSA.SelectedItem;
        }

        private async void AddKey( object sender, RoutedEventArgs e )
        {
            if ( LoadingRing.IsActive ) return;
            MarkLoading();
            await RSA.NewAuthAsync();
            MarkIdle();
        }

        private void MarkLoading()
        {
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
        }

        private void MarkIdle()
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }
}
