using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.UI.Xaml;
using GR.Effects;

namespace wenku10.Scenes
{
	interface ITextureScene : IScene
	{
		Task LoadTextures( CanvasAnimatedControl Canvas, TextureLoader Textures );
	}
}