// Room.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A BuildRoom is a kind of zone which can be built by creatures.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Room : Zone
    {
        protected static int Counter = 0;

        public bool wasDeserialized = false;

        public Room()
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

        public List<Voxel> Designations { get; set; }

        public bool IsBuilt { get; set; }
        public RoomData RoomData { get; set; }

        [JsonIgnore]
        public WorldGUIObject GUIObject { get; set; }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            wasDeserialized = true;
        }


        public virtual void OnBuilt()
        {
        }


        public virtual void CreateGUIObjects()
        {
        }

        public List<Body> GetComponentsInRoom()
        {
            var toReturn = new List<Body>();
            var components = new HashSet<Body>();
            BoundingBox box = GetBoundingBox();
            box.Max += new Vector3(0, 0, 2);
            PlayState.ComponentManager.CollisionManager.GetObjectsIntersecting(GetBoundingBox(), components,
                CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

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

            for (int i = 0; i < Designations.Count; i++)
            {
                Voxel v = Designations[i];
                float d = (v.Position - worldCoordinate).LengthSquared();

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