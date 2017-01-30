// ChopTool.cs
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
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// When using this tool, the player clicks on trees/bushes to specify that 
    /// they should be chopped down.
    /// </summary>
    public class ChopTool : PlayerTool
    {
        public Color ChopDesignationColor { get; set; }
        public float ChopDesignationGlowRate { get; set; }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.BodySelector.Enabled = false;
                WorldManager.GUI.IsMouseVisible = false;
                return;
            }

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;
            WorldManager.GUI.IsMouseVisible = true;

            if (WorldManager.IsMouseOverGui)
            {
                WorldManager.GUI.MouseMode = GUISkin.MousePointer.Pointer;
            }
            else
            {
                WorldManager.GUI.MouseMode = GUISkin.MousePointer.Chop;
            }
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

            Color drawColor = ChopDesignationColor;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * ChopDesignationGlowRate));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            foreach(BoundingBox box in Player.Faction.ChopDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
            }

            foreach (Body tree in Player.BodySelector.CurrentBodies)
            {
                if (tree.Tags.Contains("Vegetation"))
                {
                    Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                }
            }
        }

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

            List<Body> treesPickedByMouse = ComponentManager.FilterComponentsWithTag("Vegetation", bodies);

            List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Player.Faction.SelectedMinions,
                GameMaster.ToolMode.Chop);
            List<Task> tasks = new List<Task>();
            foreach (Body tree in treesPickedByMouse)
            {
                if (!tree.IsVisible || tree.IsAboveCullPlane) continue;

                Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                if (button == InputManager.MouseButton.Left)
                {
                    if (!Player.Faction.ChopDesignations.Contains(tree))
                    {
                        Player.Faction.ChopDesignations.Add(tree);
                        tasks.Add(new KillEntityTask(tree, KillEntityTask.KillType.Chop) { Priority = Task.PriorityType.Low });
                    }
                }
                else if (button == InputManager.MouseButton.Right)
                {
                    if (Player.Faction.ChopDesignations.Contains(tree))
                    {
                        Player.Faction.ChopDesignations.Remove(tree);
                    }
                }
            }
           
            if (tasks.Count > 0 && minions.Count > 0)
            {
                TaskManager.AssignTasks(tasks, minions);
                OnConfirm(minions);
            }
        }
    }
}
