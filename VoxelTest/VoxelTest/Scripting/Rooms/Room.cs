using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A BuildRoom is a kind of zone which can be built by creatures.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Room : Zone
    {
        public List<Voxel> Designations { get; set; }

        public bool IsBuilt { get; set; }
        public RoomData RoomData { get; set; }
        protected static int Counter = 0;
        public WorldGUIObject GUIObject { get; set; }

        public Room() : base()
        {
            
        }

        public Room(bool designation, IEnumerable<Voxel> designations, RoomData data, ChunkManager chunks) :
            base(data.Name + " " + Counter, chunks)
        {
            RoomData = data;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomData.FloorType);
            Chunks = chunks;
            Counter++;
            Designations = designations.ToList();
            IsBuilt = false;
        }

        public Room(IEnumerable<Voxel> voxels, RoomData data, ChunkManager chunks) :
            base(data.Name + " " + Counter, chunks)
        {
            RoomData = data;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomData.FloorType);
            Chunks = chunks;

            Designations = new List<Voxel>();
            Counter++;

            IsBuilt = true;
            foreach (Voxel voxel in voxels)
            {
                AddVoxel(voxel);
            }
        }

        public virtual void OnBuilt()
        {
            
        }


        public List<Body> GetComponentsInRoom()
        {
            List<Body> toReturn = new List<Body>();
            HashSet<Body> components = new HashSet<Body>();
            BoundingBox box = GetBoundingBox();
            box.Max += new Vector3(0, 0, 2);
            PlayState.ComponentManager.CollisionManager.GetObjectsIntersecting(GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            toReturn.AddRange(components);

            return toReturn;
        }

        public List<Body> GetComponentsInRoomContainingTag(string tag)
        {
            List<Body> inRoom = GetComponentsInRoom();

            return inRoom.Where(c => c.Tags.Contains(tag)).ToList();
        }


        public int GetClosestDesignationTo(Vector3 worldCoordinate)
        {
            float closestDist = 99999;
            int closestIndex = -1;

            for(int i = 0; i < Designations.Count; i++)
            {
                Voxel v = Designations[i];
                float d = (v.Position - worldCoordinate).LengthSquared();

                if(d < closestDist)
                {
                    closestDist = d;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }
    }

}