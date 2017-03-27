using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

using wenku8.Effects;

namespace wenku10.Scenes
{
	class CanvasStage : IDisposable
	{
		protected CanvasAnimatedControl _stage;
		public TextureLoader Textures { get; protected set; }

		public Size StageSize { get; private set; }

		protected List<IScene> Scenes;

		public CanvasStage( CanvasAnimatedControl Stage )
		{
			_stage = Stage;

			Scenes = new List<IScene>();

			Textures = new TextureLoader();
			Stage.CreateResources += Stage_CreateResources;

			Stage.GameLoopStarting += Stage_GameLoopStarting;
			Stage.GameLoopStopped += Stage_GameLoopStopped;

			Stage.SizeChanged += Stage_SizeChanged;
			Stage.Unloaded += Stage_Unloaded;
		}

		public CanvasStage( CanvasAnimatedControl Stage, TextureLoader SharedTextures )
			: this( Stage )
		{
			Stage.CreateResources -= Stage_CreateResources;
			Textures = SharedTextures;
		}

		public void Add( IScene S )
		{
			lock ( Scenes )
			{
				Scenes.Add( S );
				if ( StageSize != null ) S.UpdateAssets( StageSize );
			}
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
				lock ( Scenes ) Scenes.Remove( S );
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

		virtual protected void Stage_Unloaded( object sender, RoutedEventArgs e )
		{
			_stage.Draw -= Stage_Draw;
			_stage.SizeChanged -= Stage_SizeChanged;
		}

		virtual protected void Stage_CreateResources( CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args )
		{
			args.TrackAsyncAction( LoadTextures( sender ).AsAsyncAction() );
		}

		virtual protected async Task LoadTextures( CanvasAnimatedControl CC )
		{
			await Textures.Load( CC, Texture.Glitter, "Assets/glitter.dds" );
			await Textures.Load( CC, Texture.Circle, "Assets/circle.dds" );
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
			using ( CanvasSpriteBatch SBatch = ds.CreateSpriteBatch() )
			{
				lock ( Scenes )
				{
					Scenes.ForEach( x => x.Draw( ds, SBatch, Textures ) );
				}
			}
		}

	}
}