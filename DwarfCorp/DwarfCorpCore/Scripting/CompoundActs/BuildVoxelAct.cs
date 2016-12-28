// BuildVoxelAct.cs
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
using System.Linq;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class BuildVoxelAct : CompoundCreatureAct
    {
        public BuildVoxelAct()
        {
        }

        /// <summary>
        ///     Construct a BuildVoxelAct
        /// </summary>
        /// <param name="creature">The creature we want to build the voxel</param>
        /// <param name="voxel">The voxel location to build a wall</param>
        /// <param name="type">The type of voxel to build</param>
        public BuildVoxelAct(CreatureAI creature, Voxel voxel, VoxelType type) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Build voxel";

            var resources = new List<ResourceAmount>
            {
                new ResourceAmount(ResourceLibrary.Resources[type.ResourceToRelease], 1)
            };

            if (Agent.Faction.WallBuilder.IsDesignation(voxel))
            {
                // In sequence.. The dwarf first gets the required resources
                Tree = new Sequence(new GetResourcesAct(Agent, resources),
                    // Then, it goes to the voxel and places it there.
                    new Sequence(
                        new GoToVoxelAct(voxel, PlanAct.PlanType.Adjacent, Agent),
                        new PlaceVoxelAct(voxel, creature, resources.First()), new Wrap(Creature.RestockAll)) |
                    new Wrap(Creature.RestockAll)
                    );
            }
            else
            {
                Tree = null;
            }
        }

        public Voxel Voxel { get; set; }
    }
}