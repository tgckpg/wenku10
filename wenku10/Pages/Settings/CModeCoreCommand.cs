using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;

using GR.Resources;

namespace wenku10.Pages.Settings
{
	public sealed partial class ConsoleMode : Page
	{
		private string cwd = "./";
		private int AbsoluteHere = Path.GetFullPath( "./" ).Length;

		private string ResolvePath( string p )
		{
			p = Path.GetFullPath( p );

			if ( p.Length < AbsoluteHere ) p = ".";
			else p = ( "./" + p.Substring( AbsoluteHere ).Replace( '\\', '/' ) ).TrimEnd( '/' );

			return p;
		}

		private async void CoreCommand( string Line )
		{
			NextSeg( ref Line, out string Cmd );
			Cmd = Cmd.ToLower();

			List<string> Options = new List<string>();
			NextSeg( ref Line, out string Target );
			while ( !string.IsNullOrEmpty( Target ) && Target[ 0 ] == '-' )
			{
				Options.Add( Target );
				NextSeg( ref Line, out Target );
			}

			string p;
			try
			{
				p = ResolvePath( cwd + Target );
			}
			catch ( Exception ex )
			{
				ResponseError( ex.Message );
				return;
			}

			switch ( Cmd )
			{
				case "ls":

					if ( Shared.Storage.FileExists( p ) )
					{
						ResponseCommand( Target );
						return;
					}

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
					else
					{
						if ( Target == "" )
						{
							ResponseError( "ls: cannot open directory '.': No such file or directory" );
						}
						else
						{
							ResponseError( "ls: cannot access '" + Target + "': No such file or directory" );
						}
					}
					return;

				case "mv":
					if ( NextSeg( ref Line, out string ToTarget ) )
					{
						try
						{
							string pt = ResolvePath( cwd + ToTarget );

							if ( pt == p )
							{
								ResponseError( $"mv: '{Target}' and '{Target}' are the same file" );
							}
							else if ( pt.IndexOf( p ) == 0 )
							{
								ResponseError( $"mv: cannot move '{Target}' to a subdirectory of itself, '{ToTarget}'" );
							}
							else if ( Shared.Storage.DirExist( p ) )
							{
								if ( Shared.Storage.FileExists( pt ) )
								{
									ResponseError( $"mv: cannot overwrite non-directory '{ToTarget}' with directory '{Target}'" );
								}
								else if ( Shared.Storage.DirExist( pt ) )
								{
									Shared.Storage.MoveDir( p, pt + "/" + Path.GetFileName( p ) );
								}
								else
								{
									Shared.Storage.MoveDir( p, pt );
								}
							}
							else if ( Shared.Storage.FileExists( p ) )
							{
								if ( Shared.Storage.DirExist( pt ) )
								{
									Shared.Storage.MoveFile( p, pt + "/" + Path.GetFileName( p ) );
								}
								else if ( ToTarget.EndsWith( "/" ) )
								{
									ResponseError( $"mv: cannot move '{Target}' to '{ToTarget}': No such file or directory" );
								}
								else
								{
									Shared.Storage.MoveFile( p, pt );
								}
							}
							else
							{
								ResponseError( $"mv: cannot stat '{Target}': No such file or directory" );
							}
						}
						catch ( Exception ex )
						{
							ResponseError( ex.Message );
						}
					}
					else
					{
						ResponseError( $"mv: missing destination file operand after '{Target}'" );
					}
					return;

				case "wc":
					if ( p == "." )
					{
						ResponseError( "wc: ./: Is a directory" );
						return;
					}

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
							ResponseError( "wc: No options provided or unknown/unsupported option." );
						}
						CommandInput.IsEnabled = true;
					}
					else
					{
						ResponseError( "cat: " + Target + ": No such file" );
					}
					return;

				case "cat":
					if ( p == "." )
					{
						ResponseError( "cat: ./: Is a directory" );
						return;
					}

