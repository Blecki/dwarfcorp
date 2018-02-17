// PutItemWithTagTask.cs
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
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should find an item with the specified
    /// tags and put it in a given zone.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class PutItemWithTagTask : Task
    {
        public Zone Zone;
        public TagList Tags;

        public PutItemWithTagTask()
        {
            Priority = PriorityType.Low;
        }

        public PutItemWithTagTask(TagList tags, Zone zone)
        {
            Name = "Put Item with tag: " + tags + " in zone " + zone.ID;
            Tags = tags;
            Zone = zone;
            Priority = PriorityType.Low;
        }

        public override Act CreateScript(Creature creature)
        {
            Room room = Zone as Room;
            if(room == null)
            {
                return null;
            }

            if(!creature.Faction.RoomBuilder.IsBuildDesignation(room))
            {
                return null;
            }

            BuildVoxelOrder voxVoxelOrder = creature.Faction.RoomBuilder.GetBuildDesignation(room);

            if(voxVoxelOrder == null)
            {
                return null;
            }

            BuildRoomOrder designation = voxVoxelOrder.Order;

            if (designation == null)
                return null;

            return  new BuildRoomAct(creature.AI, designation);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (Zone == null) ? 1000 : 1.0f;
        }
    }

}