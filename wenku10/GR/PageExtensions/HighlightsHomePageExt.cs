using Microsoft.Services.Store.Engagement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using wenku10.Pages.Dialogs;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Effects;
	using Model.Loaders;
	using Model.Interfaces;
	using Resources;
	using Settings;

	class HighlightsHomePageExt : PageExtension, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; protected set; }
		public IList<ICommandBarElement> Major2ndControls { get; protected set; }
		public IList<ICommandBarElement> MinorControls { get; protected set; }

		AppBarButton FeedbackBtn;
		AppBarButton NewsBtn;
		Storyboard NewsStory;

		public override void Unload()
		{
		}

		protected override void SetTemplate()
		{
			InitAppBar();
		}

		private void InitAppBar()
		{
			StringResources stx = StringResources.Load( "NavigationTitles" );

			if ( StoreServicesFeedbackLauncher.IsSupported() )
			{
				FeedbackBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.FeedbackApp, stx.Text( "Feedback" ) );
				FeedbackBtn.Click += FeedbackBtn_Click;
				MinorControls = new ICommandBarElement[] { FeedbackBtn };
			}

			NewsBtn = UIAliases.CreateAppBarBtn( Symbol.Important, stx.Text( "News" ) );
			NewsBtn.Click += NewsBtn_Click;

			NewsStory = new Storyboard();
			SimpleStory.DoubleAnimation( NewsStory, NewsBtn, "Opacity", 0, 1, 350 );
			NewsStory.AutoReverse = true;
			NewsStory.RepeatBehavior = RepeatBehavior.Forever;

			MajorControls = new ICommandBarElement[] { NewsBtn };

			GetAnnouncements();

			MessageBus.SendUI( typeof( GR.GSystem.ActionCenter ), AppKeys.PM_CHECK_TILES );
		}

		private async void GetAnnouncements()
		{
			NewsLoader AS = new NewsLoader();
			await AS.Load();

			if ( AS.HasNewThings ) NewsStory.Begin();
		}

		private void FeedbackBtn_Click( object sender, RoutedEventArgs e )
		{
			var j = StoreServicesFeedbackLauncher.GetDefault()?.LaunchAsync();
		}

		private void NewsBtn_Click( object sender, RoutedEventArgs e ) { ShowNews(); }

		private async void ShowNews()
		{
			NewsStory.Stop();

			Announcements NewsDialog = new Announcements();
			await Popups.ShowDialog( NewsDialog );
		}
	}
}
