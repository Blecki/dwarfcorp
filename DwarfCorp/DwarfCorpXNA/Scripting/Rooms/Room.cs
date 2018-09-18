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
        public List<VoxelHandle> Designations;
        
        public bool IsBuilt;
        public RoomData RoomData;
        private static int Counter = 0;

        public bool wasDeserialized = false;

        public Room() : base()
        {
            
        }

        public Room(
            RoomData data, 
            WorldManager world, 
            Faction faction) :
            base(data.Name + " " + Counter, world, faction)
        {
            RoomData = data;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomData.FloorType);
            Designations = new List<VoxelHandle>();
            Counter++;
        }


        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            wasDeserialized = true;
            ReplacementType = VoxelLibrary.GetVoxelType(RoomData.FloorType);
        }

        public virtual void OnBuilt()
        {
            
        }

        public virtual void Update()
        {
            
        }
    }

}
