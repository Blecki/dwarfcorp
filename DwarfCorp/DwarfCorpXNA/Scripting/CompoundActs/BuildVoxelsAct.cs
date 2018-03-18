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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class BuildVoxelsAct : CompoundCreatureAct
    {
        public List<KeyValuePair<VoxelHandle, VoxelType> > Voxels { get; set; }

        public BuildVoxelsAct()
        {

        }

        public BuildVoxelsAct(CreatureAI creature, List<VoxelHandle> voxels, List<VoxelType> types) :
            base(creature)
        {

            Voxels = new List<KeyValuePair<VoxelHandle, VoxelType>>();
            for (int i = 0; i < voxels.Count; i++)
            {
                Voxels.Add(new KeyValuePair<VoxelHandle, VoxelType>(voxels[i], types[i]));
            }
            Name = "Build voxels";
        }

        public override void Initialize()
        {

            List<ResourceAmount> resources = Voxels.Select(pair => new ResourceAmount(ResourceLibrary.Resources[pair.Value.ResourceToRelease], 1)).ToList();

            List<Act> children = new List<Act>()
            {
                new GetResourcesAct(Agent, resources)
            };
            
            int i = 0;
            foreach (var pair in Voxels)
            {
                children.Add(new GoToVoxelAct(pair.Key, PlanAct.PlanType.Radius, Agent, 3.0f));
                children.Add(new PlaceVoxelAct(pair.Key, Creature.AI, resources[i]));
                i++;
            }

            children.Add(new Wrap(Creature.RestockAll));

            Tree = new Sequence(children);
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }

}