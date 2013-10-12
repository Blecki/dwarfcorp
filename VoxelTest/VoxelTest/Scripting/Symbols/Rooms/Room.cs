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
        public List<VoxelRef> Designations { get; set; }
        public List<GameComponent> Components { get; set; }
        public bool IsBuilt { get; set; }
        public RoomType RoomType { get; set; }
        private static int Counter = 0;

        public Room(bool designation, List<VoxelRef> designations, RoomType type,ChunkManager chunks) :
            base(type.Name + " " + Counter, chunks)
        {
            RoomType = type;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomType.FloorType);
            Chunks = chunks;
            Components = new List<GameComponent>();
            Counter++;
            Designations = designations;
            IsBuilt = false;
        }

        public Room(List<VoxelRef> voxels, RoomType type, ChunkManager chunks) :
            base(type.Name + " " + Counter, chunks)
        {
            RoomType = type;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomType.FloorType);
            Chunks = chunks;

            Components = new List<GameComponent>();
            Designations = new List<VoxelRef>();
            Counter++;

            IsBuilt = true;
            foreach (VoxelRef voxel in voxels)
            {
                AddVoxel(voxel);
            }


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


    }
}
