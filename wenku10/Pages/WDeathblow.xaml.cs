using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Pages;

namespace wenku10.Pages
{
	sealed partial class WDeathblow : Page, IAnimaPage
	{
		LocalBook LB;
		IDeathblow Deathblow;

		private WDeathblow()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public WDeathblow( IDeathblow Deathblow )
			: this()
		{
			this.Deathblow = Deathblow;

			LB = Deathblow.GetParser();
			LayoutRoot.DataContext = LB;
		}

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 0, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30, 350, 0, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		private void SetTemplate()
		{
			LayoutRoot.RenderTransform = new TranslateTransform();
		}

		public async void Blow()
		{
			if ( LB.CanProcess )
			{
				await ItemProcessor.ProcessLocal( LB );
			}

			if ( LB.ProcessSuccess )
			{
				Deathblow.Register();

				ControlFrame.Instance.NavigateTo(
					PageId.BOOK_INFO_VIEW
					, () => new BookInfoView( Deathblow.GetBook() )
				);
			}
		}
	}
}
