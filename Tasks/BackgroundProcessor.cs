using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Config;
using wenku8.Model.Book;
using wenku8.Model.Book.Spider;
using wenku8.Model.ListItem;
using wenku8.Model.Pages;
using wenku8.Resources;
using wenku8.Settings;
using wenku8.Storage;

namespace Tasks
{
    using ResTaotu = libtaotu.Resources.Shared;

    public sealed class BackgroundProcessor : IBackgroundTask
    {
        public static BackgroundProcessor Instance { get; private set; }

        public int TaskInterval { get { return XReg.Parameter( "Interval" ).GetSaveInt( "val" ); } }

        private const string TASK_NAME = "UpdateTaskTrigger";
        private const string ENTRY_POINT = "Tasks.BackgroundProcessor";

        private bool CanBackground = false;

        private BackgroundTaskDeferral Deferral;
        private XRegistry XReg;
        private IDisposable CanvasDevice;

        public BackgroundProcessor()
        {
            if ( Shared.Storage == null )
            {
                Shared.Storage = new GeneralStorage();
                XRegistry.AStorage = Shared.Storage;
            }

            XReg = new XRegistry( "<tasks />", FileLinks.ROOT_SETTING + FileLinks.TASKS );
        }

        private void Init()
        {
            THttpRequest.UA = string.Format( AppKeys.UA, AppSettings.SimpVersion );
            ResTaotu.SetExtractor( typeof( TasksExtractor ) );
            ResTaotu.SetMarker( typeof( TasksMarker ) );
            ResTaotu.SetListLoader( typeof( TasksListLoader ) );
            ResTaotu.CreateRequest = ( x ) => new THttpRequest( x );
        }

        public async void Run( IBackgroundTaskInstance taskInstance )
        {
            Deferral = taskInstance.GetDeferral();

            // Associate a cancellation handler with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler( OnCanceled );

            Init();
            using ( CanvasDevice = Image.CreateCanvasDevice() )
            {
                await UpdateSpiders();
            }

            Deferral.Complete();
        }

        public async void CreateTileUpdateForBookSpider( string BookId, string TileId )
        {
            if ( !CanBackground )
            {
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    "Background Task Manager"
                    , "Backgorund Task is disabled. Unable to create tile update" ) );
                return;
            }

            XParameter BookParam = new XParameter( BookId );
            BookParam.SetValue( new XKey( "tileId", TileId ) );
            BookParam.SetValue( new XKey( "spider", true ) );

            XReg.SetParameter( BookParam );

            XReg.Save();
        }

        public void ClearTileStatus( string Id )
        {
            XParameter TileParam = XReg.Parameter( Id );
            if ( TileParam == null ) return;

            string TileId = TileParam.GetValue( "tileId" );
            try
            {
                TileUpdater Updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile( TileId );
                Updater.Clear();
                Updater.EnableNotificationQueue( false );
            }
            catch ( Exception ) { }
        }

        public static async void AcquireBackgroundPriviledge()
        {
            Instance = new BackgroundProcessor();

            BackgroundAccessStatus Status = await BackgroundExecutionManager.RequestAccessAsync();
            switch ( Status )
            {
                case BackgroundAccessStatus.AlwaysAllowed:
                case BackgroundAccessStatus.AllowedSubjectToSystemPolicy:

                #pragma warning disable 0618
                case BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity:
                case BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity:
                #pragma warning restore 0618

                    Instance.CanBackground = true;
                    Instance.CreateUpdateTaskTrigger();
                    return;
            }

        }

        public void UpdateTaskInterval( uint Minutes )
        {
            foreach ( KeyValuePair<Guid, IBackgroundTaskRegistration> BTask in BackgroundTaskRegistration.AllTasks )
            {
                if ( BTask.Value.Name == TASK_NAME )
                {
                    BTask.Value.Unregister( false );
                    break;
                }
            }

#if DEBUG
            Minutes = Math.Max( 15, Minutes );
#else
            Minutes = Math.Max( 180, Minutes );
#endif
            XParameter IntParam = XReg.Parameter( "Interval" );
            XReg.SetParameter( "Interval", new XKey( "val", Minutes ) );
            XReg.Save();

            TimeTrigger MinuteTrigger = new TimeTrigger( Minutes, false );
            BackgroundTaskBuilder Builder = new BackgroundTaskBuilder();

            Builder.Name = TASK_NAME;
            Builder.TaskEntryPoint = ENTRY_POINT;
            Builder.SetTrigger( MinuteTrigger );

            BackgroundTaskRegistration task = Builder.Register();
        }

        private void CreateUpdateTaskTrigger()
        {
            foreach ( KeyValuePair<Guid, IBackgroundTaskRegistration> BTask in BackgroundTaskRegistration.AllTasks )
            {
                if ( BTask.Value.Name == TASK_NAME ) return;
            }

            UpdateTaskInterval( 420 );
        }

        private async Task UpdateSpiders()
        {
            try
            {
                XReg.SetParameter( "task-start", BookStorage.TimeKey );
                XReg.Save();

                XParameter[] Updates = XReg.Parameters( "spider" );
                List<string> Exists = new List<string>();

                foreach ( XParameter UpdateParam in Updates )
                {
                    string TileId = UpdateParam.GetValue( "tileId" );

                    if ( !SecondaryTile.Exists( TileId ) )
                    {
                        UpdateParam.SetValue( new XKey[] {
                            new XKey( AppKeys.SYS_EXCEPTION, "App Tile is missing" )
                            , BookStorage.TimeKey
                        } );
                        XReg.SetParameter( UpdateParam );
                        continue;
                    }

                    SpiderBook SBook = await SpiderBook.CreateAsyncSpider( UpdateParam.Id );
                    if ( !SBook.CanProcess )
                    {
                        XReg.RemoveParameter( UpdateParam.Id );
                        continue;
                    }

                    await ItemProcessor.ProcessLocal( SBook );
                    BookInstruction Book = SBook.GetBook();

                    if ( Book.Packed == true )
                    {
                        string OHash = Shared.Storage.GetString( Book.TOCDatePath );
                        await Book.SaveTOC( Book.GetVolumes().Cast<SVolume>() );
                        string NHash = wenku8.System.Utils.Md5( Shared.Storage.GetBytes( Book.TOCPath ).AsBuffer() );

                        if ( OHash != NHash )
                        {
                            Shared.Storage.WriteString( Book.TOCDatePath, NHash );
                            await LiveTileService.UpdateTile( CanvasDevice, Book, TileId );
                        }
                    }

                    UpdateParam.SetValue( new XKey[] {
                        new XKey( AppKeys.SYS_EXCEPTION, false )
                        , BookStorage.TimeKey
                    } );

                    XReg.SetParameter( UpdateParam );
                    XReg.Save();
                }

                XReg.SetParameter( "task-end", BookStorage.TimeKey );
                XReg.Save();
            }
            catch ( Exception ex )
            {
                try
                {
                    XReg.SetParameter( AppKeys.SYS_EXCEPTION, new XKey( AppKeys.SYS_MESSAGE, ex.Message ) );
                    XReg.Save();
                }
                catch ( Exception ) { }
            }
        }

        private void OnCanceled( IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason )
        {
            if ( Deferral != null ) Deferral.Complete();
        }

    }
}