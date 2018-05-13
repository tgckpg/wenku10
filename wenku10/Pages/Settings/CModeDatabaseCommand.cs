using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

using GR.Database;
using GR.Database.Contexts;
using GR.Database.DirectSQL;

namespace wenku10.Pages.Settings
{
	public sealed partial class ConsoleMode : Page
	{
		private string OpenedDb;

		private async void DatabaseCommand( string Command )
		{
			string[] Commands = Command.Split( ' ' );
			int len = Commands.Length;
			if ( len < 2 )
			{
				ResponseCommand( "Database: No action" );
				return;
			}

			string _action = Commands[ 1 ].Trim().ToLower();

			switch ( _action )
			{
				case "open":
				case "use":
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
				case "migrate":
					ResponseCommand( "Migrating Databases ...", "" );
					CommandInput.IsEnabled = false;

					string ErrorMesg = null;
					await Task.Run( () => {
						try
						{
							ContextManager.Migrate();
						}
						catch( Exception ex )
						{
							ErrorMesg = ex.Message;
						}
					} );

					if ( ErrorMesg != null )
					{
						ResponseCommand( ErrorMesg, "" );
						ResponseCommand( "Migration failed: Existing database might be corrupted. Please remove those databases and re-run this command" );
					}

					CommandInput.IsEnabled = true;
					ResponseCommand( "Done." );
					break;
				case "show":
					ResponseCommand( "Aviable databases: Books, Caches, FTSData, Settings" );
					break;
				default:
					ResponseCommand( $"No such action: {_action}" );
					break;
			}

			return;

			OpenDb:
			ResponseCommand( $"Entering database command console: {OpenedDb}" );
			PS1.Text = $"Db[{OpenedDb}]";
		}

		private async void ExecQuery( string Command )
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

			CommandInput.IsEnabled = false;
			await Task.Run( () =>
			{
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

					case "FTSData":
						using ( var Context = new FTSDataContext() )
							(ResultDD, Mesg) = GR.Database.DirectSQL.Command.Exec( Context, Command );
						break;

					default:
						Mesg = $"\"{OpenedDb}\" is currently unavailable";
						break;
				}
			} );
			CommandInput.IsEnabled = true;

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

			if ( !string.IsNullOrEmpty( Mesg ) )
			{
				ResponseCommand( Mesg );
			}
		}
	}
}
