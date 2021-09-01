using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public static class VoxelUpdateHookTest
    {
        [VoxelUpdateHook("TEST")]
        private static void _hook(VoxelHandle Voxel, WorldManager World)
        {
            Voxel.Type = Library.EnumerateVoxelTypes().SelectRandom();
        }
    }
}
