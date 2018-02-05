using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku10.Pages;
using wenku10.Pages.Dialogs;
using wenku10.Pages.Sharers;

namespace GR.PageExtensions
{
	using CompositeElement;
	using DataSources;
	using Model.Interfaces;
	using Resources;

	sealed class ONSPageExtension : PageExtension, ICmdControls
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => true;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private ONSViewSource ViewSource;
		private AppBarButtonEx ActivyBtn;

		public ONSPageExtension( ONSViewSource ViewSource )
		{
			this.ViewSource = ViewSource;
		}

		protected override void SetTemplate()
		{
			InitAppBar();
		}

		public override void Unload()
		{
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppResources", "ContextMenu" );

			ActivyBtn = new AppBarButtonEx()
			{
				Icon = new SymbolIcon( Symbol.Message )
				, Label = stx.Text( "Messages" )
				, Foreground = new SolidColorBrush( LayoutSettings.RelativeMajorBackgroundColor )
			};

			ActivyBtn.Click += ToggleActivities;

			SecondaryIconButton UploadBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Upload, stx.Text( "SubmitScript" ) );
			UploadBtn.Click += ( s, e ) => ControlFrame.Instance.SubNavigateTo( Page, () => new ScriptUpload( UploadExit ) );

			SecondaryIconButton MAuthBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Manage, stx.Text( "ManageAuths", "ContextMenu" ) );
			MAuthBtn.Click += ManageAuths;

			MajorControls = new ICommandBarElement[] { ActivyBtn };

#if DEBUG || TESTING
			StringResources sts = new StringResources( "Settings" );
			SecondaryIconButton ChangeServer = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.DirectAccess, sts.Text( "Advanced_Server" ) );
			ChangeServer.Click += async ( s, e ) =>
			{
				ValueHelpInput VH =  new ValueHelpInput(
					Shared.ShRequest.Server.ToString()
					, sts.Text( "Advanced_Server" ), "Address"
				) ;

				await Popups.ShowDialog( VH );
				if ( VH.Canceled ) return;

				try
				{
					new Uri( VH.Value );
					Config.Properties.SERVER_OSD_URI = VH.Value;
					Shared.ShRequest.UpdateServer();
				}
				catch ( Exception ) { }
			};

			Major2ndControls = new ICommandBarElement[] { UploadBtn, MAuthBtn, ChangeServer };
#else
			Major2ndControls = new ICommandBarElement[] { UploadBtn, MAuthBtn };
#endif
		}

		private void UploadExit( string Id, string AccessToken )
		{
			var j = ControlFrame.Instance.CloseSubView();
			// SHHub.Search( "uuid: " + Id, new string[] { AccessToken } );
		}

		private void ManageAuths( object sender, RoutedEventArgs e )
		{
			ControlFrame.Instance.SubNavigateTo( Page, () => new ManageAuth() );
		}

		private async void ToggleActivities( object sender, RoutedEventArgs e )
		{
			if ( !( await ControlFrame.Instance.CommandMgr.Authenticate() ) ) return;

			/*
			if ( Member.Activities.Count == 0 )
			{
				UpdateActivities();
			}
			else
			{
				if ( TransitionDisplay.GetState( ActivyList ) == TransitionState.Active )
				{
					TransitionDisplay.SetState( ActivyList, TransitionState.Inactive );
				}
				else
				{
					TransitionDisplay.SetState( ActivyList, TransitionState.Active );
				}
			}
			*/
		}
	}
}