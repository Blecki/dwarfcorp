using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A move action is a link between two voxels and a type of motion
    /// used to get between them.
    /// </summary>
    public struct MoveAction
    {
        /// <summary> The destination voxel of the motion </summary>
        public TemporaryVoxelHandle DestinationVoxel { get; set; }
        /// <summary> The type of motion applied to get to the voxel </summary>
        public MoveType MoveType { get; set; }
        /// <summary> The offset between the start and destination </summary>
        public Vector3 Diff { get; set; }
        /// <summary> And object to interact with to get between the start and destination </summary>
        public GameComponent InteractObject { get; set; }

        /// <summary>
        /// For climbing, this is the voxel the dwarf climbed on.
        /// </summary>
        public TemporaryVoxelHandle ActionVoxel { get; set; }

        public TemporaryVoxelHandle SourceVoxel { get; set; }
    }
}
