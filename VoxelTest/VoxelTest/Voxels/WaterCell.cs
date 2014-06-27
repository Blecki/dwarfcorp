using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// The equivalent of a voxel, but for storing liquids.
    /// </summary>
    public class WaterCell
    {
        public byte WaterLevel = 0;
        public Vector3 FluidFlow = Vector3.Zero;
        public Vector3 FlowAccel = Vector3.Zero;
        public bool HasChanged = false;
        public bool IsFalling = false;
        public LiquidType Type = LiquidType.None;
    }

}