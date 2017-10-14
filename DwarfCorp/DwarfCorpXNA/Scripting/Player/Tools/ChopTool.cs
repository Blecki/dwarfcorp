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
        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            if (bodies == null)
                return;

            var treesPicked = bodies.Where(c => c != null && c.Tags.Contains("Vegetation"));

            if (treesPicked.Any())
                Player.World.ShowToolPopup("Click to harvest this plant. Right click to cancel.");
            else
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

            Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 0));

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 0));
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 5));
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            NamedImageFrame frame = new NamedImageFrame("newgui/pointers", 32, 5, 0);
            foreach (Body tree in Player.BodySelector.CurrentBodies)
            {
                if (tree.Tags.Contains("Vegetation"))
                {
                    Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                    Drawer2D.DrawSprite(frame, tree.BoundingBox.Center(), Vector2.One * 0.5f, Vector2.Zero, new Color(255, 255, 255, 100));
                }
            }
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public static Task ChopTree(Body Tree, Faction PlayerFaction)
        {
            if (PlayerFaction.Designations.AddEntityDesignation(Tree, DesignationType.Chop) == DesignationSet.AddDesignationResult.Added)
                return new KillEntityTask(Tree, KillEntityTask.KillType.Chop)
                {
                    Priority = Task.PriorityType.Low
                };

            return null;
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            var treesPicked = bodies.Where(c => c.Tags.Contains("Vegetation"));

            if (button == InputManager.MouseButton.Left)
            {
                List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Player.Faction.SelectedMinions,
                    GameMaster.ToolMode.Chop);
                List<Task> tasks = new List<Task>();

                foreach (Body tree in treesPicked)
                {
                    if (!tree.IsVisible || tree.IsAboveCullPlane(Player.World.ChunkManager)) continue;

                    var task = ChopTree(tree, Player.Faction);
                    if (task != null)
                        tasks.Add(task);
                }

                if (tasks.Count > 0 && minions.Count > 0)
                {
                    TaskManager.AssignTasks(tasks, minions);
                    OnConfirm(minions);
                }
            }
            else if (button == InputManager.MouseButton.Right)
            {
                foreach (Body tree in treesPicked)
                {
                    if (!tree.IsVisible || tree.IsAboveCullPlane(Player.World.ChunkManager)) continue;
                    Player.Faction.Designations.RemoveEntityDesignation(tree, DesignationType.Chop);
                }
            }
        }
    }
}
