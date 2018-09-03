// GuardTool.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Using this tool, the player can specify certain voxels to be guarded.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GuardTool : PlayerTool
    {
        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            var parentTask = Player.TaskManager.EnumerateTasks().OfType<GuardZoneTask>().FirstOrDefault();
            if (parentTask == null)
            {
                parentTask = new GuardZoneTask();
                Player.TaskManager.AddTask(parentTask);
            }

            if (button == InputManager.MouseButton.Left)
            {
                foreach (var v in voxels.Where(v => v.IsValid && !v.IsEmpty))
                {
                    var key = VoxelHelpers.GetVoxelQuickCompare(v);
                    if (Player.Faction.GuardedVoxels.ContainsKey(key))
                        return;

                    Player.Faction.Designations.AddVoxelDesignation(v, DesignationType.Guard, null, new GuardZoneTask.DesignationProxyTask(parentTask, v));
                    Player.Faction.GuardedVoxels.Add(key, v);
                }

                OnConfirm(Faction.FilterMinionsWithCapability(Player.World.Master.SelectedMinions, Task.TaskCategory.Gather));
            }
            else
            {
                foreach (var v in voxels.Where(v => v.IsValid && !v.IsEmpty))
                {
                    var des = Player.Faction.Designations.GetVoxelDesignation(v, DesignationType.Guard);
                    if (des != null)
                        Player.TaskManager.CancelTask(des.Task);
                }
            }
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnBegin()
        {
            Player.VoxSelector.SelectionColor = Color.White;
        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

            Player.World.ShowTooltip("Click to guard. Rick click to cancel.");

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 3));
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }
    }
}
