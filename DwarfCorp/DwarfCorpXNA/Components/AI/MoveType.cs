using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary> Describes the way in which a creature can move from one location to another </summary>
    public enum MoveType
    {
        /// <summary> Move along a horizontal surface </summary>
        Walk,
        /// <summary> Jump from one voxel to another. </summary>
        Jump,
        /// <summary> Climb up a climbable object </summary>
        Climb,
        /// <summary> Move through water </summary>
        Swim,
        /// <summary> Fall vertically through space </summary>
        Fall,
        /// <summary> Move from one empty voxel to another </summary>
        Fly,
        /// <summary> Attack a blocking object until it is destroyed </summary>
        DestroyObject,
        /// <summary> Move along a vertical surface. </summary>
        ClimbWalls,
        EnterVehicle,
        RideVehicle,
        ExitVehicle,
        Teleport
    }
}
