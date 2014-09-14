using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// The equivalent of a voxel, but for storing liquids.
    /// </summary>
    public class WaterCell
    {
        public byte WaterLevel;
        public Vector3 FluidFlow;
        public Vector3 FlowAccel;
        public bool HasChanged;
        public bool IsFalling;
        public LiquidType Type;
    }

}