using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// An instance data represents a single instantiation of an object model
    /// at a given location, with a given color.
    /// </summary>
    // Todo: Doesn't need to know about the manager anymore.
    public class NewInstanceData
    {
        public string Type;
        public Rectangle SpriteBounds;
        public string TextureAsset;
        public Matrix Transform;
        public Color Color;
        public Color SelectionBufferColor;
        public Color VertexColorTint;

        public NewInstanceData(
            string Type,
            Matrix Transform,
            Color Color)
        {
            this.Type = Type;
            this.Transform = Transform;
            this.Color = Color;
            VertexColorTint = Color.White;
            SelectionBufferColor = Color.Black;
        }
    }

}