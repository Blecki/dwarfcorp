// DigTool.cs
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
    /// When using this tool, the player specifies that certain voxels should
    /// be mined.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class DigTool : PlayerTool
    {
        public override void OnBegin()
        {
            Player.VoxSelector.SelectionColor = Color.White;
            Player.VoxSelector.DrawBox = true;
            Player.VoxSelector.DrawVoxel = true;
            Player.World.Tutorial("mine");
        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {

            if (button == InputManager.MouseButton.Left)
            {
                int count = Player.Faction.Designations.EnumerateDesignations(DesignationType.Dig).Count();

                Player.World.Tutorial("slice");
                List<Task> assignments = new List<Task>();
                foreach (var v in refs)
                {
                    if (!v.IsValid || (v.IsEmpty && v.IsExplored) || v.Type.IsInvincible)
                        continue;

                    var boundingBox = v.GetBoundingBox().Expand(-0.1f);
                    var entities = Player.World.EnumerateIntersectingObjects(boundingBox, CollisionType.Static);
                    if (entities.OfType<IVoxelListener>().Any())
                        continue;

                    if (count >= GameSettings.Default.MaxVoxelDesignations)
                    {
                        Player.World.ShowToolPopup("Too many dig designations!");
                        break;
                    }

                    // Todo: Should this be removed from the existing compound task and put in the new one?
                    if (!Player.Faction.Designations.IsVoxelDesignation(v, DesignationType.Dig) && !(Player.Faction.RoomBuilder.IsInRoom(v) || Player.Faction.RoomBuilder.IsBuildDesignation(v)))
                    {
                        var task = new KillVoxelTask(v);
                        task.Hidden = true;
                        assignments.Add(task);
                        count++;
                    }

                }

                Player.TaskManager.AddTasks(assignments);

                var compoundTask = new CompoundTask("DIG A HOLE", Task.TaskCategory.Dig, Task.PriorityType.Medium);
                compoundTask.AddSubTasks(assignments);
                Player.TaskManager.AddTask(compoundTask);

                List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Player.SelectedMinions, Task.TaskCategory.Dig);
                OnConfirm(minions);
            }
            else
            {
                foreach (var r in refs)
                {
                    if (r.IsValid)
                    {
                        var designation = Player.Faction.Designations.GetVoxelDesignation(r, DesignationType.Dig);
                        if (designation != null && designation.Task != null)
                            Player.TaskManager.CancelTask(designation.Task);
                    }
                }
            }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            throw new NotImplementedException();
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.BodySelector.Enabled = false;
                Player.World.SetMouse(null);
                return;
            }

            Player.VoxSelector.Enabled = true;

            if (Player.VoxSelector.VoxelUnderMouse.IsValid && !Player.World.IsMouseOverGui)
            {
                Player.World.ShowTooltip(Player.VoxSelector.VoxelUnderMouse.IsExplored ? Player.VoxSelector.VoxelUnderMouse.Type.Name : "???");
            }

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 1));

            Player.BodySelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }


        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
    }
}
