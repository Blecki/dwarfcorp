// GoToTaggedObjectAct.cs
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
    public class GoToTaggedObjectAct : CompoundCreatureAct
    {
        public string Tag { get; set; }
        public string ObjectName { get; set; }
        public bool Teleport { get; set; }
        public Vector3 TeleportOffset { get; set; }
        public bool CheckForOcclusion = true;

        public GoToTaggedObjectAct()
        {
            Name = "Go to tagged object";
            ObjectName = "Tagged Object";
        }

        public GoToTaggedObjectAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Go to tagged object";
            ObjectName = "Tagged Object";
        }


        public IEnumerable<Status> TeleportFunction()
        {
            GameComponent closestItem = Creature.AI.Blackboard.GetData<GameComponent>(ObjectName);

            if (closestItem != null)
            {
                var location = TeleportOffset + closestItem.BoundingBox.Center();
                if (CheckForOcclusion)
                {
                    VoxelHandle voxAt = new VoxelHandle(Agent.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(location));
                    bool gotLocation = false;
                    if (!voxAt.IsValid || !voxAt.IsEmpty)
                    {
                        // If we can't go to the preferred location, just try any free neighbor.
                        voxAt = new VoxelHandle(Agent.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(closestItem.BoundingBox.Center()));
                        foreach (var neighbor in VoxelHelpers.EnumerateManhattanNeighbors2D(voxAt.Coordinate))
                        {
                            VoxelHandle newVox = new VoxelHandle(Agent.World.ChunkManager, neighbor);

                            if (newVox.IsValid && newVox.IsEmpty)
                            {
                                location = newVox.WorldPosition + new Vector3(0.5f, Agent.Physics.BoundingBox.Extents().Y, 0.5f);
                                gotLocation = true;
                                break;
                            }
                        }

                        // If there's no free neighbor, just teleport directly to the object.
                        if (!gotLocation)
                        {
                            location = closestItem.BoundingBox.Center();
                        }
                    }
                }
                TeleportAct act = new TeleportAct(Creature.AI) { Location = location };
                act.Initialize();
                foreach (Act.Status status in act.Run())
                {
                    yield return status;
                }

            }

            yield return Status.Fail;
        }

        public override void Initialize()
        {
            if (Teleport)
            {
                Tree =
                    new Sequence
                        (
                        new GoToEntityAct(ObjectName, Creature.AI) { PlanType = PlanAct.PlanType.Adjacent, MovingTarget = false } ,
                        new Wrap(TeleportFunction)
                        );
            }
            else
            {
                Tree =
                    new Sequence
                        (
                        new GoToEntityAct(ObjectName, Creature.AI) { PlanType = PlanAct.PlanType.Adjacent, MovingTarget = false }
                        );
            }
            base.Initialize();
        }

 
    }
}
