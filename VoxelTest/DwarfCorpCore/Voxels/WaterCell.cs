using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// The equivalent of a voxel, but for storing liquids.
    /// </summary>
    public struct WaterCell
    {
        public byte WaterLevel;
        public bool HasChanged;
        public bool IsFalling;
        public LiquidType Type;
    }

}