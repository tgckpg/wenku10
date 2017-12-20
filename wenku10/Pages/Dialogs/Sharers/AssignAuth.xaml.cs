using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Loaders;

using GR.Model.ListItem;

namespace wenku10.Pages.Dialogs.Sharers
{
	using StringAuthManager = global::GR.GSystem.AuthManager<INameValue>;

	sealed partial class AssignAuth : ContentDialog
	{
		public INameValue SelectedAuth { get; set; }
		public bool Canceled { get; private set; }

		private StringAuthManager AuthMgr;

		public AssignAuth( StringAuthManager Mgr, string Title )
		{
			this.InitializeComponent();

			this.Title = Title;
			Canceled = true;
			AuthMgr = Mgr;

			StringResources stx = new StringResources( "Message" );

			PrimaryButtonText = stx.Str( "OK" );
			SecondaryButtonText = stx.Str( "Cancel" );

			SetTemplate();
		}

		private void SetTemplate()
		{
			Keys.DataContext = AuthMgr;
			PreSelectKey();
		}

		private void RSA_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "SelectedItem" ) Keys.SelectedItem = AuthMgr.SelectedItem;
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			SelectedAuth = ( INameValue ) Keys.SelectedItem;
			Canceled = false;
		}

		private void PreSelectKey( object sender, RoutedEventArgs e ) { PreSelectKey(); }

		private void PreSelectKey()
		{
			if ( !( AuthMgr == null || Keys == null ) )
				Keys.SelectedValue = AuthMgr.SelectedItem?.Value;
		}

	}
}