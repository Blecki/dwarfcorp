// GuardVoxelTask.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should guard a voxel.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GuardVoxelTask : Task
    {
        public Voxel VoxelToGuard = null;

        public GuardVoxelTask(Voxel vox)
        {
            Name = "Guard Voxel: " + vox.Position;
            VoxelToGuard = vox;
            Priority = PriorityType.Medium;
        }

        public override Task Clone()
        {
            return new GuardVoxelTask(VoxelToGuard);
        }

        public override Act CreateScript(Creature agent)
        {
            return new GuardVoxelAct(agent.AI, VoxelToGuard);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return VoxelToGuard == null ? 1000 : (agent.AI.Position - VoxelToGuard.Position).LengthSquared();
        }

        public override bool ShouldRetry(Creature agent)
        {
            return agent.Faction.IsGuardDesignation(VoxelToGuard);
        }

        public override void Render(DwarfTime time)
        {
            BoundingBox box = VoxelToGuard.GetBoundingBox();


            Color drawColor = Color.LightBlue;

      

            drawColor.R = (byte)(Math.Min(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f)) + 50, 255));
            Drawer3D.DrawBox(box, drawColor, 0.05f, true);
            base.Render(time);
        }
    }

}