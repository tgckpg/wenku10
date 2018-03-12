using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku10;
using wenku10.Pages;
using wenku10.ShHub;
using wenku10.Pages.Dialogs;

namespace GR.GSystem
{
	using CompositeElement;
	using DataSources;
	using Ext;
	using Resources;

	sealed class MasterCommandManager
	{
		private IObservableVector<ICommandBarElement> CommandList;
		private IObservableVector<ICommandBarElement> SecondCmdList;

		private ICommandBarElement[] MasterCommands = new ICommandBarElement[ 0 ];
		private ICommandBarElement[] M2ndCommands = new ICommandBarElement[ 0 ];

		private ICommandBarElement[] CommonCommands;
		private ICommandBarElement[] SystemCommands;

		private StringResources stx = new StringResources( "AppResources", "Settings", "ContextMenu", "AppBar", "NavigationTitles" );

		SecretSwipeButton AboutBtn;

		private MesgListerner SHListener;

		public MasterCommandManager( IObservableVector<ICommandBarElement> CommandList, IObservableVector<ICommandBarElement> SecondCmdList )
		{
			this.CommandList = CommandList;
			this.SecondCmdList = SecondCmdList;

			SHListener = new MesgListerner();

			DefaultCmds();
		}

		private void DefaultCmds()
		{
			CreateSystemCommands();
			CreateCommonCommands();

			InitCommands();
		}

		private void CreateCommonCommands()
		{
			SecondaryIconButton HistoryBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.History, stx.Text( "History", "NavigationTitles" ) );
			// Goto Explorer, Auto Select History
			HistoryBtn.Click += ( s, e ) => ControlFrame.Instance.NavigateTo(
				PageId.MASTER_EXPLORER,
				() => new MasterExplorer(),
				P => ( ( MasterExplorer ) P ).NavigateToDataSource( typeof( HistoryData ) )
			);

			SecondaryIconButton ManagePinsBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Pinned, stx.Text( "ManagePins", "NavigationTitles" ) );
			ManagePinsBtn.Click += CreateCmdHandler( PageId.MANAGE_PINS, () => new ManagePins() );

			SecondaryIconButton EBWinBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Dictionary, "EBWin" );
			EBWinBtn.Click += CreateCmdHandler( async ( s, e ) =>
			{
				await Popups.ShowDialog( new EBDictSearch( new Model.Text.Paragraph( "" ) ) );
			} );

			CommonCommands = new ICommandBarElement[] { HistoryBtn, EBWinBtn, ManagePinsBtn };
		}

		private void CreateSystemCommands()
		{
			List<ICommandBarElement> Btns = new List<ICommandBarElement>();

			SecondaryIconButton SettingsBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Settings, stx.Text( "Settings", "AppBar" ) );
			SettingsBtn.Click += CreateCmdHandler( PageId.MAIN_SETTINGS, () => new global::wenku10.Pages.Settings.MainSettings() );

			SecondaryIconButton BackupBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.UpdateRestore, stx.Text( "BackupAndRestore", "AppBar" ) );
			BackupBtn.Click += BackupBtn_Click;

			AboutBtn = new SecretSwipeButton( SegoeMDL2.Info )
			{
				Label = stx.Text( "About", "AppBar" ),
				Label2 = "(ﾟ∀ﾟ)",
				Glyph2 = SegoeMDL2.Accept
			};

			AboutBtn.PendingClick += CreateCmdHandler( PageId.ABOUT, () => new About() );
			AboutBtn.CanSwipe = true;
			AboutBtn.OnIndexUpdate += ( s, i ) => Logger.Log( AboutBtn.Label2, i.ToString(), LogType.DEBUG );

			Btns.Add( BackupBtn );
			Btns.Add( new AppBarSeparator() );
			Btns.Add( SettingsBtn );
			Btns.Add( AboutBtn );

			if ( MainStage.Instance.IsPhone )
			{
				SecondaryIconButton ExitBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.ChromeClose, stx.Text( "Exit", "AppBar" ) );
				ExitBtn.Click += ( s, e ) =>
				{
					Windows.ApplicationModel.Core.CoreApplication.Exit();
				};
				Btns.Add( ExitBtn );
			}

			SystemCommands = Btns.ToArray();
		}

		private void InitCommands()
		{
			CommandList.Clear();

			new Bootstrap().Level2();

			foreach ( ICommandBarElement Btn in MasterCommands )
				CommandList.Add( Btn );

			ControlFrame.Instance.SetHomePage( PageId.MASTER_EXPLORER, () => new MasterExplorer() );
		}

		private RoutedEventHandler CreateCmdHandler( string Name, Func<Page> ViewFunc )
		{
			return ( s, e ) =>
			{
				ToggleButtons( ( AppBarToggleButton ) s );
				ControlFrame.Instance.NavigateTo( Name, ViewFunc );
			};
		}

		private RoutedEventHandler CreateCmdHandler( RoutedEventHandler Handler )
		{
			return ( s, e ) =>
			{
				ToggleButtons( ( AppBarToggleButton ) s );
				Handler( s, e );
			};
		}

		public void Set2ndCommands( IList<ICommandBarElement> Commands )
		{
			SecondCmdList.Clear();

			if ( Commands != null && 0 < Commands.Count )
			{
				foreach ( ICommandBarElement e in Commands ) SecondCmdList.Add( e );
				SecondCmdList.Add( new AppBarSeparator() );
			}

			if ( 0 < M2ndCommands.Length )
			{
				foreach ( ICommandBarElement e in M2ndCommands ) SecondCmdList.Add( e );
				SecondCmdList.Add( new AppBarSeparator() );
			}

			foreach ( ICommandBarElement e in CommonCommands ) SecondCmdList.Add( e );

			SecondCmdList.Add( new AppBarSeparator() );
			foreach ( ICommandBarElement e in SystemCommands ) SecondCmdList.Add( e );
		}

		public void SetMajorCommands( IList<ICommandBarElement> Controls, bool MajorNav )
		{
			CommandList.Clear();

			if ( MajorNav )
			{
				foreach ( ICommandBarElement Btn in MasterCommands )
					CommandList.Add( Btn );
			}

			if ( Controls != null )
			{
				if ( MajorNav )
					CommandList.Add( new AppBarSeparator() );

				foreach ( ICommandBarElement Btn in Controls )
					CommandList.Add( Btn );
			}
		}

		private void ToggleButtons( AppBarToggleButton s )
		{
			foreach ( AppBarToggleButton Btn in MasterCommands.Where( x => x != s ) ) Btn.IsChecked = false;
			s.IsChecked = true;
		}

		private async void BackupBtn_Click( object sender, RoutedEventArgs e )
		{
			StringResources stx = new StringResources( "Message" );

			bool ConfirmRestore = false;

			await Popups.ShowDialog( UIAliases.CreateDialog(
				stx.Str( "RestartToRestore" )
				, () => ConfirmRestore = true
				, stx.Str( "Yes" ), stx.Str( "No" ) )
			);

			if( ConfirmRestore )
			{
				Config.Properties.RESTORE_MODE = true;
				Windows.ApplicationModel.Core.CoreApplication.Exit();
			}
		}

	}
}