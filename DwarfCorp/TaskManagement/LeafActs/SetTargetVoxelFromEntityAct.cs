// SetTargetVoxelFromEntityAct.cs
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
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class SetTargetVoxelFromEntityAct : CreatureAct
    {
        public string VoxelOutName { get; set; }
        public string EntityName { get; set; }

        public SetTargetVoxelFromEntityAct(CreatureAI creature, string entityName, string voxelOut) :
            base(creature)
        {
            Name = "Set Target DestinationVoxel";
            VoxelOutName = voxelOut;
            EntityName = entityName;
        }

        public static Act.Status SetTarget(string voxelOutName, string entityName, Creature creature)
        {
            GameComponent target = creature.AI.Blackboard.GetData<GameComponent>(entityName);
            if (target == null)
            {
                return Status.Fail;
            }
            else
            {
                var targetPosition = target.BoundingBox.Center();
                targetPosition = MathFunctions.Clamp(targetPosition, target.World.ChunkManager.Bounds.Expand(-1));
                var voxelUnder = VoxelHelpers.FindFirstVoxelBelowIncludingWater(new VoxelHandle(
                    creature.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(targetPosition)));

                if (voxelUnder.IsValid)
                {
                    if (!voxelUnder.IsEmpty)
                        voxelUnder = VoxelHelpers.GetVoxelAbove(voxelUnder);
                    creature.AI.Blackboard.SetData(voxelOutName, voxelUnder);

                    if ((voxelUnder.GetBoundingBox().Center() - target.Position).Length() > 10)
                    {
                        return Status.Fail;
                    }
                    return Status.Success;
                }
                else
                    return Status.Fail;
            }
        }

        public override IEnumerable<Status> Run()
        {
            yield return SetTarget(VoxelOutName, EntityName, Creature);
        }
    }

}