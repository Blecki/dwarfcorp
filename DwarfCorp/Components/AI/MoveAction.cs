using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using DwarfCorp.Rail;

namespace DwarfCorp
{
    public enum VehicleTypes
    {
        None,
        Rail,
    }

    public struct MoveState : IEquatable<MoveState>
    {
        public VoxelHandle Voxel;
        public VehicleTypes VehicleType;
        public Rail.RailEntity Rail; // Todo: Ditch this, use the Tag below.
        public Rail.RailEntity PrevRail;
        public Object Tag;
        
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
            hashCode = hashCode * -1521134295 + Voxel.GetHashCode();
            if (PrevRail != null) hashCode = hashCode * -1521134295 + PrevRail.GetHashCode();
            if (Rail != null) hashCode = hashCode * -1521134295 + Rail.GetHashCode();

            return hashCode;
        }

        public static bool operator ==(MoveState A, MoveState B)
        {
            return A.Voxel == B.Voxel
                && A.VehicleType == B.VehicleType
                && Object.ReferenceEquals(A.Rail, B.Rail)
                && Object.ReferenceEquals(A.PrevRail, B.PrevRail)
                && ((A.Tag == null && B.Tag == null) || Object.ReferenceEquals(A.Tag, B.Tag));
        }

        public static bool operator !=(MoveState A, MoveState B)
        {
            return A.Voxel != B.Voxel
                || A.VehicleType != B.VehicleType
                || !Object.ReferenceEquals(A.Rail, B.Rail)
                || !Object.ReferenceEquals(A.PrevRail, B.PrevRail)
                || !((A.Tag == null && B.Tag == null) || Object.ReferenceEquals(A.Tag, B.Tag));
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
        public Vector3 Diff { get; set; } // Todo: Can be ditched?
        /// <summary> And object to interact with to get between the start and destination </summary>
        public GameComponent InteractObject { get; set; }

        public float CostMultiplier;

        /// <summary>
        /// For climbing, this is the voxel the dwarf climbed on.
        /// </summary>
        public VoxelHandle ActionVoxel { get; set; } // Todo: Can be ditched?

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
                    VehicleType = SourceState.VehicleType,
                    Rail = SourceState.Rail,
                    PrevRail = SourceState.PrevRail,
                    Tag = SourceState.Tag
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
                    VehicleType = DestinationState.VehicleType,
                    Rail = DestinationState.Rail,
                    PrevRail = DestinationState.PrevRail,
                    Tag = DestinationState.Tag
                };
            }
        }
    }
}
