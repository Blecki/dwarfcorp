// BuildRoomAct.cs
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

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item with a particular tag, and then puts it into a build zone
    /// for a BuildRoom. (This is used to construct rooms)
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class BuildRoomAct : CompoundCreatureAct
    {
        public BuildRoomOrder BuildRoom { get; set; }
        public List<Quantitiy<Resource.ResourceTags>> Resources { get; set; } 

        public IEnumerable<Status> SetTargetVoxelFromRoom(BuildRoomOrder buildRoom, string target)
        {
            if (buildRoom.VoxelOrders.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                var closestVoxel = VoxelHandle.InvalidHandle;
                float closestDist = float.MaxValue;
                foreach (var order in buildRoom.VoxelOrders)
                {
                    float dist = (order.Voxel.WorldPosition - Agent.Position).LengthSquared();

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestVoxel = order.Voxel;
                    }
                }

                Agent.Blackboard.SetData(target, closestVoxel);
                yield return Status.Success;
            }
        }

        public Act SetTargetVoxelFromRoomAct(BuildRoomOrder buildRoom, string target)
        {
            return new Wrap(() => SetTargetVoxelFromRoom(buildRoom, target));
        }

        public IEnumerable<Status> IsRoomBuildOrder(BuildRoomOrder buildRooom)
        {
            yield return Creature.Faction.RoomBuilder.BuildDesignations.Contains(buildRooom) ? Status.Success : Status.Fail;
        }


        public BuildRoomAct()
        {

        }

        public BuildRoomAct(CreatureAI agent, BuildRoomOrder buildRoom) :
            base(agent)
        {
            Name = "Build BuildRoom " + buildRoom.ToString();
            Resources = buildRoom.ListRequiredResources();

            Tree = new Sequence(new GetResourcesAct(Agent, Resources),
                new Sequence(
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    SetTargetVoxelFromRoomAct(buildRoom, "ActionVoxel"),
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    new GoToNamedVoxelAct("ActionVoxel", PlanAct.PlanType.Adjacent, Agent),
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    new Wrap(() => Creature.HitAndWait(buildRoom.VoxelOrders.Count * 0.5f / agent.Stats.BuildSpeed, true, () => buildRoom.GetBoundingBox().Center(), ContentPaths.Audio.Oscar.sfx_ic_dwarf_craft, () => !buildRoom.IsBuilt && !buildRoom.IsDestroyed)),
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    new PlaceRoomResourcesAct(Agent, buildRoom, Resources)
                    , new Wrap(Creature.RestockAll)) | new Wrap(Creature.RestockAll)
                );
        }

    }



}