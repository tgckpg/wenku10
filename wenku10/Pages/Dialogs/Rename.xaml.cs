using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku8.Model.Interfaces;

namespace wenku10.Pages.Dialogs
{
    public sealed partial class Rename : ContentDialog
    {
        private INamable NamingTarget;

        public bool Canceled = true;

        private Rename()
        {
            this.InitializeComponent();
            StringResources stx = new StringResources( "Message" );
            StringResources stm = new StringResources( "ContextMenu" );
            PrimaryButtonText = stx.Str( "OK" );
            SecondaryButtonText = stx.Str( "Cancel" );

            TitleBlock.Text = stm.Text( "ContextMenu_Rename" );
        }

        public Rename( INamable Target, string Title = null, bool ReadOnly = false )
            : this()
        {
            NamingTarget = Target;
            NewName.Text = Target.Name;
            NewName.IsReadOnly = ReadOnly;

            if ( !string.IsNullOrEmpty( Title ) )
            {
                TitleBlock.Text = Title;
            }
        }

        private async void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
        {
            string NName = NewName.Text.Trim();
            if( string.IsNullOrEmpty( NName ) )
            {
                MessageDialog Msg = new MessageDialog( "Name cannot be empty!" );
                await Popups.ShowDialog( Msg );
                args.Cancel = true;
                NewName.Focus( FocusState.Keyboard );

                return;
            }

            Canceled = false;
            NamingTarget.Name = NName;
        }
    }
}
