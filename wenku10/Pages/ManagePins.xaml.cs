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

using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Config;
using wenku8.Effects;
using wenku8.Model.Book;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Pages;
using wenku8.Resources;
using wenku8.Storage;

namespace wenku10.Pages
{
    sealed partial class ManagePins : Page, ICmdControls, IAnimaPage
    {
        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get ; private set; }

        PinManager PM;
        PinRecord SelectedRecord;

        volatile bool ActionBlocked = false;

        public ManagePins()
        {
            this.InitializeComponent();
            SetTemplate();
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

            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0 );
            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30 );

            AnimaStory.Begin();
            await Task.Delay( 350 );
        }
        #endregion

        private void SetTemplate()
        {
            LayoutRoot.RenderTransform = new TranslateTransform();

            PM = new PinManager();
            PinList.ItemsSource = PM.GetPinRecords();

            InitAppBar();
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "AppBar" );

            if ( Properties.ENABLE_ONEDRIVE )
            {
                AppBarButtonEx OneDriveButton = UIAliases.CreateAppBarBtnEx( SegoeMDL2.Cloud, stx.Text( "Sync" ) );
                ButtonOperation SyncOp = new ButtonOperation( OneDriveButton );

                SyncOp.SetOp( OneDriveRsync );
                MajorControls = new ICommandBarElement[] { OneDriveButton };
            }
            else
            {
                MajorControls = new ICommandBarElement[] { };
            }
        }

        private async Task OneDriveRsync()
        {
            await PM.SyncSettings();
            PinList.ItemsSource = PM.GetPinRecords();
        }

        private async void PinToStart( object sender, RoutedEventArgs e )
        {
            if ( ActionBlocked ) return;
            ActionBlocked = true;

            BookItem Book = await ItemProcessor.GetBookFromId( SelectedRecord.Id );
            await PageProcessor.PinToStart( Book );

            ActionBlocked = false;
        }

        private void RemovePin( object sender, RoutedEventArgs e )
        {
        }

        private void ShowContextMenu( object sender, RightTappedRoutedEventArgs e )
        {
            FrameworkElement Elem = ( FrameworkElement ) sender;
            FlyoutBase.ShowAttachedFlyout( Elem );

            SelectedRecord = ( PinRecord ) Elem.DataContext;
        }
    }
}