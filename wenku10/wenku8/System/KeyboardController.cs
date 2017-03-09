using Net.Astropenguin.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

using Net.Astropenguin.Helpers;

using wenku10;
using wenku10.Pages.Dialogs;
using Net.Astropenguin.IO;
using wenku8.Settings;

namespace wenku8.System
{
	class KeyboardController : IDisposable
	{
		private List<Action> RegKeys;
		private Dictionary<string, List<string>> KeyDesc;

		public string Name { get; private set; }

		private KeyboardCtrlHelp HelpDialog;
		private XRegistry XReg;
		private XParameter Settings;

		public KeyboardController( string Name )
		{
			this.Name = Name;

			RegKeys = new List<Action>();
			KeyDesc = new Dictionary<string, List<string>>();

			XReg = new XRegistry( "<help />", FileLinks.ROOT_SETTING + FileLinks.HELP );

			Settings = XReg.Parameter( "Keyboard" );
			if ( Settings == null ) Settings = new XParameter( "Keyboard" );

			AddCombo( "Help", ShowHelp, VirtualKey.F1 );
			AddCombo( "Help", ShowHelp, VirtualKey.Shift, ( VirtualKey ) 191 );
		}

		public void ShowHelp()
		{
			if ( MainStage.Instance.IsPhone || Settings.GetBool( Name ) )
				return;
			Settings.SetValue( new XKey( Name, true ) );
			XReg.SetParameter( Settings );
			XReg.Save();

			PopupHelp();
		}

		private void ShowHelp( KeyCombinationEventArgs e )
		{
			e.Handled = true;
			PopupHelp();
		}

		private async void PopupHelp()
		{
			if( HelpDialog != null )
			{
				HelpDialog.Hide();
				HelpDialog = null;
				return;
			}

			HelpDialog = new KeyboardCtrlHelp( Name, KeyDesc );
			await Popups.ShowDialog( HelpDialog );
			HelpDialog = null;
		}

		public void Dispose()
		{
			foreach ( Action p in RegKeys ) p();
		}

		public void AddCombo( string Desc, Action<KeyCombinationEventArgs> P, params VirtualKey[] Combinations )
		{
			RegKeys.Add( App.KeyboardControl.RegisterCombination( P, Combinations ) );

			if ( !KeyDesc.ContainsKey( Desc ) ) KeyDesc[ Desc ] = new List<string>();
			KeyDesc[ Desc ].Add( HumanReadable( string.Join( " + ", Combinations ) ) );
		}

		public void AddSeq( string Desc, Action<KeyCombinationEventArgs> P, params VirtualKey[] Seq )
		{

			RegKeys.Add( App.KeyboardControl.RegisterSequence( P, Seq ) );

			if ( !KeyDesc.ContainsKey( Desc ) ) KeyDesc[ Desc ] = new List<string>();
			KeyDesc[ Desc ].Add( HumanReadable( string.Join( "", Seq ) ) );
		}

		private string HumanReadable( string Str )
		{
			return Str.Replace( "192", "`" ).Replace( "186", ";" ).Replace( "191", "/" );
		}

	}
}