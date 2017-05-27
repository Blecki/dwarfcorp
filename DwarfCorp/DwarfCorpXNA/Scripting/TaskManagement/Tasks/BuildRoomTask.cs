// BuildRoomTask.cs
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
    internal class BuildRoomTask : Task
    {
        public BuildRoomOrder Zone;

        public BuildRoomTask()
        {
            Priority = PriorityType.Medium;
        }

        public BuildRoomTask(BuildRoomOrder zone)
        {
            Name = "Build Room " + zone.ToBuild.RoomData.Name + zone.ToBuild.ID;
            Zone = zone;
            Priority = PriorityType.Medium;
        }

        public override Task Clone()
        {
            return new BuildRoomTask(Zone);
        }

        public override Act CreateScript(Creature creature)
        {
            return new BuildRoomAct(creature.AI, Zone);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (Zone == null) ? 1000 : 1.0f;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return Zone != null && !Zone.IsBuilt;
        }

    }

}