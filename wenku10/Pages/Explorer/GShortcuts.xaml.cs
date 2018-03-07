using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Linq;

using GR.Database.Contexts;
using GR.Database.Models;
using GR.DataSources;
using GR.Effects;
using GR.Model.Interfaces;
using GR.Model.Section;

namespace wenku10.Pages.Explorer
{
	sealed partial class GShortcuts : Page, IAnimaPage, IDisposable
	{
		public IEnumerable<GRViewSource> AvailableWidgets { get; private set; }
		ObservableCollection<WidgetView> Widgets;

		public GShortcuts()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			MainContents.ItemTemplateSelector = new TemplateSel() { Resources = Resources };
			Widgets = new ObservableCollection<WidgetView>();
		}

		public async void LoadWidgets()
		{
			WidgetConfig[] WCs;
			using ( SettingsContext Db = new SettingsContext() )
			{
				WCs = Db.WidgetConfigs.OrderBy( x => x.Id ).ToList().Select( x => x.Conf ).ToArray();
			}

			MainContents.ItemsSource = Widgets;

			if ( WCs.Any() )
			{
				foreach ( WidgetConfig WC in WCs )
				{
					GRViewSource GVS = AvailableWidgets.FirstOrDefault( x => x.DataSource.ConfigId == WC.TargetType );
					if ( GVS != null )
					{
						WidgetView WView = new WidgetView( GVS );
						await WView.ConfigureAsync( WC );
						_AddWidget( WView );
					}
				}
			}
			else
			{
				foreach ( GRViewSource GVS in AvailableWidgets )
				{
					WidgetView WView = new WidgetView( GVS );
					await WView.ConfigureAsync();
					_AddWidget( WView );
				}
			}
		}

		public void AddWidget( WidgetView WView )
		{
			if ( _AddWidget( WView ) )
			{
				SaveConfigs();
			}
		}

		private bool _AddWidget( WidgetView WView )
		{
			if ( WView.Conf.Enable )
			{
				Widgets.Add( WView );
				return true;
			}
			return false;
		}

		private void SaveConfigs()
		{
			using ( SettingsContext Db = new SettingsContext() )
			{
				int l = Widgets.Count;
				Db.WidgetConfigs.RemoveRange( Db.WidgetConfigs.Where( x => l < x.Id ) );

				Widgets.ExecEach( ( x, i ) =>
				{
					GRWidgetConfig WConf = Db.WidgetConfigs.Find( i + 1 );
					if ( WConf == null )
					{
						WConf = new GRWidgetConfig() { Id = i + 1, Conf = x.Conf };
						Db.WidgetConfigs.Add( WConf );
					}
					else
					{
						WConf.Conf = x.Conf;
						Db.WidgetConfigs.Update( WConf );
					}
				} );

				Db.SaveChanges();
			}
		}

		public void RegisterWidgets( IEnumerable<GRViewSource> GVSs )
		{
			AvailableWidgets = GVSs;
		}

		public void Dispose()
		{
			MainContents.ItemsSource = null;
			Widgets.Clear();
		}

		Storyboard AnimaStory = new Storyboard();
		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350 );
			AnimaStory.Begin();
			await Task.Delay( 500 );
		}

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1, 350 );

			AnimaStory.Begin();
			await Task.Delay( 500 );
		}

		private class TemplateSel : DataTemplateSelector
		{
			public ResourceDictionary Resources { get; set; }

			protected override DataTemplate SelectTemplateCore( object Item, DependencyObject container )
			{
				if ( Item is WidgetView WItem )
				{
					string Name = WItem.TemplateName;
					if ( !Resources.ContainsKey( Name ) )
						Name = "NoSuchWidget";

					return ( DataTemplate ) Resources[ Name ];
				}

				return null;
			}
		}

		private async void WidgetRename_Click( object sender, RoutedEventArgs e )
		{
			FrameworkElement Elem = ( FrameworkElement ) sender;
			if ( Elem.DataContext is WidgetView WV )
			{
				string OName = WV.Name;
				Dialogs.Rename RenameDialog = new Dialogs.Rename( WV );
				await Popups.ShowDialog( RenameDialog );

				if( OName != WV.Name )
					SaveConfigs();
			}
		}

		private void DeleteWidget_Click( object sender, RoutedEventArgs e )
		{
			FrameworkElement Elem = ( FrameworkElement ) sender;
			if ( Elem.DataContext is WidgetView WV )
			{
				Widgets.Remove( WV );
				SaveConfigs();
			}
		}

		private void MoveUpWidget_Click( object sender, RoutedEventArgs e )
		{
			FrameworkElement Elem = ( FrameworkElement ) sender;
			if ( Elem.DataContext is WidgetView WV )
			{
				int i = Widgets.IndexOf( WV );
				if ( 0 < i )
				{
					Widgets.Move( i, i - 1 );
					SaveConfigs();
				}
			}
		}

		private void MoveDownWidget_Click( object sender, RoutedEventArgs e )
		{
			FrameworkElement Elem = ( FrameworkElement ) sender;
			if ( Elem.DataContext is WidgetView WV )
			{
				int i = Widgets.IndexOf( WV );
				if ( i < ( Widgets.Count - 1 ) )
				{
					Widgets.Move( i, i + 1 );
					SaveConfigs();
				}
			}
		}

	}
}