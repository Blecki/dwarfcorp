// BuildTool.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class HarvestTool : PlayerTool
    {
        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
        
        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            List<Task> tasks = new List<Task>();

            foreach (Body tree in bodies.Where(c => c.Tags.Contains("Vegetation")))
            {
                if (!tree.IsVisible || tree.IsAboveCullPlane(Player.World.ChunkManager)) continue;

                switch (button)
                {
                    case InputManager.MouseButton.Left:
                        if (Player.Faction.AddEntityDesignation(tree, DesignationType.Chop) == Faction.AddEntityDesignationResult.Added)
                        {
                            tasks.Add(new KillEntityTask(tree, KillEntityTask.KillType.Chop) { Priority = Task.PriorityType.Low });
                            this.Player.World.ShowToolPopup("Will harvest this " + tree.Name);
                        }
                        break;
                    case InputManager.MouseButton.Right:
                        if (Player.Faction.RemoveEntityDesignation(tree, DesignationType.Chop) == Faction.RemoveEntityDesignationResult.Removed)
                            this.Player.World.ShowToolPopup("Harvest cancelled " + tree.Name);
                        break;
                }
            }
            if (tasks.Count > 0 && Player.SelectedMinions.Count > 0)
            {
                TaskManager.AssignTasks(tasks, Player.SelectedMinions);
                OnConfirm(Player.SelectedMinions);
            }
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }
        
        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }

            Player.BodySelector.AllowRightClickSelection = true;

                    Player.VoxSelector.Enabled = false;
                    Player.BodySelector.Enabled = true;
        
            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 12));
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

        }
    }
}
