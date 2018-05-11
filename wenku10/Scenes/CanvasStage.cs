using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;

using GR.Effects;

namespace wenku10.Scenes
{
	class CanvasStage : IDisposable
	{
		protected CanvasAnimatedControl _stage;
		public TextureLoader Textures { get; protected set; }

		public Size StageSize { get; private set; }
		public bool StageLoaded { get; private set; }
		public bool DeviceExist { get; private set; }

		protected List<IScene> Scenes;

		public CanvasStage( CanvasAnimatedControl Stage )
		{
			_stage = Stage;

			StageSize = Size.Empty;
			Scenes = new List<IScene>();

			Textures = new TextureLoader();
			Stage.CreateResources += Stage_CreateResources;

			Stage.GameLoopStarting += Stage_GameLoopStarting;
			Stage.GameLoopStopped += Stage_GameLoopStopped;

			Stage.Loaded += Stage_Loaded;
			Stage.Unloaded += Stage_Unloaded;
		}

		public CanvasStage( CanvasAnimatedControl Stage, TextureLoader SharedTextures )
			: this( Stage )
		{
			Stage.CreateResources -= Stage_CreateResources;
			Textures = SharedTextures;
		}

		public async void Add( IScene S )
		{
			await LoadSceneResources( S );

			lock ( Scenes )
			{
				Scenes.Add( S );
				if ( CanDraw() ) S.UpdateAssets( StageSize );
				if ( StageLoaded ) S.Enter();
			}
		}

		private Task LoadSceneResources( IScene S )
		{
			if ( !DeviceExist ) return Task.Delay( 0 );
			return ( S as ITextureScene )?.LoadTextures( _stage, Textures );
		}

		private bool CanDraw()
		{
			if ( !StageSize.Equals( _stage.Size ) )
			{
				StageSize = _stage.Size;
			}

			return !StageSize.IsZero();
		}

		public async void Insert( int Index, IScene S )
		{
			await LoadSceneResources( S );

			lock ( Scenes )
			{
				Scenes.Insert( Index, S );
				if ( CanDraw() ) S.UpdateAssets( StageSize );
				if ( StageLoaded ) S.Enter();
			}
		}

		public IEnumerable<T> GetScenes<T>() where T : IScene
		{
			return Scenes.Where( x => x is T ).Cast<T>();
		}

		public async Task Remove( Type SceneType )
		{
			IScene[] RmScenes;
			lock ( Scenes )
			{
				RmScenes = Scenes.Where( x => x.GetType().Equals( SceneType ) ).ToArray();
			}

			if ( RmScenes == null ) return;

			foreach ( IScene S in RmScenes )
			{
				if ( S is ISceneExitable ) await ( S as ISceneExitable ).Exit();
				lock ( Scenes )
				{
					Scenes.Remove( S );
					S.Dispose();
				}
			}
		}

		~CanvasStage()
		{
			Dispose();
		}

		virtual public void Dispose()
		{
			try
			{
				lock ( Scenes )
				{
					Scenes.ForEach( x => x.Dispose() );
					Scenes.Clear();
					Textures.Dispose();
				}

			}
			catch ( Exception ) { };
		}

		private void Stage_GameLoopStopped( ICanvasAnimatedControl sender, object args )
		{
			_stage.Draw -= Stage_Draw;
		}

		private void Stage_GameLoopStarting( ICanvasAnimatedControl sender, object args )
		{
			_stage.Draw += Stage_Draw;
		}

		private void Stage_Loaded( object sender, RoutedEventArgs e )
		{
			StageLoaded = true;
			lock ( Scenes )
			{
				Scenes.ForEach( x => x.Enter() );
			}

			_stage.SizeChanged += Stage_SizeChanged;
		}

		virtual protected void Stage_Unloaded( object sender, RoutedEventArgs e )
		{
			StageLoaded = false;
			_stage.Draw -= Stage_Draw;
			_stage.SizeChanged -= Stage_SizeChanged;
		}

		virtual protected void Stage_CreateResources( CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args )
		{
			DeviceExist = true;
			args.TrackAsyncAction( LoadTextures( sender ).AsAsyncAction() );
		}

		virtual protected async Task LoadTextures( CanvasAnimatedControl CC )
		{
			IScene[] TxScenes = Scenes.Where( x => x is ITextureScene ).ToArray();
			foreach ( ITextureScene S in TxScenes.Cast<ITextureScene>() )
			{
				await S.LoadTextures( CC, Textures );
			}

			if ( CanDraw() )
			{
				lock ( Scenes )
				{
					foreach ( IScene S in Scenes )
					{
						S.UpdateAssets( StageSize );
					}
				}
			}

		}

		virtual protected void Stage_SizeChanged( object sender, SizeChangedEventArgs e )
		{
			lock ( Scenes )
			{
				Scenes.ForEach( x => x.UpdateAssets( e.NewSize ) );
				StageSize = e.NewSize;
			}
		}

		virtual protected void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args )
		{
			using ( CanvasDrawingSession ds = args.DrawingSession )
			using ( CanvasSpriteBatch SBatch = ds.CreateSpriteBatch( CanvasSpriteSortMode.Bitmap ) )
			{
				lock ( Scenes )
				{
					Scenes.ForEach( x => x.Draw( ds, SBatch, Textures ) );
				}
			}
		}

	}
}