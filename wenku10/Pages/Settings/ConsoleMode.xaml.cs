using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace wenku10.Pages.Settings
{
	public sealed partial class ConsoleMode : Page
	{
		private bool UserConfirmed;
		private UIElement CurrentElem;

		private Action<bool> PendingConfirm;

		public ConsoleMode()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			PS1.Text = "";
			ResponseHelp( "usage-warning" );
		}

		private void CommandInput_KeyDown( object sender, KeyRoutedEventArgs e )
		{
			if ( e.Key == Windows.System.VirtualKey.Enter )
			{
				string Cmd = CommandInput.Text.Trim();
				CommandInput.Text = "";

				if ( !string.IsNullOrEmpty( Cmd ) )
				{
					if ( !UserConfirmed )
					{
						if ( UserUnderstandTheRisk( Cmd ) )
						{
							ResponseHelp( "welcome-message" );
						}
						return;
					}

					DisplayCommand( Cmd );
					if ( ConfirmCommand( Cmd ) || Cmd[ 0 ] == '#' ) return;
					ProcessCommand( Cmd );
				}
			}
		}

		private bool UserUnderstandTheRisk( string cmd )
		{
			if( cmd.ToLower() == "continue" )
			{
				UserConfirmed = true;
				return true;
			}

			GR.Config.Properties.CONSOLE_MODE = false;

			ResponseCommand( "User did not confirmed the risks. Quitting ..." );
			CommandInput.IsEnabled = false;
			DelayedQuit();
			return false;
		}

		private bool ConfirmCommand( string Cmd )
		{
			if ( PendingConfirm == null )
				return false;

			PendingConfirm( Cmd.Trim().ToLower() == "y" );
			PendingConfirm = null;
			return true;
		}

		private void ProcessCommand( string Line )
		{
			if( !string.IsNullOrEmpty( OpenedDb ) )
			{
				ExecQuery( Line );
				return;
			}

			string iCommand = Line.ToLower();

			NextSeg( ref iCommand, out string Cmd );

			switch ( Cmd )
			{
				case "help": HelpCommand( Line ); break;
				case "show": ResponseHelp( "show" ); break;
				case "database": DatabaseCommand( Line ); break;

				case "ls": case "cd": case "pwd":
				case "cat": case "wc": case "mkdir":
				case "mv": case "rm": case "touch":
				case "uuidgen": case "date":
				case "export": case "import":
					CoreCommand( Line );
					break;

				case "coreutils":
					ResponseCommand( "coreutils is a group of commands. Enter \"help coreutils\" to see the available commands" );
					break;

				case "clear":
				case "reset":
					CurrentElem = null;
					OutputPanel.Children.Clear();
					break;

				case "exit":
					ResponseCommand( "Exiting..." );
					CommandInput.IsEnabled = false;
					var j = Dispatcher.RunIdleAsync( QuitApp );
					break;

				default:
					ResponseCommand( $"No such command: {Line}" );
					break;
			}
		}

		private void HelpCommand( string Command )
		{
			Command = Command.ToLower();
			NextSeg( ref Command, out string Section );
			NextSeg( ref Command, out Section );

			switch ( Section )
			{
				case "":
					ResponseHelp( "help" );
					break;
				case "coreutils":
				case "reset":
				case "database":
					ResponseHelp( "help/" + Section );
					break;
				default:
					ResponseCommand( "help: Section not found: " + Section );
					break;
			}
		}

		private async void DelayedQuit()
		{
			await Task.Delay( 2000 );
			await GR.GSystem.Utils.RestartOrExit();
		}

		private void QuitApp( IdleDispatchedHandlerArgs e )
		{
			Application.Current.Exit();
		}

		private void DisplayCommand( string Command ) => ResponseCommand( PS1.Text + CMode.Text + Command, "" );
		private void ResponseCommand( string Command, string End = "\n" )
		{
			if ( CurrentElem is TextBlock tb )
			{
				tb.Text += "\n" + Command + End;
			}
			else
			{
				AddElement( CommandTextBlock( Command + End ) );
			}
		}

		private void AddElement( UIElement Elem )
		{
			OutputPanel.Children.Add( Elem );
			CurrentElem = Elem;
		}

		private void OutputPanel_SizeChanged( object sender, SizeChangedEventArgs e )
		{
			OutputSV.ChangeView( null, OutputSV.ScrollableHeight, null );
		}

		private void ResponseHelp( string ManFile )
		{
			ResponseCommand( File.ReadAllText( $"Strings/man/{ManFile}.txt" ) );
		}

		private bool NextSeg( ref string s, out string seg )
		{
			if( string.IsNullOrEmpty( s ) )
			{
				seg = s;
				return false;
			}

			bool escDouble = false, escSingle = false, escNext = false;

			seg = "";
			int i = 0, l = s.Length;
			for ( ; i < l; i++ )
			{
				char k = s[ i ];
				if ( escNext )
				{
					seg += k;
					escNext = false;
					continue;
				}
				else if ( k == '\\' )
				{
					escNext = true;
					continue;
				}
				else if ( k == '"' && !escSingle )
				{
					escDouble = !escDouble;
					continue;
				}
				else if ( k == '\'' && !escDouble )
				{
					escSingle = !escSingle;
					continue;
				}

				if ( escSingle || escDouble )
				{
					seg += k;
				}
				else
				{
					if ( k == ' ' )
					{
						s = s.Substring( i + 1 );
						return true;
					}
					else
					{
						seg += k;
					}
				}
			}

			if ( 0 < i )
			{
				if ( i == l )
				{
					s = "";
				}
				else
				{
					s = s.Substring( i );
				}
				return true;
			}

			return false;
		}

		private TextBlock CommandTextBlock( string Text ) => new TextBlock()
		{
			FontFamily = CommandInput.FontFamily,
			Foreground = CommandInput.Foreground,
			FontSize = CommandInput.FontSize,
			TextWrapping = CommandInput.TextWrapping,
			IsTextSelectionEnabled = true,
			Text = Text
		};

	}
}