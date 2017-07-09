// GoToVoxelAct.cs
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
    /// A creature plans to a voxel and then follows the path to it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToVoxelAct : CompoundCreatureAct
    {
        public VoxelHandle Voxel { get; set; }
        public string VoxelName { get; set; }
        public PlanAct.PlanType PlanType { get; set; }
        public float Radius { get; set; }

        public GoToVoxelAct() : base()
        {
            
        }

        public GoToVoxelAct(string voxel, PlanAct.PlanType planType, CreatureAI creature, float radius = 0.0f) :
            base(creature)
        {
            Radius = radius;
            PlanType = planType;
            VoxelName = voxel;
            Name = "Go to DestinationVoxel " + voxel;

        }

        public GoToVoxelAct(VoxelHandle voxel, PlanAct.PlanType planType, CreatureAI creature, float radius = 0.0f) :
            base(creature)
        {
            Radius = radius;
            Voxel = voxel;
            Name = "Go to DestinationVoxel";
            PlanType = planType;

        }

        public override void Initialize()
        {
            if (VoxelName != null)
            {
                Tree = new Sequence(
            new Sequence(
                          new PlanAct(Agent, "PathToVoxel", VoxelName, PlanType) { Radius = Radius},
                          new FollowPathAct(Agent, "PathToVoxel")
                         ),
                          new StopAct(Agent));
            }
            else if (Voxel != null)
            {
                Tree = new Sequence(
                      new SetBlackboardData<VoxelHandle>(Agent, "ActionVoxel", Voxel),
                      new PlanAct(Agent, "PathToVoxel", "ActionVoxel", PlanType) { Radius = Radius },
                      new FollowPathAct(Agent, "PathToVoxel"),
                      new StopAct(Agent));
            }
            base.Initialize();
        }


        public override IEnumerable<Act.Status> Run()
        {
            return base.Run();
        }

    }

}