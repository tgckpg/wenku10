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

using Net.Astropenguin.Loaders;

using wenku8.Model.ListItem;

namespace wenku10.Pages.Dialogs
{
    sealed partial class NameValueInput : ContentDialog
    {
        public bool Canceled { get; private set; }
        private NameValue<string> Target;

        public NameValueInput( NameValue<string> Item
            , string Title
            , string NameLabel, string ValueLabel
            , string BtnLeft = "OK", string BtnRight = "Cancel" )
        {
            this.InitializeComponent();

            Canceled = true;
            Target = Item;

            StringResources stx = new StringResources( "Message" );
            PrimaryButtonText = stx.Str( BtnLeft );
            SecondaryButtonText = stx.Str( BtnRight );

            this.Title = Title;
            NameLbl.Text = NameLabel;
            ValueLbl.Text = ValueLabel;
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            DetectInput();
        }

        private void OnKeyDown( object sender, KeyRoutedEventArgs e )
        {
            if ( e.Key == Windows.System.VirtualKey.Enter )
            {
                DetectInput();
            }
        }

        private void DetectInput()
        {
            string Name = NameInput.Text.Trim();
            string Value = ValueInput.Text;

            if ( string.IsNullOrEmpty( Name ) || string.IsNullOrEmpty( Value ) )
            {
                if ( string.IsNullOrEmpty( Name ) )
                {
                    NameInput.Focus( FocusState.Keyboard );
                }
                else
                {
                    ValueInput.Focus( FocusState.Keyboard );
                }
                return;
            }
            else
            {
                IsPrimaryButtonEnabled
                    = IsSecondaryButtonEnabled
                    = NameInput.IsEnabled
                    = ValueInput.IsEnabled
                    = false
                    ;

                Target.Name = Name;
                Target.Value = Value;

                this.Canceled = false;
                this.Hide();
            }
        }

    }
}