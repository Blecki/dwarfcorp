using System;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    ///<summary>
    /// Intended to be a smaller memory footprint representation
    /// that can be passed around. It mainly exists to represent
    /// empty voxels.
    ///</summary>
    public class VoxelRef : IEquatable<VoxelRef>
    {
        public Point3 ChunkID { get; set; }
        public Vector3 WorldPosition { get; set; }
        public Vector3 GridPosition { get; set; }
        public string TypeName { get; set; }
        public bool IsValid;

        public override int GetHashCode()
        {
            return (int) WorldPosition.X ^ (int) WorldPosition.Y ^ (int) WorldPosition.Z;
        }

        public bool Equals(VoxelRef other)
        {
            return other.ChunkID.Equals(ChunkID)
                   && (int) (GridPosition.X) == (int) (other.GridPosition.X)
                   && (int) (GridPosition.Y) == (int) (other.GridPosition.Y)
                   && (int) (GridPosition.Z) == (int) (other.GridPosition.Z);
        }

        public override bool Equals(object obj)
        {
            if(obj is VoxelRef)
            {
                return Equals((VoxelRef) obj);
            }
            else
            {
                return false;
            }
        }

        public BoundingBox GetBoundingBox()
        {
            BoundingBox toReturn = new BoundingBox();
            toReturn.Min = WorldPosition;
            toReturn.Max = WorldPosition + new Vector3(1, 1, 1);
            return toReturn;
        }

        public Voxel CreateEmptyVoxel(ChunkManager manager)
        {
            Voxel emptyVox = new Voxel(WorldPosition, VoxelLibrary.emptyType, null, false);
            emptyVox.Chunk = manager.ChunkData.ChunkMap[ChunkID];

            return emptyVox;
        }

        public Voxel GetVoxel(bool reconstruct)
        {
            ChunkManager manager = PlayState.ChunkManager;
            if(!manager.ChunkData.ChunkMap.ContainsKey(ChunkID))
            {
                return null;
            }
            else if(manager.ChunkData.ChunkMap[ChunkID].IsCellValid((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z))
            {
                Voxel vox = manager.ChunkData.ChunkMap[ChunkID].VoxelGrid[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z];
                if(!reconstruct)
                {
                    return vox;
                }
                else
                {
                    return vox ?? CreateEmptyVoxel(manager);
                }
            }
            else
            {
                return null;
            }
        }

        public WaterCell GetWater(ChunkManager manager)
        {
            if(!manager.ChunkData.ChunkMap.ContainsKey(ChunkID))
            {
                return null;
            }
            VoxelChunk chunk = manager.ChunkData.ChunkMap[ChunkID];
            return chunk.IsCellValid((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z) ? chunk.Water[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z] : null;
        }

        public byte GetWaterLevel(ChunkManager manager)
        {
            if(!manager.ChunkData.ChunkMap.ContainsKey(ChunkID))
            {
                return 0;
            }
            else if(manager.ChunkData.ChunkMap[ChunkID].IsCellValid((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z))
            {
                return manager.ChunkData.ChunkMap[ChunkID].Water[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z].WaterLevel;
            }
            else
            {
                return 0;
            }
        }

        public void SetWaterLevel(ChunkManager manager, byte level)
        {
            if(!manager.ChunkData.ChunkMap.ContainsKey(ChunkID))
            {
                return;
            }
            else if(manager.ChunkData.ChunkMap[ChunkID].IsCellValid((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z))
            {
                manager.ChunkData.ChunkMap[ChunkID].Water[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z].WaterLevel = level;
            }
            else
            {
                return;
            }
        }

        public void AddWaterLevel(ChunkManager manager, byte level)
        {
            if(!manager.ChunkData.ChunkMap.ContainsKey(ChunkID))
            {
                return;
            }
            else if(manager.ChunkData.ChunkMap[ChunkID].IsCellValid((int) GridPosition.X, (int) GridPosition.Y, (int) GridPosition.Z))
            {
                int amount = manager.ChunkData.ChunkMap[ChunkID].Water[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z].WaterLevel + level;
                manager.ChunkData.ChunkMap[ChunkID].Water[(int) GridPosition.X][(int) GridPosition.Y][(int) GridPosition.Z].WaterLevel = (byte) (Math.Min(amount, 255));
            }
            else
            {
                return;
            }
        }
    }

}