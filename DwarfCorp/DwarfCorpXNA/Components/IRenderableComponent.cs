using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public interface IRenderableComponent
    {
        void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater);

        BoundingBox GetBoundingBox();

        bool IsVisible { get; }

        bool IsAboveCullPlane { get; }

        Matrix GlobalTransform { get; }

        bool FrustrumCull { get; }
    }
}
