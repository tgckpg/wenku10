using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.Linq;

using GR.Config;

namespace wenku10.Pages.Settings
{
	public sealed partial class ConsoleMode : Page
	{
		private readonly Type StringType = typeof( string );

		private string[] _FlagKeys;
		private string[] FlagKeys
		{
			get
			{
				if ( _FlagKeys == null )
				{
					_FlagKeys = typeof( Parameters ).GetFields().Where( x => x.FieldType == StringType ).Remap( x => x.Name );
				}
				return _FlagKeys;
			}
		}

		private void SysctlCommand( string Line )
		{
			NextSeg( ref Line, out string Cmd );

			List<string> Options = new List<string>();

			NextSeg( ref Line, out string Args );
			while ( !string.IsNullOrEmpty( Args ) && Args[ 0 ] == '-' )
			{
				Options.Add( Args );
				NextSeg( ref Line, out Args );
			}

			Type AppProps = typeof( Properties );

			if ( Options.Contains( "-a" ) )
			{
				ResponseCommand( string.Join( "\n", FlagKeys.Remap( x => x + " = " + AppProps.GetProperty( x ).GetValue( null ) ) ) );
				return;
			}

			if( Options.Contains( "-l" ) )
			{
				FieldInfo[] ParamFields = typeof( Parameters ).GetFields();
				string[] ParamValues = ParamFields.Where( x => x.FieldType == StringType ).Remap( x => ( string ) x.GetValue( null ) );

				if ( Options.Contains( "--clean" ) )
				{
					ApplicationData.Current.LocalSettings.Values
						.Where( x => Array.IndexOf( ParamValues, x.Key ) == -1 )
						.ExecEach( x => ApplicationData.Current.LocalSettings.Values.Remove( x.Key ) );
					ResponseCommand( "ok" );
				}
				else
				{
					ResponseCommand( string.Join(
						"\n", ApplicationData.Current.LocalSettings.Values
							.Where( x => Array.IndexOf( ParamValues, x.Key ) == -1 )
							.Remap( x => $"{x.Key} = {x.Value}" )
					) );
				}
				return;
			}

			if( Options.Contains( "--clean" ) )
			{
				ResponseError( "Operation not permitted. Did you mean '-l --clean'?" );
				return;
			}

			Args = Args + Line;
			if ( NextSeg( ref Args, out string Key, new char[] { '=', ' ' } ) )
			{
				if ( FlagKeys.Contains( Key ) )
				{
					if ( NextSeg( ref Args, out string Value ) )
					{
						PropertyInfo Prop = AppProps.GetProperty( Key );
						Type PropType = Prop.PropertyType;

						if ( PropType == StringType )
						{
							Prop.SetValue( null, Value );
							ResponseCommand( $"{Key} = {Value}" );
						}
						else if ( PropType == typeof( int ) )
						{
							if ( int.TryParse( Value, out int IntValue ) )
							{
								Prop.SetValue( null, IntValue );
								ResponseCommand( $"{Key} = {IntValue}" );
							}
							else
							{
								ResponseError( $"sysctl: {Key}: '{Value}' is not a valid {PropType}" );
							}
						}
						else if ( PropType == typeof( bool ) )
						{
							if ( bool.TryParse( Value, out bool BoolValue ) )
							{
								Prop.SetValue( null, BoolValue );
								ResponseCommand( $"{Key} = {BoolValue}" );
							}
							else
							{
								ResponseError( $"sysctl: {Key}: '{Value}' is not a valid {PropType}" );
							}
						}
						else
						{
							ResponseError( $"sysctl: {Key}: unsupported value type: {PropType}" );
						}
					}
					else
					{
						ResponseCommand( Key + " = " + AppProps.GetProperty( Key ).GetValue( null ) );
					}
				}
				else
				{
					ResponseError( $"sysctl: {Key}: no such key" );
				}
			}
			else
			{
				ResponseError( "sysctl: no variables specified" );
			}
		}

	}
}