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

        public bool IsRoomBuildOrder(BuildRoomOrder buildRooom)
        {
            return Creature.Faction.RoomBuilder.BuildDesignations.Contains(buildRooom);
        }


        public BuildRoomAct()
        {

        }

        public IEnumerable<Act.Status> PutResources()
        {
            if (BuildRoom.HasResources)
            {
                yield return Act.Status.Success;
            }

            if (BuildRoom.ResourcesReservedFor == Agent)
            {
                Agent.Creature.Inventory.Remove(Resources, Inventory.RestockType.None);
                BuildRoom.HasResources = true;
            }
            yield return Act.Status.Success;
        }
        public IEnumerable<Status> WaitForResources()
        {
            if (BuildRoom.ResourcesReservedFor == Agent)
            {
                yield return Act.Status.Success;
                yield break;
            }

            while (!BuildRoom.HasResources)
            {
                if (BuildRoom.ResourcesReservedFor == null || BuildRoom.ResourcesReservedFor.IsDead)
                {
                    yield return Act.Status.Fail;
                    yield break;
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> Reserve()
        {
            if (BuildRoom.ResourcesReservedFor == null)
            {
                BuildRoom.ResourcesReservedFor = Agent;
                yield return Act.Status.Success;
                yield break;
            }
            yield return Act.Status.Fail;
        }

        private bool ValidResourceState()
        {
            return BuildRoom.HasResources || (BuildRoom.ResourcesReservedFor != null);
        }



        public IEnumerable<Act.Status> OnFailOrCancel()
        {
            if (BuildRoom.ResourcesReservedFor == Agent)
            {
                BuildRoom.ResourcesReservedFor = null;
            }
            foreach(var status in Creature.RestockAll())
            {

            }
            yield return Act.Status.Success;
        }

        public override void OnCanceled()
        {
            foreach(var status in OnFailOrCancel())
            {

            }

            base.OnCanceled(); 
        }


        public BuildRoomAct(CreatureAI agent, BuildRoomOrder buildRoom) :
            base(agent)
        {
            Name = "Build BuildRoom " + buildRoom.ToString();
            Resources = buildRoom.ListRequiredResources();
            BuildRoom = buildRoom;
            if (BuildRoom.ResourcesReservedFor != null && BuildRoom.ResourcesReservedFor.IsDead)
            {
                BuildRoom.ResourcesReservedFor = null;
            }

            Tree = new Sequence(new Select(new Domain(buildRoom.HasResources || buildRoom.ResourcesReservedFor != null, true), 
                                           new Domain(!buildRoom.HasResources && (buildRoom.ResourcesReservedFor == null || buildRoom.ResourcesReservedFor == Agent), new Sequence(new Wrap(Reserve), new GetResourcesAct(Agent, Resources))),
                                           new Domain(buildRoom.HasResources || buildRoom.ResourcesReservedFor != null, true)),
                new Domain(() => IsRoomBuildOrder(buildRoom) && !buildRoom.IsBuilt && !buildRoom.IsDestroyed && ValidResourceState(), 
                new Sequence(
                    SetTargetVoxelFromRoomAct(buildRoom, "ActionVoxel"),
                    new GoToNamedVoxelAct("ActionVoxel", PlanAct.PlanType.Adjacent, Agent),
                    new Wrap(PutResources),
                    new Wrap(WaitForResources) {  Name = "Wait for resources..."},
                    new Wrap(() => Creature.HitAndWait(true, () => 1.0f, () => buildRoom.BuildProgress,
                    () => buildRoom.BuildProgress += agent.Stats.BuildSpeed / buildRoom.VoxelOrders.Count * 0.5f,
                    () => MathFunctions.RandVector3Box(buildRoom.GetBoundingBox()), ContentPaths.Audio.Oscar.sfx_ic_dwarf_craft, () => !buildRoom.IsBuilt && !buildRoom.IsDestroyed))
                    { Name = "Build room.." },
                    new CreateRoomAct(Agent, buildRoom)
                    , new Wrap(OnFailOrCancel))) | new Wrap(OnFailOrCancel)
                );
        }

    }



}