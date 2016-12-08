using Net.Astropenguin.Loaders;
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

namespace wenku10.Pages.Dialogs
{
    public sealed partial class ValueHelpInput : ContentDialog
    {
        public bool Canceled { get; private set; }

        private string _Value;
        public string Value
        {
            get { return _Value; }
            set
            {
                _Value = value;
                ValueInput.Text = value;
            }
        }

        public bool AllowEmpty { get; set; }

        public Action<HyperlinkButton,RoutedEventArgs> HelpBtnClick;

        public ValueHelpInput(
            string DefaultValue
            , string Title
            , string ValueLabel, string HelpText
            , string BtnLeft = "OK", string BtnRight = "Cancel" )
        {
            this.InitializeComponent();

            Canceled = true;

            if ( !string.IsNullOrEmpty( DefaultValue ) )
                ValueInput.PlaceholderText = DefaultValue;

            StringResources stx = new StringResources( "Message" );
            PrimaryButtonText = stx.Str( BtnLeft );
            SecondaryButtonText = stx.Str( BtnRight );

            TitleText.Text = Title;

            if ( !string.IsNullOrEmpty( ValueLabel ) )
                ValueLbl.Text = ValueLabel;

            if ( string.IsNullOrEmpty( HelpText ) )
            {
                HelpBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                HelpLbl.Text = HelpText;
            }
        }

        private void HelpBtn_Click( object sender, RoutedEventArgs e )
        {
            HelpBtnClick?.Invoke( HelpBtn, e );
        }

        private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            DetectInput();
        }

        private void OnKeyDown( object sender, KeyRoutedEventArgs e )
        {
            if ( e.Key == Windows.System.VirtualKey.Enter )
            {
                e.Handled = true;
                DetectInput();
            }
        }

        private void DetectInput()
        {
            string Value = ValueInput.Text;

            if ( string.IsNullOrEmpty( Value ) )
                Value = ValueInput.PlaceholderText;

            if ( string.IsNullOrEmpty( Value ) && !AllowEmpty )
            {
                Value = "";
                ValueInput.Focus( FocusState.Keyboard );
                return;
            }
            else
            {
                IsPrimaryButtonEnabled
                    = IsSecondaryButtonEnabled
                    = ValueInput.IsEnabled
                    = false
                    ;

                if ( this.Value == Value )
                {
                    this.Hide();
                    return;
                }

                this.Value = Value;

                this.Canceled = false;
                this.Hide();
            }
        }

    }
}