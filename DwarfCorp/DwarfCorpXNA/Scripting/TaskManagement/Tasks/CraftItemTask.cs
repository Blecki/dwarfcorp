// CraftItemTask.cs
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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class CraftItemTask : Task
    {
        public CraftItem CraftType { get; set; }
        public Voxel Voxel { get; set; }

        public CraftItemTask()
        {
            Priority = PriorityType.Low;
            AutoRetry = true;
        }

        public CraftItemTask(Voxel voxel, CraftItem type)
        {
            Name = "Craft item " + voxel.GridPosition + " " + voxel.ChunkID.X + " " + voxel.ChunkID.Y + " " + voxel.ChunkID.Z;
            Voxel = voxel;
            CraftType = type;
            Priority = PriorityType.Low;
            AutoRetry = true;
        }

        public override Task Clone()
        {
            Voxel v = new Voxel(new Point3(Voxel.GridPosition), Voxel.Chunk);
            return new CraftItemTask(v, CraftType);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return Voxel == null || !CanBuild(agent) ? 1000 : (agent.AI.Position - Voxel.Position).LengthSquared();
        }

        public override Act CreateScript(Creature creature)
        {
            return new CraftItemAct(creature.AI, Voxel, CraftType);
        }

        public override bool ShouldRetry(Creature agent)
        {
            if (!agent.Faction.CraftBuilder.IsDesignation(Voxel))
            {
                return false;
            }

            return true;
        }

        public override bool IsFeasible(Creature agent)
        {
            return CanBuild(agent);
        }

        public bool CanBuild(Creature agent)
        {
            if (!agent.Faction.CraftBuilder.IsDesignation(Voxel))
            {
                return false;
            }
            if (!String.IsNullOrEmpty(CraftType.CraftLocation))
            {
                var nearestBuildLocation = agent.Faction.FindNearestItemWithTags(CraftType.CraftLocation, Vector3.Zero, false);

                if (nearestBuildLocation == null)
                    return false;
            }

            foreach (var resourceAmount in CraftType.RequiredResources)
                if (agent.Faction.ListResourcesWithTag(resourceAmount.ResourceType).Count == 0)
                    return false;

            return true;
        }

    }


    class CraftResourceTask : Task
    {
        public CraftItem Item { get; set; }
        private string noise;
        public CraftResourceTask(CraftItem selectedResource)
        {
            Item = selectedResource;
            Name = "Craft " + Item.Name;
            Priority = PriorityType.Low;

            noise = ResourceLibrary.GetResourceByName(Item.ResourceCreated).Tags.Contains(Resource.ResourceTags.Edible)
                ? "Cook"
                : "Craft";
            AutoRetry = true;
        }

        public override Act CreateScript(Creature creature)
        {
            return new ForLoop(new CraftItemAct(creature.AI, Item)
            {
                Noise = noise
            }, this.Item.NumRepeats, false);
        }

        public override Task Clone()
        {
            return new CraftResourceTask(Item);
        }
    }

}