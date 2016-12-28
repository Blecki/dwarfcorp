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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a compound creature act (behavior tree) that tells a creature how to
    ///     build a room.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BuildRoomAct : CompoundCreatureAct
    {
        public BuildRoomAct()
        {
        }

        /// <summary>
        ///     Tells a creature to build a room.
        /// </summary>
        /// <param name="agent">The creature we want to build the room.</param>
        /// <param name="buildRoom">The room to build.</param>
        public BuildRoomAct(CreatureAI agent, BuildRoomOrder buildRoom) :
            base(agent)
        {
            Name = "Build BuildRoom " + buildRoom;
            Resources = buildRoom.ListRequiredResources();

            // The creature first gets the resources needed to build the room.
            Tree = new Sequence(new GetResourcesAct(Agent, Resources),
                new Sequence(
                    // If the room exists and is a build order that hasn't been canceled
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    // Finds a voxel in the room to go to
                    SetTargetVoxelFromRoomAct(buildRoom, "TargetVoxel"),
                    // If the room still exists and is valid
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    // Go to the room
                    new GoToVoxelAct("TargetVoxel", PlanAct.PlanType.Adjacent, Agent),
                    // If the room exists and is valid
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    // Hit it until it gets built
                    new Wrap(() => Creature.HitAndWait(buildRoom.VoxelOrders.Count*0.5f/agent.Stats.BuildSpeed, true)),
                    // If the room is still valid at the end of this
                    new Wrap(() => IsRoomBuildOrder(buildRoom)),
                    // Creae the room and destroy the resources.
                    new PlaceRoomResourcesAct(Agent, buildRoom, Resources)

                    // Fallback on falure: if something goes wrong, put all the resources back into 
                    // a stockpile.
                    , new Wrap(Creature.RestockAll)) | new Wrap(Creature.RestockAll)
                );
        }

        /// <summary>
        ///     The kind of room to build, and where.
        /// </summary>
        public BuildRoomOrder BuildRoom { get; set; }

        /// <summary>
        ///     The resources that the room requires.
        /// </summary>
        public List<Quantitiy<Resource.ResourceTags>> Resources { get; set; }

        /// <summary>
        ///     Tells the creature which voxel to go to given a room build order.
        /// </summary>
        /// <param name="buildRoom">The room to build.</param>
        /// <param name="target">Blackboard key for the target voxel. Fills blackboard with that target.</param>
        /// <returns>Sucess if a voxel could be found. Fail otherwise.</returns>
        public IEnumerable<Status> SetTargetVoxelFromRoom(BuildRoomOrder buildRoom, string target)
        {
            if (buildRoom.VoxelOrders.Count == 0)
            {
                yield return Status.Fail;
            }
            else
            {
                Voxel closestVoxel = null;
                float closestDist = float.MaxValue;
                foreach (BuildVoxelOrder voxDes in buildRoom.VoxelOrders)
                {
                    float dist = (voxDes.Voxel.Position - Agent.Position).LengthSquared();

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestVoxel = voxDes.Voxel;
                    }
                }

                Agent.Blackboard.SetData(target, closestVoxel);
                yield return Status.Success;
            }
        }

        /// <summary>
        ///     Wraps SetTargetVoxelFromRoom into an Act.
        /// </summary>
        public Act SetTargetVoxelFromRoomAct(BuildRoomOrder buildRoom, string target)
        {
            return new Wrap(() => SetTargetVoxelFromRoom(buildRoom, target));
        }

        /// <summary>
        ///     Checks to see if the player has canceled building.
        /// </summary>
        /// <param name="buildRooom">The room order that the creature wants to build.</param>
        /// <returns>Success if the room is in the build queue, and false if its canceled.</returns>
        public IEnumerable<Status> IsRoomBuildOrder(BuildRoomOrder buildRooom)
        {
            yield return
                Creature.Faction.RoomBuilder.BuildDesignations.Contains(buildRooom) ? Status.Success : Status.Fail;
        }
    }
}