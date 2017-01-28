// GetNearestFreeVoxelInZone.cs
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
    /// This act finds the nearest unoccupied and unreserved voxel in a zone,
    /// and fills the blackboard with it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GetNearestFreeVoxelInZone : CreatureAct
    {
        public Zone TargetZone { get; set; }
        public string OutputVoxel { get; set; }
        public bool ReserveVoxel { get; set; }

        public GetNearestFreeVoxelInZone(CreatureAI agent, Zone targetZone, string outputVoxel, bool reserve) :
            base(agent)
        {
            Name = "Get Free Voxel";
            OutputVoxel = outputVoxel;
            TargetZone = targetZone;
            ReserveVoxel = reserve;
        }

        public override IEnumerable<Status> Run()
        {
            if(TargetZone == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Voxel v = TargetZone.GetNearestVoxel(Agent.Position);

                if(!v.IsEmpty)
                {
                    Agent.Blackboard.SetData(OutputVoxel, v);
                    yield return Status.Success;
                }
                else
                {
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Status.Fail;
                }
            }
        }
    }

}