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

		private global::GR.Settings.Layout.BookInfoView Conf_BookInfoView;
		private global::GR.Settings.Layout.ContentReader Conf_ContentReader;

		private bool TemplateSet = false;
		public Layout()
		{
			this.InitializeComponent();

			SetTemplate();
		}

		public void SetTemplate()
		{
			// ContentReader
			Conf_ContentReader = new global::GR.Settings.Layout.ContentReader();

			// BookInfoView
			Conf_BookInfoView = new global::GR.Settings.Layout.BookInfoView();
			LayoutToggles();

			TemplateSet = true;
		}

		private void LayoutToggles()
		{
			TogBInFlo.IsOn = Conf_BookInfoView.IsRightToLeft;
			TogTOCAlign.IsOn = Conf_BookInfoView.HorizontalTOC;
			TogCAlign.IsOn = Conf_ContentReader.IsHorizontal;
			TogContFlo.IsOn = Conf_ContentReader.IsRightToLeft;

			TogPageClick.IsOn = !GRConfig.ContentReader.ReadingAnchor;
			TogDoubleTap.IsOn = GRConfig.ContentReader.DoubleTap;
			TogEmbedIllus.IsOn = GRConfig.ContentReader.EmbedIllus;
		}

		#region BookInfoView
		private void Toggled_BFlow( object sender, RoutedEventArgs e )
		{
			Conf_BookInfoView.IsRightToLeft = ( ( ToggleSwitch ) sender ).IsOn;
		}

		private void Toggled_TOCAlign( object sender, RoutedEventArgs e )
		{
			Conf_BookInfoView.HorizontalTOC = TogTOCAlign.IsOn;
		}
		#endregion

		#region ContentReader
		private void Toggled_CAlign( object sender, RoutedEventArgs e )
		{
			Conf_ContentReader.IsHorizontal = TogCAlign.IsOn;
		}

		private void Toggled_CFlow( object sender, RoutedEventArgs e )
		{
			Conf_ContentReader.IsRightToLeft = TogContFlo.IsOn;
		}

		private void Toggled_EmbedIllus( object sender, RoutedEventArgs e )
		{
			GRConfig.ContentReader.EmbedIllus = TogEmbedIllus.IsOn;
		}

		private async void Toggled_PageClick( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;

			if ( TogPageClick.IsOn && GRConfig.ContentReader.ReadingAnchor )
			{
				StringResources stx = new StringResources( "Settings" );
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
		#endregion
	}
}