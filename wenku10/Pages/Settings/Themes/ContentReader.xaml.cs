using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Power;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Text;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.Config;
using wenku8.Model.Section;
using wenku8.Model.Text;
using wenku8.Model.Pages.ContentReader;
using wenku8.Settings.Theme;

using BgContext = wenku8.Settings.Layout.BookInfoView.BgContext;

namespace wenku10.Pages.Settings.Themes
{
    public sealed partial class ContentReader : Page
    {
        public static readonly string ID = typeof( ContentReader ).Name;

        public bool NeedRedraw { get; private set; }
        Paragraph[] ExpContent;

        private ReaderView ReaderContext;
        private BgContext RBgContext;

        ScrollBar HScrollBar;
        ScrollBar VScrollBar;

        public ContentReader()
        {
            NeedRedraw = false;
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            ReaderContext = new ReaderView();
            ContentGrid.DataContext = ReaderContext;
            ContextGrid.DataContext = new ESContext();

            RBgContext = ReaderContext.Settings.GetBgContext();
            ContentBg.DataContext = RBgContext;

            RBgContext.ApplyBackgrounds();

            StringResources stx = new StringResources( "Settings" );

            ExpContent = new Paragraph[]
            {
                new Paragraph( stx.Text( "Appearance_ContentReader_Exp1") )
                , new Paragraph( stx.Text( "Appearance_ContentReader_Exp2") )
                , new Paragraph( stx.Text( "Appearance_ContentReader_Exp3") )
                , new Paragraph( stx.Text( "Appearance_ContentReader_Exp4") )
            };

            XTitleStepper.Text = YTitleStepper.Text = stx.Text( "Appearance_ContentReader_Exp2" );

            ColorList.ItemsSource = new ColorItem[]
            {
                new ColorItem(
                    stx.Text( "Appearance_ContentReader_Background" )
                    , Properties.APPEARANCE_CONTENTREADER_BACKGROUND
                )
                {
                    BindAction = ( c ) => {
                        Properties.APPEARANCE_CONTENTREADER_BACKGROUND = c;
                        UpdateExampleFc();
                    }
                }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_ScrollBar" )
                    , Properties.APPEARANCE_CONTENTREADER_SCROLLBAR
                )
                {
                    BindAction = ( c ) => {
                        Properties.APPEARANCE_CONTENTREADER_SCROLLBAR = c;
                        UpdateScrollBar();
                    }
                }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_FontColor" )
                    , Properties.APPEARANCE_CONTENTREADER_FONTCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_FONTCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_TapBrushColor" )
                    , Properties.APPEARANCE_CONTENTREADER_TAPBRUSHCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_TAPBRUSHCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_NavBg" )
                    , Properties.APPEARANCE_CONTENTREADER_NAVBG
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_NAVBG = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_AssistHelper" )
                    , Properties.APPEARANCE_CONTENTREADER_ASSISTBG
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_ASSISTBG = c; } }
            };

            ClockColorList.ItemsSource = new ColorItem[]
            {
                new ColorItem(
                    stx.Text( "Appearance_ContentReader_Clock_ARColor" )
                    , Properties.APPEARANCE_CONTENTREADER_CLOCK_ARCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_CLOCK_ARCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_Clock_HHColor" )
                    , Properties.APPEARANCE_CONTENTREADER_CLOCK_HHCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_CLOCK_HHCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_Clock_MHColor" )
                    , Properties.APPEARANCE_CONTENTREADER_CLOCK_MHCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_CLOCK_MHCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_Clock_SColor" )
                    , Properties.APPEARANCE_CONTENTREADER_CLOCK_SCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_CLOCK_SCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_EStepper_DColor" )
                    , Properties.APPEARANCE_CONTENTREADER_ES_DCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_ES_DCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_EStepper_SColor" )
                    , Properties.APPEARANCE_CONTENTREADER_ES_SCOLOR
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_ES_SCOLOR = c; } }

                , new ColorItem(
                    stx.Text( "Appearance_ContentReader_Background" )
                    , Properties.APPEARANCE_CONTENTREADER_ES_BG
                ) { BindAction = ( c ) => { Properties.APPEARANCE_CONTENTREADER_ES_BG = c; } }
            };

            BatteryReport Report = Battery.AggregateBattery.GetReport();
            if ( Report.RemainingCapacityInMilliwattHours != null )
            {
                YClock.Progress
                    = XClock.Progress
                    = ( float ) Report.RemainingCapacityInMilliwattHours / ( float ) Report.FullChargeCapacityInMilliwattHours;
            }

            FontSizeSlider.Value = Properties.APPEARANCE_CONTENTREADER_FONTSIZE;
            LineSpacingSlider.Value = Properties.APPEARANCE_CONTENTREADER_LINEHEIGHT;
            ParagraphSpacingSlider.Value = 2 * Properties.APPEARANCE_CONTENTREADER_PARAGRAPHSPACING;

            FontWeight FWeight = Properties.APPEARANCE_CONTENTREADER_FONTWEIGHT;
            // Set font weights
            Dictionary<string, FontWeight> ForReflection = new Dictionary<string, FontWeight>()
            {
                { "Thin", FontWeights.Thin }
                , { "ExtraLight", FontWeights.ExtraLight }
                , { "Light", FontWeights.Light }
                , { "SemiLight", FontWeights.SemiLight }
                , { "Normal", FontWeights.Normal }
                , { "Medium", FontWeights.Medium }
                , { "SemiBold", FontWeights.SemiBold }
                , { "Bold", FontWeights.Bold }
                , { "ExtraBold", FontWeights.ExtraBold }
                , { "Black", FontWeights.Black }
                , { "ExtraBlack", FontWeights.ExtraBlack }
            };

            Logger.Log( ID, "Default FontWeight is " + FWeight.Weight );
            List<PInfoWrapper> PIW = new List<PInfoWrapper>();
            foreach ( KeyValuePair<string, FontWeight> K in ForReflection )
            {
                PInfoWrapper PWrapper = new PInfoWrapper( K.Key, K.Value );
                PIW.Add( PWrapper );
            }
            FontWeightCB.ItemsSource = PIW;

            FontWeightCB.SelectedItem = PIW.First( x => x.Weight.Weight == FWeight.Weight );

            YClock.Time = XClock.Time = DateTime.Now;
            XDayofWeek.Text = YDayofWeek.Text = YClock.Time.ToString( "dddd" );
            XDayofMonth.Text = YDayofMonth.Text = YClock.Time.Day.ToString();
            XMonth.Text = YMonth.Text = YClock.Time.ToString( "MMMM" );

            SetLayoutAware();

            UpdateExampleLs();
            UpdateExamplePs();
            UpdateExampleFc();
            UpdateExampleFs();
            for ( int i = 0; i < 3; i++ )
            {
                var j = ContentGrid.Dispatcher.RunIdleAsync( ( x ) => UpdateExampleFs() );
            }
        }

        private void SetLayoutAware()
        {
            if ( ReaderContext.Settings.IsHorizontal )
            {
                Grid.SetRowSpan( ContentGrid, 1 );
                Grid.SetColumnSpan( ContentGrid, 2 );

                XPanel.Visibility = Visibility.Collapsed;
                YPanel.Visibility = Visibility.Visible;
            }
            else
            {
                Grid.SetRowSpan( ContentGrid, 2 );
                Grid.SetColumnSpan( ContentGrid, 1 );

                XPanel.Visibility = Visibility.Visible;
                YPanel.Visibility = Visibility.Collapsed;
            }
        }

        // private void FontSizeSlider_ValueChanged_1( object sender, RangeBaseValueChangedEventArgs e ) { UpdateExampleFs(); }
        private void FontSizeSlider_PointerCaptureLost_1( object sender, PointerRoutedEventArgs e )
        {
            NeedRedraw = true;
            UpdateExampleFs();
            Properties.APPEARANCE_CONTENTREADER_FONTSIZE = FontSizeSlider.Value;
        }
        private void LineSpacingSlider_ValueChanged_1( object sender, RangeBaseValueChangedEventArgs e ) { UpdateExampleLs(); }
        private void LineSpacingSlider_PointerCaptureLost_1( object sender, PointerRoutedEventArgs e )
        {
            Properties.APPEARANCE_CONTENTREADER_LINEHEIGHT = LineSpacingSlider.Value;
        }
        private void ParagraphSpacingSlider_ValueChanged_1( object sender, RangeBaseValueChangedEventArgs e ) { UpdateExamplePs(); }
        private void ParagraphSpacingSlider_PointerCaptureLost_1( object sender, PointerRoutedEventArgs e )
        {
            // Setting it will auto scale down half
            Properties.APPEARANCE_CONTENTREADER_PARAGRAPHSPACING = ParagraphSpacingSlider.Value ;
        }

        // Status Update
        private void UpdateExampleFs()
        {
            if ( ExpContent == null ) return;

            double d = Math.Round( FontSizeSlider.Value, 2 );
            ExpContent[ 0 ].FontSize = d;

            UpdateExampleLs();
            ContentGrid.ItemsSource = null;
            ContentGrid.ItemsSource = ExpContent;
        }
        private void UpdateScrollBar()
        {
            if ( HScrollBar == null ) return;
            HScrollBar.Foreground
                = VScrollBar.Foreground
                = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_SCROLLBAR );
        }
        private void UpdateExampleLs()
        {
            if ( ExpContent == null ) return;

            ExpContent.All( x => { x.LineHeight = LineSpacingSlider.Value; return true; } );
        }
        private void UpdateExamplePs()
        {
            if ( ExpContent == null ) return;

            double d = Math.Round( ParagraphSpacingSlider.Value, 2 );
            ExpContent.All( x => { x.SetParagraphSpacing( d ); return true; } );
        }
        private void UpdateExampleFc()
        {
            if ( ExpContent == null ) return;

            SolidColorBrush C = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_FONTCOLOR );
            ExpContent.All( ( x ) => { x.FontColor = C; return true; } );

            ContentGrid.Background = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_BACKGROUND );
        }

        private async void ColorList_ItemClick( object sender, ItemClickEventArgs e )
        {
            ColorItem C = e.ClickedItem as ColorItem;
            Dialogs.ColorPicker Picker = new Dialogs.ColorPicker( C );
            await Popups.ShowDialog( Picker );

            if ( Picker.Canceled ) return;

            C.ChangeColor( Picker.UserChoice );
        }

        private void FontWeightCB_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count < 1 ) return;
            FontWeight Weight = ( e.AddedItems[ 0 ] as PInfoWrapper ).Weight;
            ExpContent.All( ( x ) => { x.FontWeight = Weight; return true; } );

            Properties.APPEARANCE_CONTENTREADER_FONTWEIGHT = Weight;
        }

        private class PInfoWrapper
        {
            public FontWeight Weight { get; private set; }

            public PInfoWrapper( string Name, FontWeight F )
            {
                Weight = F;
                this.Name = Name;
            }

            public string Name { get; private set; }
        }

        private void BgChoice_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count < 1 ) return;
            RBgContext.SetBackground( ( ( ComboBoxItem ) e.AddedItems.First() ).Tag.ToString() );
        }

        private void BgChoice_Loaded( object sender, RoutedEventArgs e )
        {
            if ( ReaderContext == null ) return;

            BgChoice.SelectedIndex = Array.IndexOf(
                new string[] { "None", "Custom", "Preset", "System" }, RBgContext.BgType
            );
        }

        private void ContentGrid_Loaded( object sender, RoutedEventArgs e )
        {
            VScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 1 );
            HScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 2 );
            UpdateScrollBar();
        }

    }
}