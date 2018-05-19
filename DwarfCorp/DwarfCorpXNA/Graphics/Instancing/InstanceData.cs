using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// An instance data represents a single instantiation of an object model
    /// at a given location, with a given color.
    /// </summary>
    public class InstanceData
    {
        public Matrix Transform { get; set; }
        public Color LightRamp { get; set; }
        public uint ID { get; set; }
        private static uint maxID = 0;
        public bool ShouldDraw { get; set; }
        public float Depth { get; set; }
        public Color SelectionBufferColor { get; set; }
        public Color VertexColorTint { get; set; }
        public InstanceData(Matrix world, Color colour, bool shouldDraw)
        {
            Transform = world;
            LightRamp = colour;
            ID = maxID;
            maxID++;
            ShouldDraw = shouldDraw;
            Depth = 0.0f;
            SelectionBufferColor = Color.Black;
            VertexColorTint = Color.White;
        }
    }

}