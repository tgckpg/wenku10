using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

using wenku8.Effects;
using wenku8.Effects.P2DFlow;
using wenku8.Effects.P2DFlow.ForceFields;

namespace wenku10.Scenes
{
    abstract class BasicScene : IDisposable
    {
        protected PFSimulator PFSim = new PFSimulator();

        protected bool ShowWireFrame = false;

        protected CanvasAnimatedControl Stage;
        public TextureLoader Textures { get; protected set; }

        protected const int Texture_Glitter = 1;
        protected const int Texture_Circle = 2;

        public BasicScene( CanvasAnimatedControl Stage )
        {
            this.Stage = Stage;

            Textures = new TextureLoader();
            Stage.CreateResources += Stage_CreateResources;

            Stage.GameLoopStarting += Stage_GameLoopStarting;
            Stage.GameLoopStopped += Stage_GameLoopStopped;

            Stage.SizeChanged += Stage_SizeChanged;
            Stage.Unloaded += Stage_Unloaded;
        }

        public BasicScene( CanvasAnimatedControl Stage, TextureLoader SharedTextures )
            :this( Stage )
        {
            Stage.CreateResources -= Stage_CreateResources;
            Textures = SharedTextures;
        }

        ~BasicScene()
        {
            Dispose();
        }

        virtual public void Dispose()
        {
            try
            {
                lock ( PFSim )
                {
                    Textures.Dispose();
                    PFSim.Reapers.Clear();
                    PFSim.Fields.Clear();
                    PFSim.Spawners.Clear();
                }

                PFSim = null;
            }
            catch ( Exception ) { };
        }

        private void Stage_GameLoopStopped( ICanvasAnimatedControl sender, object args )
        {
            Stage.Draw -= Stage_Draw;
        }

        private void Stage_GameLoopStarting( ICanvasAnimatedControl sender, object args )
        {
            Stage.Draw += Stage_Draw;
        }

        virtual protected void Stage_Unloaded( object sender, RoutedEventArgs e )
        {
            lock ( PFSim )
            {
                Stage.Draw -= Stage_Draw;
                Stage.SizeChanged -= Stage_SizeChanged;
            }
        }

        virtual protected void Stage_CreateResources( CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args )
        {
            args.TrackAsyncAction( LoadTextures( sender ).AsAsyncAction() );
        }

        virtual protected async Task LoadTextures( CanvasAnimatedControl CC )
        {
            await Textures.Load( CC, Texture_Glitter, "Assets/glitter.dds" );
            await Textures.Load( CC, Texture_Circle, "Assets/circle.dds" );
        }

        abstract public void Start();
        abstract protected void Stage_SizeChanged( object sender, SizeChangedEventArgs e );
        abstract protected void Stage_Draw( ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args );

        protected void DrawWireFrames( CanvasDrawingSession ds )
        {
#if DEBUG
            lock ( PFSim )
            {
                if ( ShowWireFrame )
                {
                    foreach ( IForceField IFF in PFSim.Fields )
                    {
                        IFF.WireFrame( ds );
                    }
                }

            }
#endif
        }

    }
}