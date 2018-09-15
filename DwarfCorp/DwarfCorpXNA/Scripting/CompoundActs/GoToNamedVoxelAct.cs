// GoToNamedVoxelAct.cs
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
    /// A creature goes to a voxel named in the blackboard.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToNamedVoxelAct : CompoundCreatureAct
    {
        public string VoxelName;
        public PlanAct.PlanType PlanType;
        public float Radius;

        public GoToNamedVoxelAct() : base()
        {

        }

        public GoToNamedVoxelAct(string voxel, PlanAct.PlanType planType, CreatureAI creature, float radius = 0.0f) :
            base(creature)
        {
            Radius = radius;
            PlanType = planType;
            VoxelName = voxel;
            Name = "Go to DestinationVoxel " + voxel;
        }

        public override void Initialize()
        {
            if (!String.IsNullOrEmpty(VoxelName))
            {
                Tree = new Sequence(
                    new Repeat(new Sequence(
                    new PlanWithGreedyFallbackAct() { Agent = Agent, PathName = "PathToVoxel", VoxelName = VoxelName, PlanType = PlanType, Radius = Radius},
                    new FollowPathAct(Agent, "PathToVoxel")), 10, true),
                    new StopAct(Agent));
            }
            base.Initialize();
        }
    }
}