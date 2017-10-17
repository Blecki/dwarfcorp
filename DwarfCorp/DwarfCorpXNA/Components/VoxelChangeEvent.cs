using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public enum VoxelChangeEventType
    {
        VoxelTypeChanged,
        RampsChanged,
        // Todo: Revealed, etc.
    }

    public class VoxelChangeEvent
    {
        public VoxelChangeEventType Type;
        public VoxelHandle Voxel;
        public short OriginalVoxelType;
        public short NewVoxelType;
        public RampType OldRamps;
        public RampType NewRamps;
    }
}
