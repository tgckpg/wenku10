using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using GR.Settings.Theme;

namespace wenku10.Pages.Dialogs
{
	sealed partial class ColorPicker : ContentDialog
	{
		private ColorPickerSection SectionData;

		public bool Canceled { get; private set; }
		public Windows.UI.Color UserChoice { get; private set; }

		public ColorPicker( ColorItem BindColor )
			: base()
		{
			Canceled = true;
			this.InitializeComponent();
			SetTemplate( BindColor );
		}

		private void SetTemplate( ColorItem BindColor )
		{
			StringResources stx = StringResources.Load( "Message" );

			PrimaryButtonText = stx.Str( "OK" );
			SecondaryButtonText = stx.Str( "Cancel" );
			// PresetColors
			PresetColors.ItemsSource = global::GR.GSystem.ThemeManager.PresetColors();

			UpdateColor( BindColor );
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			Canceled = false;
			UserChoice = SectionData.CColor.TColor;
		}

		private void VariableGridView_ItemClick( object sender, ItemClickEventArgs e )
		{
			Palette P = e.ClickedItem as Palette;
			IList<Palette> Palettes = ( IList<Palette> ) SectionData.Palettes;

			// If it is the current color, do nothing
			if ( Palettes.IndexOf( P ) < 1 ) return;
			UpdateColor( P );
		}

		private bool PresetAutoUpdate = false;
		private void UpdateColor( ColorItem C )
		{
			IList<ColorItem> Presets = ( IList<ColorItem> ) PresetColors.ItemsSource;
			try
			{
				ColorItem PreSelected = PresetColors.SelectedItem as ColorItem;
				ColorItem Selected = Presets.First(
					( C1 ) =>
					{
						return C1.R == C.R
							&&  C1.G == C.G
							&&  C1.B == C.B
						;
					}
				);
				if ( PreSelected != Selected )
				{
					PresetAutoUpdate = true;
					PresetColors.SelectedItem = Selected;
				}
			}
			catch ( Exception )
			{
				PresetColors.SelectedItem = null;
			}

			SectionData = new ColorPickerSection( new ColorItem( C.ColorTag, C.TColor ) );
			MainView.DataContext = SectionData;
		}

		private void PresetColors_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( PresetAutoUpdate )
			{
				PresetAutoUpdate = false;
				return;
			}

			if ( e.AddedItems.Count() < 1 ) return;
			UpdateColor( e.AddedItems[0] as ColorItem );
		}

		private async void HexInput( object sender, RoutedEventArgs e )
		{
			TextBox InputBox = sender as TextBox;
			string Hex = InputBox.Text.Trim();
			Regex HexMatch = new Regex( "^#[\\dA-Fa-f]{8}$" );
			if ( HexMatch.IsMatch( Hex ) )
			{
				if( Hex != SectionData.CColor.Hex )
				{
					UpdateColor( new ColorItem( "Hex", global::GR.GSystem.ThemeManager.StringColor( Hex ) ) );
				}
			}
			else
			{
				await InvalidFormat();
				InputBox.Text = SectionData.CColor.Hex;
				InputBox.Focus( FocusState.Keyboard );
			}
		}

		private async void NumericInput( object sender, RoutedEventArgs e )
		{
			TextBox InputBox = sender as TextBox;
			string Input = InputBox.Text.Trim();

			string[] Param = InputBox.Tag.ToString().Split( ',' );
			string PropName = Param[ 0 ];
			int Max = int.Parse( Param[ 1 ] );

			bool Pass = true;

			// Empty String
			if( Pass && string.IsNullOrEmpty( Input ) )
			{
				Pass = false;
			}

			// Not a number
			if ( Pass && !global::GR.GSystem.Utils.Numberstring( Input ) )
			{
				Pass = false;
				await InvalidFormat();
			}

			int Value = 0;
			int.TryParse( Input, out Value );

			// Out of range
			if ( Pass && ( Value < 0 || Max < Value ) )
			{
				Pass = false;
				await OutOfRange( Value, 0, Max );
			}

			Type p = typeof( ColorItem );
			PropertyInfo PInfo = p.GetProperty( PropName );
			int OValue = ( int ) PInfo.GetValue( SectionData.CColor );

			if ( !Pass )
			{
				InputBox.Text = OValue.ToString();
				InputBox.Focus( FocusState.Keyboard );
				return;
			}

			// Unchanged
			if ( Value == OValue ) return;

			// All tests passed
			ColorItem C = SectionData.CColor;
			PInfo.SetValue( C, Value );

			UpdateColor( C );
		}

		private async Task InvalidFormat()
		{
			MessageDialog Msg = new MessageDialog( "Invalid Format" );
			await Popups.ShowDialog( Msg );
		}

		private async Task OutOfRange( int v, int min, int max )
		{
			MessageDialog Msg = new MessageDialog(
				string.Format(
					"Value \"{0}\" is out of range {1} < x < {2}."
					, v, min, max
				)
			);
			await Popups.ShowDialog( Msg );
		}
	}
}
