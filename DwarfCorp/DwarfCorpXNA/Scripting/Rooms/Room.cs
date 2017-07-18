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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
        public List<TemporaryVoxelHandle> Designations;
        public bool IsBuilt;
        public RoomData RoomData;
        private static int Counter = 0;

        public bool wasDeserialized = false;

        public Room() : base()
        {
            
        }

        // Todo: Kill unused parameter? Seems to be to distinquish between two ways of creating
        // a room where arguments are same types.
        public Room(
            bool designation,
            IEnumerable<TemporaryVoxelHandle> designations, 
            RoomData data,
            WorldManager world,
            Faction faction) :
            base(data.Name + " " + Counter, world, faction)
        {
            RoomData = data;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomData.FloorType);
            Counter++;
            Designations = designations.ToList();
            IsBuilt = false;
        }


        public Room(
            IEnumerable<TemporaryVoxelHandle> voxels, 
            RoomData data, 
            WorldManager world, 
            Faction faction) :
            base(data.Name + " " + Counter, world, faction)
        {
            RoomData = data;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomData.FloorType);

            Designations = new List<TemporaryVoxelHandle>();
            Counter++;

            IsBuilt = true;
            foreach (var voxel in voxels)
            {
                AddVoxel(voxel);
            }
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            wasDeserialized = true;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomData.FloorType);
        }


        public virtual void OnBuilt()
        {
            
        }

        public List<Body> GetComponentsInRoom()
        {
            List<Body> toReturn = new List<Body>();
            HashSet<IBoundedObject> components = new HashSet<IBoundedObject>();
            BoundingBox box = GetBoundingBox();
            box.Max += new Vector3(0, 0, 2);
            World.CollisionManager.GetObjectsIntersecting(GetBoundingBox(), components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            toReturn.AddRange(components.Where(o => o is Body).Select(o => o as Body));

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
                var v = Designations[i];
                float d = (v.Coordinate.ToVector3() - worldCoordinate).LengthSquared();

                if(d < closestDist)
                {
                    closestDist = d;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        public virtual void Update()
        {
            
        }
    }

}
