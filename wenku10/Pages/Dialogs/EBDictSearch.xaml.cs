using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Loaders;

using wenku8.Effects;
using wenku8.Model;

using EBDictManager = wenku8.System.EBDictManager;
using WParagraph = wenku8.Model.Text.Paragraph;

namespace wenku10.Pages.Dialogs
{
    sealed partial class EBDictSearch : ContentDialog
    {
        private EBDictionary Dict;
        private DispatcherTimer Longed;

        private List<Action> RegKey;

        private int VI = 0;
        private int VJ = 0;

        private EBDictSearch()
        {
            this.InitializeComponent();

            StringResources stx = new StringResources( "Message" );
            PrimaryButtonText = stx.Str( "OK" );
        }

        public EBDictSearch( WParagraph P )
            : this()
        {
            ParaText.Text = P.Text;
            ParaText.FontSize = P.FontSize;
            SetTemplate();
        }

        private async void SetTemplate()
        {
            EBDictManager Manager = new EBDictManager();

            Dict = await Manager.GetDictionary();
            LayoutRoot.DataContext = Dict;

            MaskLoading.IsActive = false;
            TransitionDisplay.SetState( Mask, TransitionState.Inactive );

            if ( string.IsNullOrEmpty( ParaText.Text ) )
            {
                StringResources stx = new StringResources();
                CurrentWord.PlaceholderText = stx.Text( "Desc_InputKey" );
            }

            RegKey = new List<Action>();
            // KeyBoard Navigations
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VJ++; UpdateVisual(); }, VirtualKey.L ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VJ--; UpdateVisual(); }, VirtualKey.H ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VI++; UpdateVisual(); }, VirtualKey.Shift, VirtualKey.L ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VI--; UpdateVisual(); }, VirtualKey.Shift, VirtualKey.H ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VJ++; UpdateVisual(); }, VirtualKey.Right ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VJ--; UpdateVisual(); }, VirtualKey.Left ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VI++; UpdateVisual(); }, VirtualKey.Shift, VirtualKey.Right ) );
            RegKey.Add( App.KeyboardControl.RegisterCombination( e => { e.Handled = true; VI--; UpdateVisual(); }, VirtualKey.Shift, VirtualKey.Left ) );

            Closed += EBDictSearch_Closed;
        }

        private void EBDictSearch_Closed( ContentDialog sender, ContentDialogClosedEventArgs args )
        {
            foreach ( Action p in RegKey ) p();
        }

        private void UpdateVisual()
        {
            int l = ParaText.Text.Length;
            if ( VI < 0 ) VI = 0;
            if ( VJ < 0 ) VJ = 0;
            if ( l < VI ) VI = l;
            if ( l < VJ ) VJ = l;

            if ( VI < VJ )
            {
                ParaText.SelectionStart = VI;
                ParaText.SelectionLength = VJ - VI;
            }
            else
            {
                ParaText.SelectionStart = VJ;
                ParaText.SelectionLength = VI - VJ;
            }
        }

        private void TextSelected( object sender, RoutedEventArgs e )
        {
            CurrentWord.Text = ParaText.SelectedText;
            SearchTermUpdate();
        }

        private void ManualSearchTerm( TextBox sender, TextBoxTextChangingEventArgs args ) { SearchTermUpdate(); }

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

        private void GoInstallDictionary( Hyperlink sender, HyperlinkClickEventArgs args )
        {
            this.Hide();
            ControlFrame.Instance.NavigateTo(
                PageId.MAIN_SETTINGS
                , () => new Settings.MainSettings()
                , P => {
                    ( ( Settings.MainSettings ) P ).PopupSettings( typeof( Settings.Data.EBWin ) );
                }
            );
        }

    }
}