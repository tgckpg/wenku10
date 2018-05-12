using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.Linq;

using GR.Resources;

namespace wenku10.Pages.Settings
{
	public sealed partial class ConsoleMode : Page
	{
		private string cwd = "./";
		private int AbsoluteHere = Path.GetFullPath( "./" ).Length;

		private async void FileCommand( string Line )
		{
			NextSeg( ref Line, out string Cmd );
			List<string> Options = new List<string>();

			// Extract options
			NextSeg( ref Line, out string Target );
			while ( !string.IsNullOrEmpty( Target ) && Target[ 0 ] == '-' )
			{
				Options.Add( Target );
				NextSeg( ref Line, out Target );
			}

			string p = Path.GetFullPath( cwd + Target );

			switch ( Cmd )
			{
				case "ls":
					if ( p.Length < AbsoluteHere ) p = "./";
					else p = ( "./" + p.Substring( AbsoluteHere ).Replace( '\\', '/' ) ).TrimEnd( '/' );

					p = p + "/";
					if ( Shared.Storage.DirExist( p ) )
					{
						string Lines = string.Join( "/\n", Shared.Storage.ListDirs( p ) );
						if ( !string.IsNullOrEmpty( Lines ) )
						{
							ResponseCommand( Lines + "/", "" );
						}

						Lines = string.Join( "\n", Shared.Storage.ListFiles( p ) );
						if ( !string.IsNullOrEmpty( Lines ) )
						{
							ResponseCommand( Lines, "" );
						}

						ResponseCommand( "", "" );
					}
					return;

				case "wc":
					if ( p.Length < AbsoluteHere )
					{
						ResponseCommand( "wc: ./: Is a directory" );
						return;
					}

					p = ( "./" + p.Substring( AbsoluteHere ).Replace( '\\', '/' ) ).TrimEnd( '/' );
					if ( Shared.Storage.FileExists( p ) )
					{
						CommandInput.IsEnabled = false;
						if ( Options.Contains( "-l" ) )
						{
							ResponseCommand( await Shared.Storage.LinesCount( p ) + " " + p.Substring( 2 ) );
						}
						else if ( Options.Contains( "-c" ) )
						{
							ResponseCommand( await Shared.Storage.FileSize( p ) + " " + p.Substring( 2 ) );
						}
						else
						{
							ResponseCommand( "wc: No options provided or unknown/unsupported option." );
						}
						CommandInput.IsEnabled = true;
					}
					else
					{
						ResponseCommand( "cat: " + Target + ": No such file" );
					}
					return;

				case "cat":
					if ( p.Length < AbsoluteHere )
					{
						ResponseCommand( "cat: ./: Is a directory" );
						return;
					}

					p = ( "./" + p.Substring( AbsoluteHere ).Replace( '\\', '/' ) ).TrimEnd( '/' );
					if ( Shared.Storage.FileExists( p ) )
					{
						ResponseCommand( Shared.Storage.GetString( p ) );
					}
					else
					{
						ResponseCommand( "cat: " + Target + ": No such file" );
					}
					return;

				case "touch":
					if ( p.Length < AbsoluteHere )
						return;

					p = ( "./" + p.Substring( AbsoluteHere ).Replace( '\\', '/' ) ).TrimEnd( '/' );
					if ( !( Shared.Storage.DirExist( p ) || Shared.Storage.FileExists( p ) ) )
					{
						if( !Shared.Storage.WriteBytes( p, new byte[ 0 ] ) )
						{
							ResponseCommand( "Access is denined" );
						}
					}
					return;

				case "rm":
					if ( p.Length < AbsoluteHere )
					{
						ResponseCommand( "Are you sure you want to clear application root? This will reset the application states." );

						PS1.Text = "Confirm reset? (y/n)";
						PendingConfirm = async ( bool Confirmed ) =>
						{
							PS1.Text = "";
							if ( Confirmed )
							{
								CommandInput.IsEnabled = false;

								await Task.Run( () =>
								{
									Shared.Storage.ListDirs( "./" ).ExecEach( x =>
									{
										var NOP = Dispatcher.RunIdleAsync( u => ResponseCommand( "rm " + x, "" ) );
										Shared.Storage.RemoveDir( x );
									} );

									Shared.Storage.ListFiles( "./" ).ExecEach( x =>
									{
										var NOP = Dispatcher.RunIdleAsync( u => ResponseCommand( "rm " + x, "" ) );
										Shared.Storage.DeleteFile( x );
									} );

									var j = Dispatcher.RunIdleAsync( u => ResponseCommand( "done" ) );
								} );

								CommandInput.IsEnabled = true;
							}
							else
							{
								ResponseCommand( "Operation canceled." );
							}
						};
						return;
					}

					p = ( "./" + p.Substring( AbsoluteHere ).Replace( '\\', '/' ) ).TrimEnd( '/' );

					if ( p == "." && Options.Contains( "-r" ) )
						goto case "rm";

					if ( Shared.Storage.FileExists( p ) )
					{
						Shared.Storage.DeleteFile( p );
						if( p.EndsWith( ".db" ) )
						{
							ResponseCommand(
								"You just removed a database file! Application might crash if some database are missing."
								+ " Remember to run \"Database Migrate\" to create required databases." );
						}
					}
					else if ( Shared.Storage.DirExist( p ) )
					{
						if ( Options.Contains( "-r" ) )
						{
							Shared.Storage.RemoveDir( p );
						}
						else
						{
							ResponseCommand( "rm: cannot remove '" + Target + "': Is a directory" );
						}
					}
					else
					{
						ResponseCommand( "rm: " + Target + ": No such file or directory" );
					}
					return;

				case "cd":

					if ( p.Length < AbsoluteHere )
					{
						cwd = "./";
						return;
					}

					p = ( "./" + p.Substring( AbsoluteHere ).Replace( '\\', '/' ) ).TrimEnd( '/' );
					if ( Shared.Storage.DirExist( p ) )
					{
						cwd = p + "/";
					}
					else
					{
						ResponseCommand( "cd: " + Target + ": No such file or directory" );
					}
					return;

				case "pwd":
					ResponseCommand( cwd );
					return;
			}
		}
	}
}