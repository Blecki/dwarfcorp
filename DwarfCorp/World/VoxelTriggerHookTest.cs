using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public static class VoxelTriggerHookTest
    {
        [VoxelTriggerHook("TEST")]
        private static void _hook(VoxelEvent Event, WorldManager World)
        {
            if (Event.Type == VoxelEventType.SteppedOn)
                Event.Voxel.Type = Library.EnumerateVoxelTypes().SelectRandom();
        }
    }
}
