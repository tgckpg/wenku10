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

using wenku8.Config;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;

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

		private global::wenku8.Settings.Layout.BookInfoView Conf_BookInfoView;
		private global::wenku8.Settings.Layout.MainPage Conf_MainPage;
		private global::wenku8.Settings.Layout.NavList Conf_NavList;
		private global::wenku8.Settings.Layout.ContentReader Conf_ContentReader;

		private bool TemplateSet = false;
		public Layout()
		{
			this.InitializeComponent();

			SetTemplate();
		}

		public void SetTemplate()
		{
			// MainPage
			CustomSection();

			// NavList
			Conf_NavList = new global::wenku8.Settings.Layout.NavList();

			// ContentReader
			Conf_ContentReader = new global::wenku8.Settings.Layout.ContentReader();

			// BookInfoView
			Conf_BookInfoView = new global::wenku8.Settings.Layout.BookInfoView();
			LayoutToggles();

			TemplateSet = true;
		}

		private void LayoutToggles()
		{
			TogBInFlo.IsOn = Conf_BookInfoView.IsRightToLeft;
			TogTOCAlign.IsOn = Conf_BookInfoView.HorizontalTOC;
			TogNAlign.IsOn = Conf_NavList.IsHorizontal;
			TogCAlign.IsOn = Conf_ContentReader.IsHorizontal;
			TogContFlo.IsOn = Conf_ContentReader.IsRightToLeft;

			TogPageClick.IsOn = !Properties.APPEARANCE_CONTENTREADER_ENABLEREADINGANCHOR;
			TogDoubleTap.IsOn = Properties.APPEARANCE_CONTENTREADER_ENABLEDOUBLETAP;
			TogEmbedIllus.IsOn = Properties.APPEARANCE_CONTENTREADER_EMBED_ILLUS;
		}

		// NavList
		private void Toggled_NAlign( object sender, RoutedEventArgs e )
		{
			Conf_NavList.IsHorizontal = TogNAlign.IsOn;
		}

		#region MainPage
		private void CustomSection()
		{
			Conf_MainPage = new global::wenku8.Settings.Layout.MainPage();
			SectionListContext.DataContext = Conf_MainPage;
			SectionChoices.SelectedValue = Conf_MainPage.SelectedSection.Desc2;
		}

		private void SectionList_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count() == 0 ) return;

			if ( Conf_MainPage.ChangeCustSection( ( ActiveItem ) e.AddedItems[ 0 ] ) )
			{
				ControlFrame.Instance.BackStack.Remove( PageId.W_NAV_SEL );
				ControlFrame.Instance.CommandMgr.UpdateCustBtn();
				ControlChanged?.Invoke( this );
			}
		}
		#endregion

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
			Properties.APPEARANCE_CONTENTREADER_EMBED_ILLUS = TogEmbedIllus.IsOn;
		}

		private async void Toggled_PageClick( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;

			if ( TogPageClick.IsOn && Properties.APPEARANCE_CONTENTREADER_ENABLEREADINGANCHOR )
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

			Properties.APPEARANCE_CONTENTREADER_ENABLEREADINGANCHOR = !TogPageClick.IsOn;

			if ( TogPageClick.IsOn )
			{
				Properties.APPEARANCE_CONTENTREADER_ENABLEDOUBLETAP = TogDoubleTap.IsOn = false;
			}
		}

		private void Toggled_DoubleTap( object sender, RoutedEventArgs e )
		{
			if ( !TemplateSet ) return;

			Properties.APPEARANCE_CONTENTREADER_ENABLEDOUBLETAP = TogDoubleTap.IsOn;

			if ( TogDoubleTap.IsOn )
			{
				Properties.APPEARANCE_CONTENTREADER_ENABLEREADINGANCHOR = true;
				TogPageClick.IsOn = false;
			}
		}
		#endregion
	}
}