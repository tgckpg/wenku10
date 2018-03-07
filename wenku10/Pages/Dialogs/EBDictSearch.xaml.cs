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

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using GR.Effects;
using GR.Model;

using EBDictManager = GR.GSystem.EBDictManager;
using WParagraph = GR.Model.Text.Paragraph;

namespace wenku10.Pages.Dialogs
{
	sealed partial class EBDictSearch : ContentDialog
	{
		private EBDictionary Dict;
		private DispatcherTimer Longed;

		private GR.GSystem.KeyboardController RegKey;

		private int VI = 0;
		private int VJ = 0;

		private bool InSearchBox
		{
			get { return 0 < ( CurrentWord.FocusState & ( FocusState.Keyboard | FocusState.Pointer ) ); }
		}

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
			Closed += EBDictSearch_Closed;

			RegKey = new GR.GSystem.KeyboardController( "SearchWords" );
			RegKey.AddCombo( "Move1stEndToRight", Right1, VirtualKey.L );
			RegKey.AddCombo( "Move1stEndToLeft", Left1, VirtualKey.H );
			RegKey.AddCombo( "Move2ndEndToRight", Right2, VirtualKey.Shift, VirtualKey.L );
			RegKey.AddCombo( "Move2ndEndToLeft", Left1, VirtualKey.Shift, VirtualKey.H );
			RegKey.AddCombo( "Move1stEndToRight", Right1, VirtualKey.Right );
			RegKey.AddCombo( "Move1stEndToLeft", Left1, VirtualKey.Left );
			RegKey.AddCombo( "Move2ndEndToRight", Right2, VirtualKey.Shift, VirtualKey.Right );
			RegKey.AddCombo( "Move2ndEndToLeft", Left2, VirtualKey.Shift, VirtualKey.Left );
			RegKey.AddCombo( "ScrollMore", ScrollMore, VirtualKey.J );
			RegKey.AddCombo( "ScrollMore", ScrollMore, VirtualKey.Down );
			RegKey.AddCombo( "ScrollLess", ScrollLess, VirtualKey.K );
			RegKey.AddCombo( "ScrollLess", ScrollLess, VirtualKey.Up );

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
		}

		private void Right1( KeyCombinationEventArgs e )
		{
			e.Handled = true;
			if ( InSearchBox ) return;
			VJ++; UpdateVisual();
		}

		private void Left1( KeyCombinationEventArgs e )
		{
			e.Handled = true;
			if ( InSearchBox ) return;
			VJ--; UpdateVisual();
		}

		private void Right2( KeyCombinationEventArgs e )
		{
			e.Handled = true;
			if ( InSearchBox ) return;
			VI++; UpdateVisual();
		}

		private void Left2( KeyCombinationEventArgs e )
		{
			e.Handled = true;
			if ( InSearchBox ) return;
			VI--; UpdateVisual();
		}

		private void ScrollMore( KeyCombinationEventArgs e )
		{
			e.Handled = true;
			if ( InSearchBox ) return;
			ScrollViewer SV = Results.ChildAt<ScrollViewer>( 1 );
			SV.ChangeView( null, SV.VerticalOffset + 50, null );
		}

		private void ScrollLess( KeyCombinationEventArgs e )
		{
			e.Handled = true;
			if ( InSearchBox ) return;
			ScrollViewer SV = Results.ChildAt<ScrollViewer>( 1 );
			SV.ChangeView( null, SV.VerticalOffset - 50, null );
		}

		private void EBDictSearch_Closed( ContentDialog sender, ContentDialogClosedEventArgs args )
		{
			RegKey.Dispose();
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
			if ( Longed == null )
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
				, P =>
				{
					( ( Settings.MainSettings ) P ).PopupSettings( typeof( Settings.Data.EBWin ) );
				}
			);
		}

	}
}