// LookInterestingTask.cs
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
    /// <summary>
    /// Tells a creature that it should do something (anything) since there 
    /// is nothing else to do.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class LookInterestingTask : Task
    {
        public LookInterestingTask()
        {
            Name = "LookInteresting";
            Priority = PriorityType.Eventually;
        }

        public override Task Clone()
        {
            return new LookInterestingTask();
        }

        public override Act CreateScript(Creature creature)
        {
            return new WanderAct(creature.AI, 2, 0.5f, 1.0f);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }
    }

    /// <summary>
    /// If the creature is in liquid, causes it to find the nearest land and go there.
    /// </summary>
    /// <seealso cref="Task" />
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class FindLandTask : Task
    {
        public FindLandTask()
        {
            Name = "Find Land";
            Priority = PriorityType.High;
        }

        public override Task Clone()
        {
            return new FindLandTask();
        }

        /// <summary>
        /// Finds a voxel to stand on within a radius of N voxels using BFS.
        /// </summary>
        /// <param name="radius">The radius to search in.</param>
        /// <returns>The voxel within the radius which is over land if it exists, null otherwise.</returns>
        public Voxel FindLand(int radius, Voxel checkVoxel)
        {
            return checkVoxel.Chunk.Manager.BreadthFirstSearch(checkVoxel, radius * radius, voxel => voxel != null && voxel.IsEmpty && voxel.WaterLevel == 0 && !voxel.IsBottomEmpty());
        }

        /// <summary>
        /// Finds the air above the creature.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>A voxel containing air above the creature if it exists, or null otherwise</returns>
        public Voxel FindAir(Creature creature)
        {
            int startHeight = (int) creature.AI.Position.Y;
            int x = (int)creature.Physics.CurrentVoxel.GridPosition.X;
            int z = (int)creature.Physics.CurrentVoxel.GridPosition.Z;
            VoxelChunk chunk = creature.Physics.CurrentVoxel.Chunk;
            Voxel check = chunk.MakeVoxel(0, 0, 0);
            for (int y = startHeight; y < creature.Chunks.ChunkData.ChunkSizeY; y++)
            {
                check.GridPosition = new Vector3(x, y, z);
                if (check.WaterLevel == 0 && check.IsEmpty)
                {
                    return check;
                }
            }
            return null;
        }

        public IEnumerable<Act.Status> SwimUp(Creature creature)
        {
            Timer timer = new Timer(10.0f, false, Timer.TimerMode.Game);

            while (!timer.HasTriggered)
            {
                timer.Update(DwarfTime.LastTime);

                creature.Physics.ApplyForce(Vector3.Up * 25, DwarfTime.Dt);

                if (!creature.Physics.IsInLiquid)
                {
                    yield return Act.Status.Success;
                    yield break;
                }
                yield return Act.Status.Running;
            }
            yield return Act.Status.Fail;
        }

        public override Act CreateScript(Creature creature)
        {
            Voxel above = creature.Physics.CurrentVoxel.GetVoxelAbove();
            if ((above != null && above.WaterLevel > 0 )|| creature.AI.Movement.CanFly)
            {
                return new Wrap(() => SwimUp(creature)) { Name = "Swim up"};
            }

            Voxel findLand = FindLand(3, creature.Physics.CurrentVoxel);
            if (findLand == null)
            {
                if (creature.Faction.GetRooms().Count == 0)
                {
                    return new LongWanderAct(creature.AI) {PathLength = 20, Radius = 30, Is2D = true};
                }
                else
                {
                    return new GoToZoneAct(creature.AI, Datastructures.SelectRandom(creature.Faction.GetRooms()));
                }
            }
            else
            {
                return new GoToVoxelAct(findLand, PlanAct.PlanType.Into, creature.AI);
            }
        }

        public override float  ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public override bool IsFeasible(Creature agent)
        {
            return agent.Physics.IsInLiquid;
        }
    }

}
