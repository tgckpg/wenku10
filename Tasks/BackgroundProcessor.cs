using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.System.Diagnostics;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Logging.Handler;

using GR.CompositeElement;
using GR.Config;
using GR.GFlow;
using GR.Model.Book.Spider;
using GR.Model.ListItem;
using GR.Model.Pages;
using GR.Resources;
using GR.Settings;
using GR.Storage;

namespace Tasks
{
	using ProcList = global::GFlow.Controls.GFProcedureList;
	using ResGFlow = GFlow.Resources.Shared;

	public sealed class BackgroundProcessor : IBackgroundTask
	{
		private static readonly string ID = typeof( BackgroundProcessor ).Name;

		public static BackgroundProcessor Instance { get; private set; }

		public int TaskInterval { get { return XReg.Parameter( "Interval" ).GetSaveInt( "val" ); } }

		private const string TASK_MAIN = "UpdateTaskTrigger";
		private const string TASK_RETRY = "RetryTaskTrigger";
		private const string ENTRY_POINT = "Tasks.BackgroundProcessor";

		private bool CanBackground = false;
		private bool Retrying = false;
		private int MaxRetry = 3;

		private static volatile string ActiveTask = "";

		private string TASK_START { get { return Retrying ? "retry-start" : "task-start"; } }
		private string TASK_END { get { return Retrying ? "retry-end" : "task-end"; } }

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

		private async Task Init()
		{
			Worker.BackgroundOnly = true;

			if ( Properties.ENABLE_SYSTEM_LOG )
			{
				new FileSystemLog( FileLinks.ROOT_LOG + ( Retrying ? FileLinks.LOG_BGTASK_RETRY : FileLinks.LOG_BGTASK_UPDATE ) );
				Logger.Log( ID, "BackgroundTask init, mode: " + ( Retrying ? "Retry" : "Update" ), LogType.INFO );
			}

			THttpRequest.UA = string.Format( AppKeys.UA, AppSettings.SimpVersion );
			ResGFlow.CreateRequest = ( x ) => new THttpRequest( x );
			ProcList.Register( "EXTRACT", "/Resources:ProcEXTRACT", typeof( GrimoireExtractor ) );
			ProcList.Register( "MARK", "/Resources:ProcMARK", typeof( GrimoireMarker ) );
			ProcList.Register( "LIST", "/Resources:ProcLIST", typeof( GrimoireListLoader ) );
			ProcList.Register( "TRANSLATE", "/Resources:ProcTRANSLATE", typeof( TongWenTang ) );

			Shared.Conv = new GR.Model.Text.TranslationAPI();
			await Shared.Conv.InitContextTranslator();
		}

		public async void Run( IBackgroundTaskInstance taskInstance )
		{
			lock ( ActiveTask )
			{
				uint PID = ProcessDiagnosticInfo.GetForCurrentProcess().ProcessId;
				if ( !string.IsNullOrEmpty( ActiveTask ) )
				{
					Logger.Log( ID, "Another Task is already running: " + ActiveTask + " | " + PID, LogType.INFO );
					taskInstance.GetDeferral().Complete();
					return;
				}

				ActiveTask = string.Format( "{0}: {1}", taskInstance.Task.Name, PID );
			}

			Deferral = taskInstance.GetDeferral();

			// Associate a cancellation handler with the background task.
			taskInstance.Canceled += new BackgroundTaskCanceledEventHandler( OnCanceled );

			try
			{
				Retrying = ( taskInstance.Task.Name == TASK_RETRY );

				await Init();
				using ( CanvasDevice = Image.CreateCanvasDevice() )
				{
					await UpdateSpiders();
				}
			}
			finally
			{
				ActiveTask = "";
				Deferral.Complete();
			}
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
				if ( BTask.Value.Name == TASK_MAIN )
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

			Builder.Name = TASK_MAIN;
			Builder.TaskEntryPoint = ENTRY_POINT;
			Builder.SetTrigger( MinuteTrigger );
			Builder.AddCondition( new SystemCondition( SystemConditionType.InternetAvailable ) );

			BackgroundTaskRegistration task = Builder.Register();
		}

		private void CreateUpdateTaskTrigger()
		{
			foreach ( KeyValuePair<Guid, IBackgroundTaskRegistration> BTask in BackgroundTaskRegistration.AllTasks )
			{
				if ( BTask.Value.Name == TASK_MAIN ) return;
			}

			UpdateTaskInterval( 420 );
		}

