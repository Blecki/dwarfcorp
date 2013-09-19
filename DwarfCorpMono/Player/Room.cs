using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{




    public class Room : Zone
    {
        public List<VoxelRef> Voxels { get; set; }
        public List<VoxelRef> Designations { get; set; }
        public List<BoxPrimitive> OriginalPrimitives { get; set; }
        public ChunkManager Chunks { get; set; }
        public List<GameComponent> Components { get; set; }
        public bool IsBuilt { get; set; }
        public RoomType RoomType { get; set; }
        private static int Counter = 0;

        public Room(bool designation, List<VoxelRef> designations, RoomType type,ChunkManager chunks) :
            base(type.Name + " " + Counter)
        {
            Voxels = new List<VoxelRef>();
            RoomType = type;
            Chunks = chunks;
            OriginalPrimitives = new List<BoxPrimitive>();

            Components = new List<GameComponent>();
            Counter++;
            Designations = designations;
            IsBuilt = false;
        }

        public Room(List<VoxelRef> voxels, RoomType type, ChunkManager chunks) :
            base(type.Name + " " + Counter)
        {
            Voxels = voxels;
            RoomType = type;

            OriginalPrimitives = new List<BoxPrimitive>();
            Chunks = chunks;

            Components = new List<GameComponent>();
            Designations = new List<VoxelRef>();
            Counter++;
            foreach (VoxelRef voxelRef in voxels)
            {
                Voxel voxel = voxelRef.GetVoxel(chunks, true);

                if (voxel != null)
                {
                    OriginalPrimitives.Add(voxel.Primitive);
                    Designations.Add(voxelRef);
                    if (voxelRef.TypeName != "empty")
                    {
                        voxel.Primitive = BoxPrimitive.RetextureTop(voxel.Primitive, chunks.Graphics, RoomType.FloorTexture);
                        voxel.Chunk.ShouldRebuild = true;
                    }
                }
            }

            IsBuilt = true;

        }

        public bool IsFull()
        {
            return false;
        }

        public void AddVoxel(VoxelRef voxelRef)
        {
            Voxel voxel = voxelRef.GetVoxel(Chunks, true);

            if (voxel != null)
            {
                OriginalPrimitives.Add(voxel.Primitive);

                if (voxelRef.TypeName != "empty")
                {
                    voxel.Primitive = BoxPrimitive.RetextureTop(voxel.Primitive, Chunks.Graphics, RoomType.FloorTexture);
                }

                voxel.Chunk.ShouldRebuild = true;
                voxel.Chunk.NotifyChangedComponents();
            }

            Voxels.Add(voxelRef);
        }

        public void ClearRoom()
        {
            int i = 0;
            foreach (VoxelRef voxelRef in Voxels)
            {
                Voxel voxel = voxelRef.GetVoxel(Chunks, true);
                if (voxel != null)
                {
                    voxel.Primitive = OriginalPrimitives[i];
                }
                i++;
            }

            foreach (GameComponent entity in Components)
            {
                entity.Die();
            }
        }

        public bool IsInRoom(VoxelRef voxel)
        {
            return Voxels.Contains(voxel);
        }

        public List<LocatableComponent> GetComponentsInRoom()
        {
            List<LocatableComponent> toReturn = new List<LocatableComponent>();
            HashSet<LocatableComponent> components = new HashSet<LocatableComponent>();
            BoundingBox box = GetBoundingBox();
            box.Max += new Vector3(0, 0, 2);
            LocatableComponent.m_octree.Root.GetComponentsIntersecting<LocatableComponent>(GetBoundingBox(), components);

            toReturn.AddRange(components);

            return toReturn;
        }

        public List<LocatableComponent> GetComponentsInRoomContainingTag(string tag)
        {
            List<LocatableComponent> inRoom = GetComponentsInRoom();
            List<LocatableComponent> toReturn = new List<LocatableComponent>();

            foreach(LocatableComponent c in inRoom)
            {
                if (c.Tags.Contains(tag))
                {
                    toReturn.Add(c);
                }
            }

            return toReturn;
        }

        public bool IsInRoom(Vector3 worldCoordinate)
        {
            foreach (VoxelRef v in Voxels)
            {
                if (worldCoordinate.X > v.WorldPosition.X && worldCoordinate.Y > v.WorldPosition.Y && worldCoordinate.Z > v.WorldPosition.Z
                    && worldCoordinate.X < v.WorldPosition.Z + 1.0f && worldCoordinate.Y < v.WorldPosition.Y + 1.0f && worldCoordinate.Z < v.WorldPosition.Z + 1.0f)
                {
                    return true;
                }
            }

            return false;
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> elements = new List<BoundingBox>();

            foreach (VoxelRef v in Voxels)
            {
                elements.Add(v.GetBoundingBox());
            }

            return LinearMathHelpers.GetBoundingBox(elements);
        }

        public int GetClosestDesignationTo(Vector3 worldCoordinate)
        {
            float closestDist = 99999;
            int closestIndex = -1;

            for (int i = 0; i < Designations.Count; i++)
            {
                VoxelRef v = Designations[i];
                float d = (v.WorldPosition - worldCoordinate).LengthSquared();

                if (d < closestDist)
                {
                    closestDist = d;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        public int GetClosestVoxelTo(Vector3 worldCoordinate)
        {
            float closestDist = 99999;
            int closestIndex = -1;

            for(int i = 0; i < Voxels.Count; i++)
            {
                VoxelRef v = Voxels[i];
                float d = (v.WorldPosition - worldCoordinate).LengthSquared();

                if (d < closestDist)
                {
                    closestDist = d;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }


    }
}
