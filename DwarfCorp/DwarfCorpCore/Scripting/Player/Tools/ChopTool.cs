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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     When using this tool, the player clicks on trees/bushes to specify that
    ///     they should be chopped down.
    /// </summary>
    public class ChopTool : PlayerTool
    {
        /// <summary>
        /// Gets or sets the color of the chop designation (bounding box drawn around object).
        /// </summary>
        /// <value>
        /// The color of the chop designation.
        /// </value>
        public Color ChopDesignationColor { get; set; }
        /// <summary>
        /// Gets or sets the chop designation glow rate (hz that the chop designation will pulsate at.).
        /// </summary>
        /// <value>
        /// The chop designation glow rate.
        /// </value>
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
                PlayState.GUI.IsMouseVisible = false;
                return;
            }

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;
            PlayState.GUI.IsMouseVisible = true;

            if (PlayState.GUI.IsMouseOver())
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
            }
            else
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Chop;
            }
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            Color drawColor = ChopDesignationColor;

            var alpha = (float) Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds*ChopDesignationGlowRate));
            drawColor.R = (byte) (Math.Min(drawColor.R*alpha + 50, 255));
            drawColor.G = (byte) (Math.Min(drawColor.G*alpha + 50, 255));
            drawColor.B = (byte) (Math.Min(drawColor.B*alpha + 50, 255));

            foreach (BoundingBox box in Player.Faction.ChopDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f*alpha + 0.05f, true);
            }
        }

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            // Get all the selected bodies that have the tag "Vegetation"
            List<Body> treesPickedByMouse = ComponentManager.FilterComponentsWithTag("Vegetation", bodies);

            // Get all the creatures that can chop.
            List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Player.Faction.SelectedMinions,
                GameMaster.ToolMode.Chop);

            // Create a list of chop tasks.
            var tasks = new List<Task>();
            foreach (Body tree in treesPickedByMouse)
            {
                // Ignore invisible trees or trees above the slice.
                if (!tree.IsVisible || tree.IsAboveCullPlane) continue;

                // Draw a box around the tree.
                Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);

                // On left click, add it to the chop designations. On right, remove it.
                switch (button)
                {
                    case InputManager.MouseButton.Left:
                        if (!Player.Faction.ChopDesignations.Contains(tree))
                        {
                            Player.Faction.ChopDesignations.Add(tree);
                            tasks.Add(new KillEntityTask(tree, KillEntityTask.KillType.Chop)
                            {
                                Priority = Task.PriorityType.Low
                            });
                        }
                        break;
                    case InputManager.MouseButton.Right:
                        if (Player.Faction.ChopDesignations.Contains(tree))
                        {
                            Player.Faction.ChopDesignations.Remove(tree);
                        }
                        break;
                }
            }

            // Assign tasks.
            if (tasks.Count > 0 && minions.Count > 0)
            {
                TaskManager.AssignTasks(tasks, minions);
                OnConfirm(minions);
            }
        }
    }
}