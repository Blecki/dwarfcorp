using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public enum VoxelEventType
    {
        VoxelTypeChanged,
        RampsChanged,
        Explored,
        SteppedOn,
        // Todo: Revealed, etc.
    }

    public class VoxelEvent
    {
        public VoxelEventType Type;
        public VoxelHandle Voxel;
        public short OriginalVoxelType;
        public short NewVoxelType;
        public RampType OldRamps;
        public RampType NewRamps;
        public CreatureAI Creature;
    }
}
