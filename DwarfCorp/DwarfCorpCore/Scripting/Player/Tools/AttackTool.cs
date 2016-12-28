// AttackTool.cs
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
    ///     When using this tool, the player clicks on creatures to specify that
    ///     they should be killed
    /// </summary>
    public class AttackTool : PlayerTool
    {
        /// <summary>
        ///     Color of boxes to draw around enemies.
        /// </summary>
        public Color DesignationColor { get; set; }

        /// <summary>
        ///     Boxes pulsate at this frequency.
        /// </summary>
        public float GlowRate { get; set; }


        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {
            // Intentionally left blank. This tool selects bodies, not voxels.
        }

        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            // Intentionally left blank. This tool selects bodies, not voxels.
        }

        public override void OnBegin()
        {
            // Intentionally left blank. Nothing to initialize.
        }

        public override void OnEnd()
        {
            // Intentionally left blank. Nothing to clean up.
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            // If the player is rotating the camera, don't activate the tool.
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.BodySelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = false;
                return;
            }

            // Otherwise, activate the tool. The body selector is on, and the voxel selector is off.
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
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Attack;
            }
        }


        /// <summary>
        ///     Draw boxes around the targets.
        /// </summary>
        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            Color drawColor = DesignationColor;

            var alpha = (float) Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds*GlowRate));
            drawColor.R = (byte) (Math.Min(drawColor.R*alpha + 50, 255));
            drawColor.G = (byte) (Math.Min(drawColor.G*alpha + 50, 255));
            drawColor.B = (byte) (Math.Min(drawColor.B*alpha + 50, 255));

            foreach (BoundingBox box in Player.Faction.AttackDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f*alpha + 0.05f, true);
            }
        }

        /// <summary>
        ///     Called whenever a list of bodies was selected by the player. Determines which bodies can be attacked,
        ///     and tells the player's Dwarfs to attack those bodies.
        /// </summary>
        /// <param name="bodies">The bodies which were selected by the player.</param>
        /// <param name="button">The mouse button (left/right/center) that was pressed.</param>
        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            // For each body, determine if it can be attacked.
            foreach (Body other in bodies)
            {
                // We can only attack creatures.
                Creature creature = other.GetChildrenOfType<Creature>().FirstOrDefault();
                if (creature == null)
                {
                    continue;
                }

                // We can't attack our friends!
                if (
                    PlayState.ComponentManager.Diplomacy.GetPolitics(creature.Faction, Player.Faction)
                        .GetCurrentRelationship() == Relationship.Loving)
                {
                    continue;
                }

                // Now we know which things can be attacked. Draw a debug box around them.
                Drawer3D.DrawBox(other.BoundingBox, DesignationColor, 0.1f, false);

                // If left button is pressed, tell all the selected dwarves to attack.
                if (button == InputManager.MouseButton.Left)
                {
                    if (!Player.Faction.AttackDesignations.Contains(other))
                    {
                        Player.Faction.AttackDesignations.Add(other);

                        foreach (CreatureAI minion in Player.Faction.SelectedMinions)
                        {
                            minion.Tasks.Add(new KillEntityTask(other, KillEntityTask.KillType.Attack));
                        }

                        OnConfirm(Player.Faction.SelectedMinions);
                    }
                }
                    // If right button is pressed, tell all the selected dwarves not to attack.
                else if (button == InputManager.MouseButton.Right)
                {
                    if (Player.Faction.AttackDesignations.Contains(other))
                    {
                        Player.Faction.AttackDesignations.Remove(other);
                    }
                }
            }
        }
    }
}