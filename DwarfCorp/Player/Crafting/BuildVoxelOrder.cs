using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    /// This designation specifies that a given voxel from a given BuildRoom should be built.
    /// A BuildRoom build designation is actually a colletion of these.
    /// </summary>
    public class BuildVoxelOrder
    {
        public Zone ToBuild;
        public VoxelHandle Voxel;
        public BuildZoneOrder Order;

        public BuildVoxelOrder(BuildZoneOrder Order, Zone ToBuild, VoxelHandle Voxel)
        {
            this.Order = Order;
            this.ToBuild = ToBuild;
            this.Voxel = Voxel;
        }
    }
}