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
	public sealed partial class KeyboardCtrlHelp : ContentDialog
	{
		private KeyboardCtrlHelp()
		{
			this.InitializeComponent();
		}

		public KeyboardCtrlHelp( string Name, Dictionary<string, List<string>> Descs )
			:this()
		{
			StringResources stx = new StringResources( "Resources" );

			TitleText.Text = stx.Str( "Kb_For_" + Name ) + " - " + stx.Str( "KeyboardControls" );

			Dictionary<string, List<string>> LocalizedDesc = new Dictionary<string, List<string>>();
			foreach ( string Key in Descs.Keys )
			{
				string Desc = stx.Str( "Kb_" + Key );
				LocalizedDesc[ string.IsNullOrEmpty( Desc ) ? Key : Desc ] = Descs[ Key ];
			}

			KeyList.ItemsSource = LocalizedDesc;
		}

		private void CloseDialog( object sender, ItemClickEventArgs e ) { Hide(); }
	}
}