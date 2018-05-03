using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

namespace wenku10.Pages.Dialogs
{
	public sealed partial class Rename : ContentDialog
	{
		private INamable NamingTarget;

		public bool Canceled = true;

		public string Placeholder
		{
			set
			{
				NewName.PlaceholderText = value;
			}
		}

		private Rename()
		{
			this.InitializeComponent();
			StringResources stx = StringResources.Load( "Message", "ContextMenu" );
			PrimaryButtonText = stx.Str( "OK" );
			SecondaryButtonText = stx.Str( "Cancel" );

			TitleBlock.Text = stx.Text( "Rename", "ContextMenu" );
		}

		// For Activator.CreateInstance
		public Rename( INamable Target ) : this( Target, null, false ) { }
		public Rename( INamable Target, string Title ) : this( Target, Title, false ) { }

		public Rename( INamable Target, string Title, bool ReadOnly )
			: this()
		{
			NamingTarget = Target;
			NewName.Text = Target.Name;
			NewName.IsReadOnly = ReadOnly;

			NewName.SelectAll();

			if ( !string.IsNullOrEmpty( Title ) )
			{
				TitleBlock.Text = Title;
			}
		}

		private void OnKeyDown( object sender, KeyRoutedEventArgs e )
		{
			if ( e.Key == Windows.System.VirtualKey.Enter ) Confirmed();
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs e )
		{
			e.Cancel = true;
			Confirmed();
		}

		private async void Confirmed()
		{
			string NName = NewName.Text.Trim();
			string Error = "";

			if ( string.IsNullOrEmpty( NName ) )
			{
				Error = "Value cannot be empty!";
			}
			else
			{
				try
				{
					NamingTarget.Name = NName;
					Canceled = false;
					this.Hide();
					return;
				}
				catch ( Exception ex )
				{
					Error = ex.Message;
				}
			}

			if ( Error != "" )
			{
				MessageDialog Msg = new MessageDialog( Error );
				// Should NOT use Popups.ShowDialog because it closes the rename dialog
				// Making the caller await step thru, causing undesired behaviour
				await Msg.ShowAsync();
				NewName.Focus( FocusState.Keyboard );
			}
		}

	}
}
