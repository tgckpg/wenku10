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
using Net.Astropenguin.UI;

using GR.Config;
using GR.Model.Section;
using GR.Model.Text;
using GR.Model.Pages.ContentReader;
using GR.Settings.Theme;

using BgContext = GR.Settings.Layout.BookInfoView.BgContext;

namespace wenku10.Pages.Settings.Themes
{
	public sealed partial class ContentReader : Page
	{
		public static readonly string ID = typeof( ContentReader ).Name;

		public bool NeedRedraw { get; private set; }
		Paragraph[] ExpContent;

		private ReaderView ReaderContext;
		private BgContext RBgContext;

		private Dictionary<string, float> ValidValues = new Dictionary<string, float>();
		private Dictionary<string, Slider> SSliders = new Dictionary<string, Slider>();

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
					, GRConfig.ContentReader.BackgroundColor
				)
				{
					BindAction = ( c ) => {
						GRConfig.ContentReader.BackgroundColor = c;
						UpdateExampleFc();
					}
				}

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_ScrollBar" )
					, GRConfig.ContentReader.ScrollBarColor
				)
				{
					BindAction = ( c ) => {
						GRConfig.ContentReader.ScrollBarColor = c;
						UpdateScrollBar();
					}
				}

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_FontColor" )
					, GRConfig.ContentReader.FontColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.FontColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_TapBrushColor" )
					, GRConfig.ContentReader.TapBrushColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.TapBrushColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_NavBg" )
					, GRConfig.ContentReader.BgColorNav
				) { BindAction = ( c ) => { GRConfig.ContentReader.BgColorNav = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_AssistHelper" )
					, GRConfig.ContentReader.BgColorAssist
				) { BindAction = ( c ) => { GRConfig.ContentReader.BgColorAssist = c; } }
			};

			ClockColorList.ItemsSource = new ColorItem[]
			{
				new ColorItem(
					stx.Text( "Appearance_ContentReader_Clock_ARColor" )
					, GRConfig.ContentReader.Clock.ARColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.Clock.ARColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_Clock_HHColor" )
					, GRConfig.ContentReader.Clock.HHColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.Clock.HHColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_Clock_MHColor" )
					, GRConfig.ContentReader.Clock.MHColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.Clock.MHColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_Clock_SColor" )
					, GRConfig.ContentReader.Clock.SColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.Clock.SColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_EStepper_DColor" )
					, GRConfig.ContentReader.EpStepper.DColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.EpStepper.DColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_EStepper_SColor" )
					, GRConfig.ContentReader.EpStepper.SColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.EpStepper.SColor = c; } }

				, new ColorItem(
					stx.Text( "Appearance_ContentReader_Background" )
					, GRConfig.ContentReader.EpStepper.BackgroundColor
				) { BindAction = ( c ) => { GRConfig.ContentReader.EpStepper.BackgroundColor = c; } }
			};

			BatteryReport Report = Battery.AggregateBattery.GetReport();
			if ( Report.RemainingCapacityInMilliwattHours != null )
			{
				YClock.Progress
					= XClock.Progress
					= ( float ) Report.RemainingCapacityInMilliwattHours / ( float ) Report.FullChargeCapacityInMilliwattHours;
			}

			SSliders[ FontSizeInput.Name ] = FontSizeSlider;
			SSliders[ BlockHeightInput.Name ] = BlockHeightSlider;
			SSliders[ LineSpacingInput.Name ] = LineSpacingSlider;
			SSliders[ ParaSpacingInput.Name ] = ParaSpacingSlider;

			FontSizeSlider.Value = GRConfig.ContentReader.FontSize;
			LineSpacingSlider.Value = GRConfig.ContentReader.LineHeight;
			ParaSpacingSlider.Value = 2 * GRConfig.ContentReader.ParagraphSpacing;

			FontSizeInput.Text = FontSizeSlider.Value.ToString();
			LineSpacingInput.Text = LineSpacingSlider.Value.ToString();
			ParaSpacingInput.Text = ParaSpacingSlider.Value.ToString();

			double BlockHeight = GRConfig.ContentReader.BlockHeight;
			if ( 0 < BlockHeight )
			{
				BlockHeightSlider.Value = BlockHeight;
			}

			FontWeight FWeight = GRConfig.ContentReader.FontWeight;
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
			UpdateExampleBh();

			for ( int i = 0; i < 3; i++ )
			{
				var j = ContentGrid.Dispatcher.RunIdleAsync( ( x ) =>
				{
					UpdateExampleFs();
					UpdateExampleBh();
				} );
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

				BlockHeightSlider.IsEnabled = BlockHeightInput.IsEnabled = false;
			}
		}

		// private void FontSizeSlider_ValueChanged_1( object sender, RangeBaseValueChangedEventArgs e ) { UpdateExampleFs(); }
		private void FontSizeSlider_PointerCaptureLost( object sender, PointerRoutedEventArgs e = null )
		{
			NeedRedraw = true;
			UpdateExampleFs();
			UpdateExampleBh();

			GRConfig.ContentReader.FontSize = FontSizeSlider.Value;
			GRConfig.ContentReader.BlockHeight = 0;
			FontSizeInput.Text = FontSizeSlider.Value.ToString();
		}

		private void BlockHeightSlider_PointerCaptureLost( object sender, PointerRoutedEventArgs e = null )
		{
			NeedRedraw = true;

			VerticalLogaTable LogaTable = VerticalLogaManager.GetLoga( FontSizeSlider.Value );
			LogaTable.Override( BlockHeightSlider.Value );

			double BlockHeight = GRConfig.ContentReader.BlockHeight;
			if ( BlockHeightSlider.Value != BlockHeight )
			{
				VerticalLogaManager.Destroy( BlockHeight );

				GRConfig.ContentReader.BlockHeight = BlockHeightSlider.Value;
				BlockHeightInput.Text = BlockHeightSlider.Value.ToString();

				UpdateExampleFs();
			}
		}

		private void LineSpacingSlider_ValueChanged( object sender, RangeBaseValueChangedEventArgs e ) { UpdateExampleLs(); }
		private void LineSpacingSlider_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			GRConfig.ContentReader.LineHeight = LineSpacingSlider.Value;
			LineSpacingInput.Text = LineSpacingSlider.Value.ToString();
		}
		private void ParaSpacingSlider_ValueChanged( object sender, RangeBaseValueChangedEventArgs e ) { UpdateExamplePs(); }
		private void ParaSpacingSlider_PointerCaptureLost( object sender, PointerRoutedEventArgs e )
		{
			// Setting it will auto scale down half
			GRConfig.ContentReader.ParagraphSpacing = ParaSpacingSlider.Value;
			ParaSpacingInput.Text = ParaSpacingSlider.Value.ToString();
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

		private void UpdateExampleBh()
		{
			VerticalLogaTable LogaTable = VerticalLogaManager.GetLoga( FontSizeSlider.Value );

			if ( LogaTable.CertaintyLevel == 0 )
			{
				var j = Dispatcher.RunIdleAsync( ( x ) => { UpdateExampleBh(); } );
				return;
			}

			BlockHeightSlider.Value = LogaTable.BlockHeight;
			BlockHeightInput.Text = LogaTable.BlockHeight.ToString();

			double qd = Math.Round( 0.25 * FontSizeSlider.Value, 2 );
			double Low = LogaTable.Low;
			double High = LogaTable.High;

			if ( ( High - Low ) < qd )
			{
				Low = LogaTable.BlockHeight - qd;
				High = LogaTable.BlockHeight + qd;
			}

			BlockHeightSlider.Minimum = Low;
			BlockHeightSlider.Maximum = High;
			BlockHeightSlider.StepFrequency = 0.01 * ( High - Low );
		}

		private void UpdateScrollBar()
		{
			if ( HScrollBar == null ) return;
			HScrollBar.Foreground
				= VScrollBar.Foreground
				= new SolidColorBrush( GRConfig.ContentReader.ScrollBarColor );
		}
		private void UpdateExampleLs()
		{
			if ( ExpContent == null ) return;

			ExpContent.All( x => { x.LineHeight = LineSpacingSlider.Value; return true; } );
		}
		private void UpdateExamplePs()
		{
			if ( ExpContent == null ) return;

			double d = Math.Round( ParaSpacingSlider.Value, 2 );
			ExpContent.All( x => { x.SetParagraphSpacing( d ); return true; } );
		}
		private void UpdateExampleFc()
		{
			if ( ExpContent == null ) return;

			SolidColorBrush C = new SolidColorBrush( GRConfig.ContentReader.FontColor );
			ExpContent.All( ( x ) => { x.FontColor = C; return true; } );

			ContentGrid.Background = new SolidColorBrush( GRConfig.ContentReader.BackgroundColor );
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

			GRConfig.ContentReader.FontWeight = Weight;
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
			VScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 2 );
			HScrollBar = ContentGrid.ChildAt<ScrollBar>( 0, 0, 1, 0, 0, 3 );
			UpdateScrollBar();
		}

		private void ForceSliderRange( object sender, TextChangedEventArgs e = null )
		{
			TextBox Input = ( TextBox ) sender;

			string Key = Input.Name;
			Slider SourceSlider = SSliders[ Key ];

			float Value;
			float Min = ( float ) SourceSlider.Minimum;
			float Max = ( float ) SourceSlider.Maximum;

			if ( float.TryParse( Input.Text.Trim(), out Value ) )
			{
				SourceSlider.Value = Value;
				ValidValues[ Key ] = ( float ) SourceSlider.Value;
				return;
			}
			else if ( ValidValues.ContainsKey( Input.Name ) )
			{
				Input.Text = ValidValues[ Key ].ToString();
			}
			else
			{
				Input.Text = SourceSlider.Value.ToString();
			}

			Input.SelectionStart = Input.Text.Length;
		}

		private void BlockHeightInput_LostFocus( object sender, RoutedEventArgs e )
		{
			ForceSliderRange( sender );
			BlockHeightSlider_PointerCaptureLost( sender, null );
		}

		private void FontSizeInput_LostFocus( object sender, RoutedEventArgs e )
		{
			ForceSliderRange( sender );
			FontSizeSlider_PointerCaptureLost( sender, null );
		}

	}
}