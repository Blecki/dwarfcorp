using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public static class VoxelPool
    {


        public Voxel Allocate(Point3 Coordinate, VoxelChunk Chunk)
        {
            return new Voxel(Coordinate, Chunk);
        }

        public Voxel Allocate()
        {
            return new Voxel();
        }
    }
}
