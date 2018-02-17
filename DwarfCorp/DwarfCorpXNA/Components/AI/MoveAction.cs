using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using DwarfCorp.Rail;

namespace DwarfCorp
{
    public struct VehicleState : IEquatable<VehicleState>
    {
        public Rail.RailEntity Rail;

        public override bool Equals(object obj)
        {
            if (!(obj is VehicleState)) return false;
            return this == (VehicleState)obj;
        }

        public bool Equals(VehicleState other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            var hashCode = -7133344;
            hashCode = hashCode * -1521134295 + EqualityComparer<RailEntity>.Default.GetHashCode(Rail);
            return hashCode;
        }

        public static bool operator ==(VehicleState A, VehicleState B)
        {
            return A.Rail == B.Rail;
        }

        public static bool operator !=(VehicleState A, VehicleState B)
        {
            return A.Rail != B.Rail;
        }

        public bool IsRidingVehicle
        {
            get
            {
                return Rail != null;
            }
        }
    }

    public struct MoveState : IEquatable<MoveState>
    {
        public VoxelHandle Voxel;
        public VehicleState VehicleState;

        public static MoveState InvalidState
        {
            get
            {
                return new MoveState
                {
                    Voxel = VoxelHandle.InvalidHandle
                };
            }
        }

        public bool IsValid
        {
            get { return Voxel.IsValid; }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MoveState)) return false;
            return this == (MoveState)obj;
        }

        public bool Equals(MoveState other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            var hashCode = 68161903;
            hashCode = hashCode * -1521134295 + EqualityComparer<VoxelHandle>.Default.GetHashCode(Voxel);
            hashCode = hashCode * -1521134295 + EqualityComparer<VehicleState>.Default.GetHashCode(VehicleState);
            return hashCode;
        }

        public static bool operator ==(MoveState A, MoveState B)
        {
            return A.Voxel == B.Voxel && A.VehicleState == B.VehicleState;
        }

        public static bool operator !=(MoveState A, MoveState B)
        {
            return A.Voxel != B.Voxel || A.VehicleState != B.VehicleState;
        }

    }

    /// <summary>
    /// A move action is a link between two voxels and a type of motion
    /// used to get between them.
    /// </summary>
    public struct MoveAction
    {
        /// <summary> The destination voxel of the motion </summary>
        public MoveState DestinationState { get; set; }
        /// <summary> The type of motion applied to get to the voxel </summary>
        public MoveType MoveType { get; set; }
        /// <summary> The offset between the start and destination </summary>
        public Vector3 Diff { get; set; }
        /// <summary> And object to interact with to get between the start and destination </summary>
        public GameComponent InteractObject { get; set; }

        /// <summary>
        /// For climbing, this is the voxel the dwarf climbed on.
        /// </summary>
        public VoxelHandle ActionVoxel { get; set; }

        public MoveState SourceState { get; set; }

        public VoxelHandle SourceVoxel
        {
            get
            {
                return SourceState.Voxel;
            }
            set
            {
                SourceState = new MoveState()
                {
                    Voxel = value,
                    VehicleState = SourceState.VehicleState
                };
            }
        }

        public VoxelHandle DestinationVoxel
        {
            get
            {
                return DestinationState.Voxel;
            }
            set
            {
                DestinationState = new MoveState()
                {
                    Voxel = value,
                    VehicleState = DestinationState.VehicleState
                };
            }
        }
    }
}