		private void CreateRetryTimer()
		{
			foreach ( KeyValuePair<Guid, IBackgroundTaskRegistration> BTask in BackgroundTaskRegistration.AllTasks )
			{
				if ( BTask.Value.Name == TASK_RETRY ) return;
			}

			// Use the shortest interval as this is a retry, one shot
			TimeTrigger MinuteTrigger = new TimeTrigger( 15, false );
			BackgroundTaskBuilder Builder = new BackgroundTaskBuilder();

			Builder.Name = TASK_RETRY;
			Builder.TaskEntryPoint = ENTRY_POINT;
			Builder.SetTrigger( MinuteTrigger );
			Builder.AddCondition( new SystemCondition( SystemConditionType.InternetAvailable ) );

			BackgroundTaskRegistration task = Builder.Register();
		}

		private async Task UpdateSpiders()
		{
			try
			{
				XReg.SetParameter( TASK_START, CustomAnchor.TimeKey );
				XReg.Save();

				IEnumerable<XParameter> Updates;
				List<string> Exists = new List<string>();

				if ( Retrying )
				{
					Updates = XReg.Parameters( AppKeys.BTASK_SPIDER ).Where( x =>
					{
						int r = x.GetSaveInt( AppKeys.BTASK_RETRY );
						return 0 < r && r < MaxRetry;
					});
				}
				else
				{
					Updates = XReg.Parameters( AppKeys.BTASK_SPIDER ).Where( x => {
						int r = x.GetSaveInt( AppKeys.BTASK_RETRY );
						if ( r == 0 || MaxRetry <= r )
						{
							return true;
						}
						else
						{
							// Consider Retry Timer dead if LastUpdate is 20 < minutes
							DateTime LastRun = DateTime.FromFileTimeUtc( x.GetSaveLong( AppKeys.LBS_TIME ) );
							return 30 < DateTime.Now.Subtract( LastRun ).TotalMinutes;
						}
					} );
				}

				foreach ( XParameter UpdateParam in Updates )
				{
					string TileId = UpdateParam.GetValue( "tileId" );

					if ( !SecondaryTile.Exists( TileId ) )
					{
						UpdateParam.SetValue( new XKey[] {
							new XKey( AppKeys.SYS_EXCEPTION, "App Tile is missing" )
							, CustomAnchor.TimeKey
						} );
						XReg.SetParameter( UpdateParam );
						continue;
					}

					string[] Keys = UpdateParam.Id.Split( '.' );

					if( Keys.Length != 3 )
					{
						XReg.RemoveParameter( UpdateParam.Id );
						continue;
					}

					SpiderBook SBook = await SpiderBook.CreateSAsync( Keys[ 0 ], Keys[ 2 ], null );
					if ( !SBook.CanProcess )
					{
						XReg.RemoveParameter( UpdateParam.Id );
						continue;
					}

					SBook.MarkUnprocessed();

					string OHash = null, NHash = null;
					SBook.GetBook()?.Entry.Meta.TryGetValue( "TOCHash", out OHash );

					await ItemProcessor.ProcessLocal( SBook );

					if ( SBook.ProcessSuccess )
					{
						BookInstruction NBook = SBook.GetBook();
						NBook?.Entry.Meta.TryGetValue( "TOCHash", out NHash );

						if ( NBook.Packed == true && ( NBook.NeedUpdate || OHash != NHash ) )
						{
							await LiveTileService.UpdateTile( CanvasDevice, NBook, TileId );
						}

						UpdateParam.SetValue( new XKey[] {
							new XKey( AppKeys.SYS_EXCEPTION, false )
							, new XKey( AppKeys.BTASK_RETRY, 0 )
							, CustomAnchor.TimeKey
						} );
					}
					else
					{
						CreateRetryTimer();

						int NRetries = UpdateParam.GetSaveInt( AppKeys.BTASK_RETRY );
						UpdateParam.SetValue( new XKey[]
						{
							new XKey( AppKeys.SYS_EXCEPTION, true )
							, new XKey( AppKeys.BTASK_RETRY, NRetries + 1 )
							, CustomAnchor.TimeKey
						} );
					}

					XReg.SetParameter( UpdateParam );
					XReg.Save();
				}

				XReg.SetParameter( TASK_END, CustomAnchor.TimeKey );
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
