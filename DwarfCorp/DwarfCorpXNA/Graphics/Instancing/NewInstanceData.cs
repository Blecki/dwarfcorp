using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// An instance data represents a single instantiation of an object model
    /// at a given location, with a given color.
    /// </summary>
    public class NewInstanceData
    {
        public string Type;
        public Rectangle SpriteBounds;
        public string TextureAsset;
        public Matrix Transform;
        public Color LightRamp;
        public Color SelectionBufferColor;
        public Color VertexColorTint;

        // Todo: Cache the transformed sprite bounds as well?
        [JsonIgnore]
        public Gui.TileSheet AtlasCache = null;

        public NewInstanceData(
            string Type,
            Matrix Transform,
            Color LightRamp)
        {
            this.Type = Type;
            this.Transform = Transform;
            this.LightRamp = LightRamp;
            VertexColorTint = Color.White;
            SelectionBufferColor = Color.Black;
        }
    }

}