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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku8.AdvDM;
using wenku8.Model.ListItem;
using wenku8.Model.REST;
using wenku8.Resources;

namespace wenku10.Pages.Dialogs.Sharers
{
    using RSAManager = wenku8.System.RSAManager;
    using SHTarget = SharersRequest.SHTarget;

    sealed partial class PlaceRequest : ContentDialog
    {
        public bool Canceled { get; private set; }
        private HubScriptItem BindItem;

        private SHTarget Target;
        private RuntimeCache RCache;
        private RSAManager RSA;
        private string RemarksPlaceholder;

        public PlaceRequest( SHTarget Target, HubScriptItem HSI, string Placeholder )
        {
            this.InitializeComponent();

            StringResources stx = new StringResources( "Message" );

            PrimaryButtonText = stx.Str( "OK" );
            SecondaryButtonText = stx.Str( "Cancel" );

            this.Target = Target;

            Canceled = true;
            BindItem = HSI;
            RemarksPlaceholder = Placeholder;

            SetTemplate();
        }

        private async void SetTemplate()
        {
            MarkLoading();

            RCache = new RuntimeCache();
            RemarksInput.PlaceholderText = RemarksPlaceholder;
            RSA = await RSAManager.CreateAsync();
            RSA.PropertyChanged += RSA_PropertyChanged;
            Keys.DataContext = RSA;
            PreSelectKey();

            Keys.IsEnabled = true;
            MarkIdle();
        }

        private void RSA_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == "SelectedItem" ) Keys.SelectedItem = RSA.SelectedItem;
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            if ( Keys.SelectedItem == null )
            {
                StringResources stx = new StringResources();
                ServerMessage.Text = "Please Select a key";
                args.Cancel = true;
                return;
            }

            string PubKey = RSA.SelectedItem.GenPublicKey();
            string Remarks = RemarksInput.Text.Trim();

            if ( string.IsNullOrEmpty( Remarks ) )
            {
                Remarks = RemarksPlaceholder;
            }

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.PlaceRequest( Target, PubKey, BindItem.Id, Remarks )
                , PlaceSuccess
                , ( c, Id, ex ) => { Error( ex.Message ); }
                , false
            );
        }

        private void PlaceSuccess( DRequestCompletedEventArgs e, string Id )
        {
            try
            {
                JsonStatus.Parse( e.ResponseString );
                Canceled = false;
                this.Hide();
            }
            catch( Exception ex )
            {
                Error( ex.Message );
            }
        }


        private void Error( string Mesg )
        {
            Worker.UIInvoke( () =>
            {
                ServerMessage.Text = Mesg;
            } );
        }

        private void PreSelectKey( object sender, RoutedEventArgs e ) { PreSelectKey(); }

        private void PreSelectKey()
        {
            if ( !( RSA == null || Keys == null ) )
                Keys.SelectedItem = RSA.SelectedItem;
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
