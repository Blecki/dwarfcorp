// BuildVoxelsAct.cs
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
    ///     This is the aggregate version of BuildVoxelAct -- the difference being that the Dwarf may choose to
    ///     gather the resources required for all voxels before building them.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class BuildVoxelsAct : CompoundCreatureAct
    {
        public BuildVoxelsAct()
        {
        }

        /// <summary>
        ///     Create a BuildVoxelsAct.
        /// </summary>
        /// <param name="creature">The creature to build a voxel.</param>
        /// <param name="voxels">List of voxels to build.</param>
        /// <param name="types">List of voxel types to place at the voxels (1-1 mapping)</param>
        public BuildVoxelsAct(CreatureAI creature, List<Voxel> voxels, List<VoxelType> types) :
            base(creature)
        {
            Voxels = new List<KeyValuePair<Voxel, VoxelType>>();
            for (int i = 0; i < voxels.Count; i++)
            {
                Voxels.Add(new KeyValuePair<Voxel, VoxelType>(voxels[i], types[i]));
            }
            Name = "Build voxels";
        }

        /// <summary>
        ///     A list of voxels to place a new voxel wall type at.
        /// </summary>
        public List<KeyValuePair<Voxel, VoxelType>> Voxels { get; set; }

        /// <summary>
        ///     Initialize the Act tree based on the voxels and types.
        /// </summary>
        public override void Initialize()
        {
            // Get the list of all required resources to build the voxels.
            List<ResourceAmount> resources =
                Voxels.Select(pair => new ResourceAmount(ResourceLibrary.Resources[pair.Value.ResourceToRelease], 1))
                    .ToList();

            // Start with an Act that just tells the dwarf to get the resources.
            var children = new List<Act>
            {
                new GetResourcesAct(Agent, resources)
            };

            // Now, for each voxel, go to it and place it.
            int i = 0;
            foreach (var pair in Voxels)
            {
                children.Add(new GoToVoxelAct(pair.Key, PlanAct.PlanType.Radius, Agent, 3.0f));
                children.Add(new PlaceVoxelAct(pair.Key, Creature.AI, resources[i]));
                i++;
            }

            // If anything at all fails, put all the resources back.
            Tree = new Select(new Sequence(children), new Wrap(Creature.RestockAll));
            base.Initialize();
        }

        /// <summary>
        ///     If the Act is cancelled, remove from the list any and all walls that are not currently
        ///     designations.
        /// </summary>
        public override void OnCanceled()
        {
            Voxels.RemoveAll(pair => !Creature.Faction.WallBuilder.IsDesignation(pair.Key));
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}