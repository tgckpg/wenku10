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

using GR.Database.Contexts;
using GR.Database.DirectSQL;

namespace wenku10.Pages.Settings
{
	public sealed partial class ConsoleMode : Page
	{
		private string OpenedDb;
		private bool UserConfirmed;

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
							ResponseCommand( "User confirmed. Enter \"help\" to see help, \"show\" to list available commands." );
						}
						return;
					}

					DisplayCommand( Cmd );
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

			ResponseCommand( "User did not confirmed the risks. Quitting ..." );
			CommandInput.IsEnabled = false;
			DelayedQuit();
			return false;
		}

		private void ProcessCommand( string Line )
		{
			if( !string.IsNullOrEmpty( OpenedDb ) )
			{
				ExecQuery( Line );
				return;
			}

			string iCommand = Line.ToLower();

			string[] pCommand = iCommand.Split( new char[] { ' ' }, 2 );

			switch ( pCommand[ 0 ] )
			{
				case "help":
					if ( pCommand.Length == 2 ) HelpCommand( pCommand[ 1 ] );
					else ResponseHelp( "help" );
					break;
				case "show":
					ResponseHelp( "show" );
					break;

				case "database":
					DatabaseCommand( Line );
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

		private void DatabaseCommand( string Command )
		{
			string[] Commands = Command.Split( ' ' );
			int len = Commands.Length;
			if ( len < 2 )
			{
				ResponseCommand( "Database: No action" );
				return;
			}

			string Action = Commands[ 1 ].Trim().ToLower();

			switch ( Action )
			{
				case "open":
					if ( len < 3 )
					{
						ResponseCommand( "Database: Please specify a database" );
						break;
					}

					string Db = Commands[ 2 ].Trim().ToLower();

					switch ( Db )
					{
						case "books": OpenedDb = "Books"; goto OpenDb;
						case "caches": OpenedDb = "Caches"; goto OpenDb;
						case "ftsdata": OpenedDb = "FTSData"; goto OpenDb;
						case "settings": OpenedDb = "Settings"; goto OpenDb;
						default:
							ResponseCommand( $"Database: \"{Db}\" does not exists" );
							break;
					}

					break;
				case "show":
					ResponseCommand( "Aviable databases: Books, Caches, FTSData, Settings" );
					break;
				default:
					ResponseCommand( $"No such action: {Action}" );
					break;
			}

			return;

			OpenDb:
			ResponseCommand( $"Entering database command console: {OpenedDb}" );
			PS1.Text = $"Db[{OpenedDb}]";
		}

		private void ExecQuery( string Command )
		{
			ResultDisplayData ResultDD = null;
			string Mesg = null;

			switch ( Command.Trim() )
			{
				case "quit":
				case "q":
					ResponseCommand( "Quit." );
					OpenedDb = null;
					PS1.Text = "";
					return;
			}

			switch ( OpenedDb )
			{
				case "Books":
					using ( var Context = new BooksContext() )
						(ResultDD, Mesg) = GR.Database.DirectSQL.Command.Exec( Context, Command );
					break;

				case "Caches":
					using ( var Context = new ZCacheContext() )
						(ResultDD, Mesg) = GR.Database.DirectSQL.Command.Exec( Context, Command );
					break;

				case "Settings":
					using ( var Context = new SettingsContext() )
						(ResultDD, Mesg) = GR.Database.DirectSQL.Command.Exec( Context, Command );
					break;

				default:
					ResponseCommand( $"\"{OpenedDb}\" is currently unavailable" );
					break;
			}

			if ( ResultDD != null )
			{
				if ( ResultDD.HasData )
				{
					Border B = new Border
					{
						Margin = new Thickness( 5 ),
						Background = new SolidColorBrush( GR.Resources.LayoutSettings.MajorBackgroundColor ),
						MinHeight = 400,
						Height = GR.Resources.LayoutSettings.ScreenHeight * 0.7
					};

					Explorer.GRTableView TableView = new Explorer.GRTableView();
					TableView.ViewMode = Explorer.ViewMode.Table;
					var j = TableView.View( new ResultViewSource( ResultDD ) );

					B.Child = TableView;
					AddElement( B );
				}
				else
				{
					ResponseCommand( "(Empty set)" );
				}
			}

			if( !string.IsNullOrEmpty( Mesg ) )
			{
				ResponseCommand( Mesg );
			}
		}

		private void HelpCommand( string Command )
		{
			switch ( Command )
			{
				case "file":
				case "database":
					ResponseHelp( "help/" + Command );
					break;
				default:
					ResponseCommand( "No help for such command: " + Command );
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

		private void DisplayCommand( string Command ) => AddElement( CommandTextBlock( PS1.Text + CMode.Text + Command ) );
		private void ResponseCommand( string Command, string End = "\n" ) => AddElement( CommandTextBlock( Command + End ) );

		private void AddElement( UIElement Elem )
		{
			OutputPanel.Children.Add( Elem );
		}

		private void OutputPanel_SizeChanged( object sender, SizeChangedEventArgs e )
		{
			OutputSV.ChangeView( null, OutputSV.ScrollableHeight, null );
		}

		private void ResponseHelp( string ManFile )
		{
			ResponseCommand( File.ReadAllText( $"Strings/man/{ManFile}.txt" ) );
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