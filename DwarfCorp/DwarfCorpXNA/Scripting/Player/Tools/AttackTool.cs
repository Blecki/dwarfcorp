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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// When using this tool, the player clicks on creatures to specify that 
    /// they should be killed
    /// </summary>
    public class AttackTool : PlayerTool
    {
        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnBegin()
        {
            
        }

        public override void OnEnd()
        {
            
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            bool shown = false;
            foreach (Body other in bodies)
            {
                var creature = other.EnumerateAll().OfType<Creature>().FirstOrDefault();
                if (creature == null)
                {
                    continue;
                }

                if (Player.World.Diplomacy.GetPolitics(creature.Faction, Player.Faction).GetCurrentRelationship() ==
                    Relationship.Loving)
                {
                    Player.Faction.World.ShowToolPopup("We refuse to attack allies.");
                    shown = true;
                    continue;
                }
                Player.Faction.World.ShowToolPopup("Click to attack this " + creature.Species);
                shown = true;
            }
            if (!shown)
                DefaultOnMouseOver(bodies);
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

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;


            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 2));
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

            foreach (Body other in bodies)
            {
                var creature = other.EnumerateAll().OfType<Creature>().FirstOrDefault();
                if (creature == null)
                {
                    continue;
                }

                if (Player.World.Diplomacy.GetPolitics(creature.Faction, Player.Faction).GetCurrentRelationship() == Relationship.Loving)
                {
                    Player.Faction.World.ShowToolPopup("We refuse to attack allies.");
                    continue;
                }

                Drawer3D.DrawBox(other.BoundingBox, Color.Red, 0.1f, false);

                if (button == InputManager.MouseButton.Left)
                {
                    if (Player.Faction.Designations.AddEntityDesignation(other, DesignationType.Attack) == DesignationSet.AddEntityDesignationResult.Added)
                    { 
                        foreach (CreatureAI minion in Player.Faction.SelectedMinions)
                        {
                            minion.AssignTask(new KillEntityTask(other, KillEntityTask.KillType.Attack));
                            CreatureAI otherAi = other.GetRoot().GetComponent<CreatureAI>();
                            if (otherAi != null)
                            {
                                otherAi.AssignTask(new KillEntityTask(minion.Physics, KillEntityTask.KillType.Auto));
                            }
                        }
                        Player.Faction.World.ShowToolPopup("Will attack this " + creature.Species);
                        OnConfirm(Player.Faction.SelectedMinions);
                    }
                }
                else if (button == InputManager.MouseButton.Right)
                {
                    Player.Faction.World.ShowToolPopup("Attack cancelled for " + creature.Species);
                    Player.Faction.Designations.RemoveEntityDesignation(other, DesignationType.Attack);
                }
            }
        }
    }
}