					if ( Shared.Storage.FileExists( p ) )
					{
						ResponseCommand( Shared.Storage.GetString( p ) );
					}
					else
					{
						ResponseError( "cat: " + Target + ": No such file" );
					}
					return;

				case "mkdir":
					if ( Shared.Storage.DirExist( p ) || Shared.Storage.FileExists( p ) )
					{
						ResponseError( $"mkdir: cannot create directory ‘{Target}’: File exists" );
					}
					else
					{
						Shared.Storage.CreateDirectory( p );
					}
					return;

				case "touch":
					if ( p == "." )
						return;

					if ( !( Shared.Storage.DirExist( p ) || Shared.Storage.FileExists( p ) ) )
					{
						if ( !Shared.Storage.WriteBytes( p, new byte[ 0 ] ) )
						{
							ResponseError( "Access is denined" );
						}
					}
					return;

				case "rm":
					if ( p == "." )
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
										var NOP = Dispatcher.RunIdleAsync( u => ResponseCommand( "rm -r " + x, "/" ) );
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
								ResponseError( "Operation canceled." );
							}
						};
						return;
					}

					if ( Shared.Storage.FileExists( p ) )
					{
						Shared.Storage.DeleteFile( p );
						if ( p.ToLower().EndsWith( ".db" ) )
						{
							ResponseCommand(
								"You just removed a database file! Application might crash if some databases are missing."
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
							ResponseError( "rm: cannot remove '" + Target + "': Is a directory" );
						}
					}
					else
					{
						ResponseError( "rm: " + Target + ": No such file or directory" );
					}
					return;

				case "cd":

					if ( p == "." )
					{
						cwd = "./";
						return;
					}

					if ( Shared.Storage.DirExist( p ) )
					{
						cwd = p + "/";
					}
					else
					{
						ResponseError( "cd: " + Target + ": No such file or directory" );
					}
					return;

				case "pwd": ResponseCommand( cwd ); return;
				case "date": ResponseCommand( DateTime.Now.ToString() ); return;
				case "uuidgen": ResponseCommand( Guid.NewGuid().ToString() ); return;

				case "import":
					if ( Shared.Storage.DirExist( p ) )
					{
						if ( Target == "" ) Target = "./";
						ResponseError( "import: " + Target + ": Is a directory" );
						return;
					}

					CommandInput.IsEnabled = false;
					try
					{
						IStorageFile ISF = await AppStorage.OpenFileAsync( "*" );
						if ( ISF == null )
						{
							ResponseError( $"import: {Target}: terminated", "" );
						}
						else
						{
							ResponseCommand( "Importing file ...", "" );
							await Task.Run( async () =>
							{
								using ( Stream ss = await ISF.OpenStreamForReadAsync() )
									Shared.Storage.WriteStream( p, ss );
							} );
						}

						ResponseCommand( "", "" );
					}
					catch ( Exception ex )
					{
						ResponseError( ex.Message );
					}

					CommandInput.IsEnabled = true;
					return;

				case "export":
					CommandInput.IsEnabled = false;

					try
					{
						if ( Shared.Storage.FileExists( p ) )
						{
							IStorageFile ISF = await AppStorage.SaveFileAsync( "Export", new string[] { Path.GetExtension( p ) }, Path.GetFileName( p ) );
							if ( ISF == null )
							{
								ResponseError( $"export: {Target}: terminated", "" );
							}
							else
							{
								ResponseCommand( "exporting file ...", "" );
								using ( Stream s = Shared.Storage.GetStream( p ) )
								using ( Stream ss = await ISF.OpenStreamForWriteAsync() )
									await s.CopyToAsync( ss );
							}

							ResponseCommand( "", "" );
						}
						else if ( Shared.Storage.DirExist( p ) )
						{
							if ( Target == "" ) Target = "./";
							ResponseError( "export: " + Target + ": Is a directory" );
						}
						else
						{
							ResponseError( "export: " + Target + ": No such file or directory" );
						}
					}
					catch ( Exception ex )
					{
						ResponseError( ex.Message );
					}

					CommandInput.IsEnabled = true;
					return;
			}
		}
	}
}