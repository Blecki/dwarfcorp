using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Elevators
{
    public static class Helper
    {
        public class ElevatorExit
        {
            public VoxelHandle OntoVoxel;
            public ElevatorShaft ShaftSegment;
        }

        public static IEnumerable<ElevatorExit> EnumerateExits(ElevatorStack Stack)
        {
            foreach (var segment in Stack.Pieces)
            {
                foreach (var neighborVoxel in VoxelHelpers.EnumerateManhattanNeighbors2D_Y(segment.GetContainingVoxel().Coordinate))
                {
                    var neighborHandle = new VoxelHandle(segment.Manager.World.ChunkManager, neighborVoxel);
                    if (neighborHandle.IsValid && neighborHandle.IsEmpty)
                    {
                        var below = neighborVoxel + new GlobalVoxelOffset(0, -1, 0);
                        var belowHandle = new VoxelHandle(segment.Manager.World.ChunkManager, below);
                        if (belowHandle.IsValid && !belowHandle.IsEmpty)
                            yield return new ElevatorExit
                            {
                                OntoVoxel = neighborHandle,
                                ShaftSegment = segment
                            };
                    }
                }
            }
        }
    }
}
