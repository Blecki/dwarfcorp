// BuildVoxelTask.cs
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
    ///     Tells a creature that it should get a resource, and put it into a voxel
    ///     to build it. This is the Task that wraps BuildVoxelAct.
    ///     NOTE: This is now a legacy task. The real work happens in GatherManager, which
    ///     assigns a whole group of voxels to a Dwarf instead of assigning them one at a time.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class BuildVoxelTask : Task
    {
        public BuildVoxelTask()
        {
            Priority = PriorityType.Low;
        }

        /// <summary>
        ///     Create a BuildVoxelTask
        /// </summary>
        /// <param name="voxel">The location to place the voxel.</param>
        /// <param name="type">The type to place.</param>
        public BuildVoxelTask(Voxel voxel, VoxelType type)
        {
            Name = "Put voxel of type: " + type.Name + " on voxel " + voxel.Position;
            Voxel = voxel;
            VoxType = type;
            Priority = PriorityType.Low;
        }

        /// <summary>
        ///     The type of voxel to place.
        /// </summary>
        public VoxelType VoxType { get; set; }

        /// <summary>
        ///     The location to place the voxel.
        /// </summary>
        public Voxel Voxel { get; set; }

        public override bool IsFeasible(Creature agent)
        {
            return Voxel != null && agent.Faction.WallBuilder.IsDesignation(Voxel);
        }

        public override bool ShouldDelete(Creature agent)
        {
            return Voxel == null || !agent.Faction.WallBuilder.IsDesignation(Voxel);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Voxel != null && agent.Faction.WallBuilder.IsDesignation(Voxel);
        }

        public override Task Clone()
        {
            return new BuildVoxelTask(Voxel, VoxType);
        }

        public override float ComputeCost(Creature agent)
        {
            return Voxel == null
                ? 1000
                : 0.01f*(agent.AI.Position - Voxel.Position).LengthSquared() + (Voxel.Position.Y);
        }

        /// <summary>
        ///     Adds this build voxel task to the creature's build order queue (managed by GatherManager).
        /// </summary>
        /// <param name="creature">The creature to perform the task.</param>
        /// <returns>Act.Status.Success</returns>
        public IEnumerable<Act.Status> AddBuildOrder(Creature creature)
        {
            creature.AI.GatherManager.AddVoxelOrder(new GatherManager.BuildVoxelOrder {Type = VoxType, Voxel = Voxel});
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature creature)
        {
            return new Wrap(() => AddBuildOrder(creature));
        }
    }
}