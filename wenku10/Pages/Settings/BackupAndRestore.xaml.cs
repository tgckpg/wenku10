using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using GR.GSystem;
using GR.MigrationOps;

namespace wenku10.Pages.Settings
{
	public sealed partial class BackupAndRestore : Page
	{
		MigrationManager MigrationMgr;

		public BackupAndRestore()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			Bootstrap.LogInstance?.Stop();
			MigrationMgr = new MigrationManager();
			LayoutRoot.DataContext = MigrationMgr;
		}

		bool Working = false;

		private async void Backup_Click( object sender, RoutedEventArgs e )
		{
			if ( Working ) return;
			Working = true;
			await Task.Run( MigrationMgr.Backup );
			Working = false;
		}

		private async void Migrate_Click( object sender, RoutedEventArgs e )
		{
			if ( Working ) return;
			Working = true;
			await Task.Run( MigrationMgr.Migrate );
			Working = false;
		}

		private async void Restore_Click( object sender, RoutedEventArgs e )
		{
			if ( Working ) return;
			Working = true;
			await Task.Run( MigrationMgr.Restore );
			Working = false;
		}

	}
}