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

using Net.Astropenguin.Helpers;
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
using wenku8.Model.Loaders;

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

        private PinManager PM;
        private IEnumerable<PinRecord> CurrRecords;
        private PinRecord SelectedRecord;

        private volatile bool ActionBlocked = false;

        AppBarButton PinPolicyBtn;

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
            UpdatePinData();

            InitAppBar();
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "AppBar" );

            // Do nothing ( default ) / Ask / Pin Missing / Remove Missing
            PinPolicyBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.Pin, "Pin Policy" );
            PinPolicyBtn.Click += RotatePolicy;
            UpdatePinPolicy( stx );

            if ( Properties.ENABLE_ONEDRIVE )
            {
                AppBarButtonEx OneDriveButton = UIAliases.CreateAppBarBtnEx( SegoeMDL2.Cloud, stx.Text( "Sync" ) );
                ButtonOperation SyncOp = new ButtonOperation( OneDriveButton );

                SyncOp.SetOp( OneDriveRsync, false );
                MajorControls = new ICommandBarElement[] { PinPolicyBtn, OneDriveButton };
            }
            else
            {
                MajorControls = new ICommandBarElement[] { PinPolicyBtn };
            }
        }

        private void UpdatePinPolicy( StringResources stx )
        {
            switch ( PM.Policy )
            {
                case PinPolicy.DO_NOTHING:
                    ( ( FontIcon ) PinPolicyBtn.Icon ).Glyph = SegoeMDL2.BlockedLegacy;
                    PinPolicyBtn.Label = stx.Text( "PinPolicy_DoNothing" );
                    break;
                case PinPolicy.ASK:
                    ( ( FontIcon ) PinPolicyBtn.Icon ).Glyph = SegoeMDL2.Permissions;
                    PinPolicyBtn.Label = stx.Text( "PinPolicy_Ask" );
                    break;
                case PinPolicy.REMOVE_MISSING:
                    ( ( FontIcon ) PinPolicyBtn.Icon ).Glyph = SegoeMDL2.Delete;
                    PinPolicyBtn.Label = stx.Text( "PinPolicy_RemoveMissing" );
                    break;
                case PinPolicy.PIN_MISSING:
                    ( ( FontIcon ) PinPolicyBtn.Icon ).Glyph = SegoeMDL2.Pin;
                    PinPolicyBtn.Label = stx.Text( "PinPolicy_PinMissing" );
                    break;
            }
        }

        private void RotatePolicy( object sender, RoutedEventArgs e )
        {
            switch ( PM.Policy )
            {
                case PinPolicy.DO_NOTHING:
                    PM.Policy = PinPolicy.ASK;
                    break;
                case PinPolicy.ASK:
                    PM.Policy = PinPolicy.PIN_MISSING;
                    break;
                case PinPolicy.PIN_MISSING:
                    PM.Policy = PinPolicy.REMOVE_MISSING;
                    break;
                case PinPolicy.REMOVE_MISSING:
                    PM.Policy = PinPolicy.DO_NOTHING;
                    break;
            }

            UpdatePinPolicy( new StringResources( "AppBar" ) );
        }

        private async Task OneDriveRsync()
        {
            await PM.SyncSettings();
            UpdatePinData();
        }

        private void UpdatePinData()
        {
            SelectedRecord = null;
            CurrRecords = PM.GetPinRecords();
            PinList.ItemsSource = CurrRecords;
        }

        private async void PinToStart( object sender, RoutedEventArgs e )
        {
            if ( ActionBlocked ) return;
            ActionBlocked = true;

            await PinRecord( SelectedRecord );
            PM.Save();

            UpdatePinData();
            ActionBlocked = false;
        }

        private async void PinDevToStart( object sender, RoutedEventArgs e )
        {
            if ( ActionBlocked ) return;
            ActionBlocked = true;

            PinManager PM = new PinManager();
            PinRecord[] Records = CurrRecords.Where( x => x.DevId == SelectedRecord.DevId && 0 < x.TreeLevel ).ToArray();

            if ( 5 < Records.Length )
            {
                bool Canceled = true;
                StringResources stx = new StringResources( "Message" );
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    string.Format( stx.Str( "ConfirmMassPin" ), Records.Length )
                    , () => Canceled = false
                    , stx.Str( "Yes" ), stx.Str( "No" )
                ) );

                if ( Canceled )
                {
                    ActionBlocked = false;
                    return;
                }
            }

            foreach ( PinRecord Record in Records )
            {
                await PinRecord( Record );
            }

            PM.Save();
            UpdatePinData();
            ActionBlocked = false;
        }

        private void RemoveDev( object sender, RoutedEventArgs e )
        {
            PM.RemoveDev( SelectedRecord.DevId );
            UpdatePinData();
        }

        private async void RemovePin( object sender, RoutedEventArgs e )
        {
            if ( AppSettings.DeviceId == SelectedRecord.DevId )
            {
                PM.RemovePin( SelectedRecord.Id );
                UpdatePinData();
            }
            else
            {
                StringResources stx = new StringResources( "Message" );
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    stx.Str( "PinMgr_NoRemoteAction" )
                ) );
            }
        }

        private void ShowContextMenu( object sender, RightTappedRoutedEventArgs e )
        {
            FrameworkElement Elem = ( FrameworkElement ) sender;
            FlyoutBase.ShowAttachedFlyout( Elem );

            SelectedRecord = ( PinRecord ) Elem.DataContext;
        }

        private async Task PinRecord( PinRecord Record )
        {
            BookItem Book = await ItemProcessor.GetBookFromId( Record.Id );
            if ( Book == null ) return;

            TaskCompletionSource<bool> TCS = new TaskCompletionSource<bool>();
            BookLoader BL = new BookLoader( async ( b ) =>
            {
                if ( b != null )
                {
                    string TileId = await PageProcessor.PinToStart( Book );
                    if ( !string.IsNullOrEmpty( TileId ) )
                    {
                        PM.RegPin( b, TileId, false );
                    }
                }

                TCS.SetResult( true );
            } );

            BL.Load( Book );
            await TCS.Task;
        }

    }
}