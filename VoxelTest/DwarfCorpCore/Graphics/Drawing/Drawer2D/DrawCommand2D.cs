using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// 2D draw commands are queued up by the 2D draw manager, and then rendered
    /// to the screen. They exist mostly for conveience to draw things like lines, boxes, etc.
    /// in the update thread.
    /// </summary>
    public abstract class DrawCommand2D
    {
        public abstract void Render(SpriteBatch batch, Camera camera, Viewport viewport);
    }

}