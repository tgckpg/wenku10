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

using wenku8.Model;
using wenku8.Model.Text;

namespace wenku10.Pages.Dialogs
{
    sealed partial class EBDictSearch : ContentDialog
    {
        EBDictionary Dict;
        private EBDictSearch()
        {
            this.InitializeComponent();

            StringResources stx = new StringResources( "Message" );
            PrimaryButtonText = stx.Str( "OK" );
        }

        public EBDictSearch( Paragraph P )
            : this()
        {
            ParaText.Text = P.Text;
            P.FontSize = P.FontSize;
            SetTemplate();
        }

        private async void SetTemplate()
        {
            wenku8.System.EBDictManager Manager = new wenku8.System.EBDictManager();

            Dict = await Manager.GetDictionary();
            LayoutRoot.DataContext = Dict;
        }

        DispatcherTimer Longed;
        private void TextSelected( object sender, RoutedEventArgs e )
        {
            CurrentWord.Text = ParaText.SelectedText;
            SearchTermUpdate();
        }

        private void SearchTermUpdate( TextBox sender, TextBoxTextChangingEventArgs args ) { SearchTermUpdate(); }
        private void ParaText_Tapped( object sender, TappedRoutedEventArgs e ) { SearchTermUpdate(); }

        private void SearchTermUpdate()
        {
            if( Longed == null )
            {
                Longed = new DispatcherTimer();
                Longed.Interval = TimeSpan.FromMilliseconds( 800 );
                Longed.Tick += Longed_Tick;
            }

            Longed.Stop();
            Longed.Start();
        }

        private void Longed_Tick( object sender, object e )
        {
            Longed.Stop();
            string text = CurrentWord.Text.Trim();
            if ( Dict == null || string.IsNullOrEmpty( text ) || text == Dict.SearchTerm ) return;

            Dict.SearchTerm = CurrentWord.Text;
        }
    }
}
