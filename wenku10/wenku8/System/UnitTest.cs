using System;
using System.IO;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.UnitTest;

namespace wenku8
{
	using Ext;
	using Resources;
	using Settings;

	class UnitTest : NetTrigger
	{
		public void Test_AppGateDownload( TestResult t )
		{
			IRuntimeCache wc = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
			wc.InitDownload(
				"Test"
				, X.Call<XKey[]>( XProto.WRequest, "GetBookInfo", "20" )
				, ( DRequestCompletedEventArgs e, string id ) =>
				{
					t.writeLine( "Download Success: " + id );
					t.writeLine( e.ResponseString );
					t.Done( true );
				}
				, ( string a, string b, Exception c ) =>
				{
					t.writeLine( b );
					t.Done( false );
				}
				, false
			);
		}

		public void Test_AppStorageRW( TestResult t )
		{
			try
			{
				string filename = "TEST_APPSTORAGE_WRITE/1/2/3/4.txt";
				if ( Shared.Storage.WriteString( filename, filename ) )
				{
					string backEcho = Shared.Storage.GetString( filename );
					if( backEcho != filename )
					{
						throw new Exception( "Data retrived is not equal to data written" );
					}

					Shared.Storage.DeleteFile( filename );
					if ( Shared.Storage.FileExists( filename ) )
						throw new Exception( "Unable to remove file" );
				}
				else
				{
					throw new Exception( "Failed to write file" );
				}
				t.Done( true );
			}
			catch ( Exception ex )
			{
				t.writeLine( "Failed to write file: " + ex.Message );
				t.Done( false );
			}

		}

		public async void Test_AppStorageLibrary( TestResult t )
		{
			try
			{
				string filename = "unit_test_img.gif";
				await Shared.Storage.DeletePicture( filename );

				await Shared.Storage.SavePicture(
					filename
					, new MemoryStream( Resources.Image.EMPTY_IMAGE )
				);

				if ( !await Shared.Storage.SearchLibrary( filename ) )
				{
					throw new Exception( "Unable to find the saved picture from library" );
				}
				await Shared.Storage.DeletePicture( filename );

				t.Done( true );
			}
			catch( Exception ex )
			{
				t.writeLine( "Library Operation failed: " + ex.Message );
				t.Done( false );
			}
		}

		/*
		public void Test_NAME( TestResult t )
		{
		}
		*/

	}
}
