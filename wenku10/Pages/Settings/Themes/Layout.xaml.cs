using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using GR.Config;
using GR.Model.Interfaces;
using GR.Model.ListItem;

namespace wenku10.Pages.Settings.Themes
{
	public sealed partial class Layout : Page, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private bool TemplateSet = false;
		public Layout()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public void SetTemplate()
		{
			LayoutToggles();
			TemplateSet = true;
		}

		private void LayoutToggles()
		{
			TogPageClick.IsOn = !GRConfig.ContentReader.ReadingAnchor;
			TogDoubleTap.IsOn = GRConfig.ContentReader.DoubleTap;
			TogEmbedIllus.IsOn = GRConfig.ContentReader.EmbedIllus;
			TogCAlign.IsOn = GRConfig.ContentReader.IsHorizontal;
			TogContFlo.IsOn = GRConfig.ContentReader.IsRightToLeft;
		}

		private void Toggled_CAlign( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;
			GRConfig.ContentReader.IsHorizontal = TogCAlign.IsOn;
		}

		private void Toggled_CFlow( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;
			GRConfig.ContentReader.IsRightToLeft = TogContFlo.IsOn;
		}

		private void Toggled_EmbedIllus( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;
			GRConfig.ContentReader.EmbedIllus = TogEmbedIllus.IsOn;
		}

		private async void Toggled_PageClick( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;

			if ( TogPageClick.IsOn && GRConfig.ContentReader.ReadingAnchor )
			{
				StringResources stx = StringResources.Load( "Settings" );
				MessageDialog Msg = new MessageDialog( stx.Text( "Layout_ContentReader_UsePageClick_Warning" ), stx.Text( "Layout_ContentReader_UsePageClick" ) );

				Msg.Commands.Add(
					new UICommand( stx.Text( "Enabled" ) )
				);

				Msg.Commands.Add(
					new UICommand( stx.Text( "Disabled" ), ( x ) => TogPageClick.IsOn = false )
				);
				await Popups.ShowDialog( Msg );
			}

			GRConfig.ContentReader.ReadingAnchor = !TogPageClick.IsOn;

			if ( TogPageClick.IsOn )
			{
				GRConfig.ContentReader.DoubleTap = TogDoubleTap.IsOn = false;
			}
		}

		private void Toggled_DoubleTap( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;

			GRConfig.ContentReader.DoubleTap = TogDoubleTap.IsOn;

			if ( TogDoubleTap.IsOn )
			{
				GRConfig.ContentReader.ReadingAnchor = true;
				TogPageClick.IsOn = false;
			}
		}

	}
}