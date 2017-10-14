// FarmTask.cs
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
    public class FarmTask : Task
    {
        public FarmTile FarmToWork { get; set; }
        public FarmAct.FarmMode Mode { get; set; }
        public string Plant { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; } 

        public FarmTask()
        {
            Priority = PriorityType.Low;
        }

        public FarmTask(FarmTile farmToWork)
        {
            FarmToWork = farmToWork;
            Name = "Work " + FarmToWork.Voxel.Coordinate;
            Priority = PriorityType.Low;
            AutoRetry = true;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return true;
        }


        public override bool ShouldDelete(Creature agent)
        {
            return !IsFeasible(agent);
        }

        public override void OnAssign(Creature agent)
        {
            if (FarmToWork != null)
            {
                FarmToWork.Farmer = agent.AI;
            }
            base.OnAssign(agent);
        }

        public override void OnUnAssign(Creature agent)
        {
            if (FarmToWork != null && FarmToWork.Farmer == agent.AI)
            {
                FarmToWork.Farmer = null;
            }
            base.OnUnAssign(agent);
        }

        public override bool IsFeasible(Creature agent)
        {
            bool farmValid =  FarmToWork != null && !FarmToWork.IsCanceled;
            if (!farmValid)
            {
                return false;
            }
            if (Mode == FarmAct.FarmMode.Till && FarmToWork.Voxel.Type.Name == "TilledSoil")
            {
                return false;
            }

            if (Mode == FarmAct.FarmMode.Plant && FarmToWork.PlantExists())
            {
                return false;
            }

            return true;
        }

        public override Act CreateScript(Creature agent)
        {
            return new FarmAct(agent.AI) {Resources = RequiredResources, PlantToCreate = Plant, Mode = Mode, FarmToWork = FarmToWork, Name = "Work " + FarmToWork.Voxel.Coordinate};
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (FarmToWork == null) return float.MaxValue;
            else
            {
                return (FarmToWork.Voxel.WorldPosition - agent.AI.Position).LengthSquared();
            }
        }

        public override Task Clone()
        {
            return new FarmTask() { RequiredResources = RequiredResources, FarmToWork = FarmToWork, Name = Name, Mode = Mode, Plant = Plant, Priority = Priority };
        }
    }
}
