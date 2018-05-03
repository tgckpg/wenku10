using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace GR.MigrationOps
{
	using Config;
	using Database.Contexts;
	using Model.Interfaces;
	using Resources;
	using Settings;
	using Settings.Theme;

	sealed class M0001 : IMigrationOp
	{
		public Action<string> Mesg { get; set; }
		public Action<string> MesgR { get; set; }

		public bool ShouldMigrate { get; set; }

		public M0001()
		{
			using ( var Context = new SettingsContext() )
			{
				ShouldMigrate = ( Context.Theme.Find( ".ColorMajor" ) == null );
			}
		}

		private IPropertySet Settings => ApplicationData.Current.LocalSettings.Values;

		StringResources stx = StringResources.Load( "InitQuestions", "Message", "Settings", "NavigationTitles" );

		public async Task Up()
		{
			try
			{
				Mesg( stx.Text( "MigrateDatabase" ) );
				Database.ContextManager.Migrate();

				using ( var Context = new SettingsContext() )
				{
					Context.Theme.RemoveRange( Context.Theme.ToArray() );
					Context.ContentReader.RemoveRange( Context.ContentReader.ToArray() );
					Context.SaveChanges();
				}

				Mesg( stx.Text( "Appearance_Theme", "Settings" ) );
				await MigrateSettings();

				foreach ( Func<IMigrationOp, Task> MOp in Database.ContextManager.MigrationOps )
				{
					await MOp( this );
				}

				Mesg( stx.Text( "Appearance_Layout", "Settings" ) );
				MigrateContentReaderLayout();

				Mesg( stx.Text( "MigrationComplete" ) + " - M0001" );
			}
			catch ( Exception ex )
			{
				Mesg( ex.Message );
			}
		}

		private Task MigrateSettings()
		{
			return Task.Run( () =>
			{
				ThemeSet ThSet = GSystem.ThemeManager.DefaultDark();
				ThSet.GreyShades();

				GRConfig.ContentReader.BackgroundColor = Color.FromArgb( 255, 20, 20, 20 );
				GRConfig.ContentReader.FontColor = Color.FromArgb( 255, 45, 77, 59 );
				GRConfig.ContentReader.TapBrushColor = Color.FromArgb( 255, 138, 41, 0 );
				GRConfig.ContentReader.BgColorNav = Color.FromArgb( 255, 50, 50, 50 );
				GRConfig.ContentReader.BgColorAssist = Color.FromArgb( 23, 0, 0, 0 );
				ThSet.Apply();

				Dictionary<string, Action<object>> LegacyConf = new Dictionary<string, Action<object>>
				{
					[ "ContentReader_Autobookmark" ] = x => GRConfig.ContentReader.AutoBookmark = ( bool ) x,
					[ "ContentReader_UseInertia" ] = x => GRConfig.ContentReader.UseInertia = ( bool ) x,
					[ "Appearance_ContentReader_FontSize" ] = x => GRConfig.ContentReader.FontSize = ( double ) x,
					[ "Appearance_ContentReader_LeftContext" ] = x => GRConfig.ContentReader.LeftContext = ( bool ) x,
					[ "Appearance_ContentReader_EmbedIllus" ] = x => GRConfig.ContentReader.EmbedIllus = ( bool ) x,
					[ "Appearance_ContentReader_LineHeight" ] = x => GRConfig.ContentReader.LineHeight = ( double ) x,
					[ "Appearance_ContentReader_ParagraphSpacing" ] = x => GRConfig.ContentReader.ParagraphSpacing = 2 * ( double ) x,
					[ "Appearance_ContentReader_BlockHeight" ] = x => GRConfig.ContentReader.BlockHeight = ( double ) x,
					[ "Appearance_ContentReader_FontColor" ] = x => GRConfig.ContentReader.FontColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_Clock_Arc_Hand_Color" ] = x => GRConfig.ContentReader.Clock.ARColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_Clock_Hour_Hand_Color" ] = x => GRConfig.ContentReader.Clock.HHColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_Clock_Hour_Hand_Color" ] = x => GRConfig.ContentReader.Clock.HHColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_Clock_Minute_Hand_Color" ] = x => GRConfig.ContentReader.Clock.MHColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_Clock_Scales_Color" ] = x => GRConfig.ContentReader.Clock.SColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_EpStepper_DateColor" ] = x => GRConfig.ContentReader.EpStepper.DColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_EpStepper_StepperColor" ] = x => GRConfig.ContentReader.EpStepper.SColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_EpStepper_Background" ] = x => GRConfig.ContentReader.EpStepper.BackgroundColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_AssistBg" ] = x => GRConfig.ContentReader.BgColorAssist = GetColorFromByte( x ),
					[ "Appearance_ContentReader_NavBg" ] = x => GRConfig.ContentReader.BgColorNav = GetColorFromByte( x ),
					[ "Appearance_ContentReader_FontWeight" ] = x => GRConfig.ContentReader.FontWeight = new Windows.UI.Text.FontWeight() { Weight = ( ushort ) x },
					[ "Appearance_ContentReader_Background" ] = x => GRConfig.ContentReader.BackgroundColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_ScrollBar" ] = x => GRConfig.ContentReader.ScrollBarColor = GetColorFromByte( x ),
					[ "Appearance_ContentReader_EnableReadingAnchor" ] = x => GRConfig.ContentReader.ReadingAnchor = ( bool ) x,
					[ "Appearance_ContentReader_EnableDoubleTap" ] = x => GRConfig.ContentReader.DoubleTap = ( bool ) x,
					[ "Appearance_ContentReader_TapBrushColor" ] = x => GRConfig.ContentReader.TapBrushColor = GetColorFromByte( x ),

					[ "Appearance_Theme_Text_Color_Relative_To_Background" ] = x => GRConfig.Theme.RelColorMajorBackground = GetColorFromByte( x ),
					[ "Appearance_Theme_Text_Color_Relative_To_Major" ] = x => GRConfig.Theme.RelColorMajor = GetColorFromByte( x ),
					[ "Appearance_Theme_Subtle_Text_Color" ] = x => GRConfig.Theme.SubtleColor = GetColorFromByte( x ),
					[ "Appearance_Theme_Major_Background_Color" ] = x => GRConfig.Theme.BgColorMajor = GetColorFromByte( x ),
					[ "Appearance_Theme_Minor_Background_Color" ] = x => GRConfig.Theme.BgColorMinor = GetColorFromByte( x ),
					[ "Appearance_Theme_Major_Color" ] = x => GRConfig.Theme.ColorMajor = GetColorFromByte( x ),
					[ "Appearance_Theme_Minor_Color" ] = x => GRConfig.Theme.ColorMinor = GetColorFromByte( x ),
					[ "Appearance_Theme_Horizontal_Ribbon_Color" ] = x => GRConfig.Theme.RibbonColorHorz = GetColorFromByte( x ),
					[ "Appearance_Theme_Vertical_Ribbon_Color" ] = x => GRConfig.Theme.RibbonColorVert = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_10" ] = x => GRConfig.Theme.Shades10 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_20" ] = x => GRConfig.Theme.Shades20 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_30" ] = x => GRConfig.Theme.Shades30 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_40" ] = x => GRConfig.Theme.Shades40 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_50" ] = x => GRConfig.Theme.Shades50 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_60" ] = x => GRConfig.Theme.Shades60 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_70" ] = x => GRConfig.Theme.Shades70 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_80" ] = x => GRConfig.Theme.Shades80 = GetColorFromByte( x ),
					[ "Appearance_Theme_Shades_90" ] = x => GRConfig.Theme.Shades90 = GetColorFromByte( x ),
					[ "Appearence_Theme_Relative_Shades_Color" ] = x => GRConfig.Theme.RelColorShades = GetColorFromByte( x ),

					[ "Enable_OneDrive" ] = x => GRConfig.System.EnableOneDrive = ( bool ) x,
					[ "Misc_Text_Patch_Syntax" ] = x => GRConfig.System.PatchSyntax = ( bool ) x,
					[ "Misc_Chunk_Single_Volume" ] = x => GRConfig.System.ChunkSingleVol = ( bool ) x
				};

				foreach ( KeyValuePair<string, Action<object>> Conf in LegacyConf )
				{
					if ( Settings.TryGetValue( Conf.Key, out object value ) )
					{
						Conf.Value( value );
						Settings.Remove( Conf.Key );
					}
				}
			} );
		}

		private void MigrateContentReaderLayout()
		{
			Shared.Storage.DeleteFile( FileLinks.ROOT_SETTING + "Layout_MainPage.xml" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_SETTING + "Layout_NavLPage.xml" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_SETTING + "Layout_StaffPicks.xml" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_SETTING + "Deathblow.xml" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_SETTING + "ReadingHistory.xml" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_SETTING + "LocalBookStorage.xml" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_WTEXT + "SpecialTopics.xml" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_WTEXT + "SpecialTopics.lat" );
			Shared.Storage.DeleteFile( FileLinks.ROOT_WTEXT + "presslist.xml" );

			XRegistry XReg = new XRegistry( "<xml />", FileLinks.ROOT_SETTING + "Layout_BookInfoView.xml" );

			GRConfig.System.TwitterConfirmed = XReg.Parameter( "TwitterConfirmed" )?.GetBool( "val", false ) == true;

			XParameter BgConf = XReg.Parameter( "INFO_VIEW" );
			if( BgConf != null )
			{
				GRConfig.BookInfoView.BgContext.BgType = BgConf.GetValue( "type" );
				GRConfig.BookInfoView.BgContext.BgValue = BgConf.GetValue( "value" );
			}

			Shared.Storage.DeleteFile( XReg.Location );

			XReg = new XRegistry( "<xml />", FileLinks.ROOT_SETTING + "Layout_ContentReader.xml" );
			GRConfig.ContentReader.IsRightToLeft = XReg.Parameter( "IsRightToLeft" )?.GetBool( "enable", false ) == true;
			GRConfig.ContentReader.IsHorizontal = XReg.Parameter( "IsHorizontal" )?.GetBool( "enable", false ) == true;

			BgConf = XReg.Parameter( "CONTENT_READER" );
			if( BgConf != null )
			{
				GRConfig.BookInfoView.BgContext.BgType = BgConf.GetValue( "type" );
				GRConfig.BookInfoView.BgContext.BgValue = BgConf.GetValue( "value" );
			}

			Shared.Storage.DeleteFile( XReg.Location );
		}

		private Color GetColorFromByte( object obj )
		{
			if ( obj is byte[] b )
			{
				Color c = new Color();
				c.A = b[ 0 ];
				c.R = b[ 1 ];
				c.G = b[ 2 ];
				c.B = b[ 3 ];
				return c;
			}

			return Colors.Black;
		}

	}
}