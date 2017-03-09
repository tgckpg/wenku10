using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

using Microsoft.Graphics.Canvas;

using wenku8.Effects;

namespace wenku10.Scenes
{
	interface IScene : IDisposable
	{
		void UpdateAssets( Size Sim );
		void Draw( CanvasDrawingSession ds, CanvasSpriteBatch SBatch, TextureLoader Textures );
	}
}