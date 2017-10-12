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
    public class TillTool : PlayerTool
    {
        public List<ResourceAmount> RequiredResources { get; set; }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            foreach (var voxel in voxels)
                ValidateTilling(voxel);
        }

        private bool ValidateTilling(VoxelHandle voxel)
        {
            if (!voxel.Type.IsSoil)
            {
                Player.World.ShowToolPopup(String.Format("Can only till soil (not {0})!", voxel.Type.Name));
                return false;
            }
            if (voxel.Type.Name == "TilledSoil")
            {
                Player.World.ShowToolPopup("Soil already tilled!");
                return false;
            }
            var above = VoxelHelpers.GetVoxelAbove(voxel);
            if (above.IsValid && !above.IsEmpty)
            {
                Player.World.ShowToolPopup("Something is blocking the top of this tile.");
                return false;
            }
            Player.World.ShowToolPopup("Click to till.");
            return true;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            List<CreatureAI> minions = Player.World.Master.SelectedMinions.Where(minion => minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm)).ToList();
            List<FarmTask> goals = new List<FarmTask>();

            foreach (var voxel in voxels)
            {
                if (button == InputManager.MouseButton.Left)
                {
                    if (!ValidateTilling(voxel))
                        continue;

                    var existingFarmTile = Player.Faction.GetFarmDesignation(voxel);
                    if (existingFarmTile == null)
                    {
                        existingFarmTile = new FarmTile { Vox = voxel };
                        Player.Faction.AddFarmDesignation(existingFarmTile, DesignationType.Till);
                    }

                    goals.Add(new FarmTask(existingFarmTile) { Mode = FarmAct.FarmMode.Till });
                }
                else
                {
                    var existingFarmTile = Player.Faction.GetFarmDesignation(voxel);
                    if (existingFarmTile != null && !existingFarmTile.PlantExists())
                    {
                        existingFarmTile.IsCanceled = true;
                        existingFarmTile.Farmer = null;
                        Player.Faction.RemoveFarmDesignation(existingFarmTile.Vox, DesignationType.Till);
                    }
                }
            }

            TaskManager.AssignTasksGreedy(goals.Cast<Task>().ToList(), minions, 1);

            foreach (CreatureAI creature in minions)
                creature.Creature.NoiseMaker.MakeNoise("Ok", creature.Position);
        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
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

            Player.VoxSelector.Enabled = true;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            Player.BodySelector.Enabled = false;
            ValidateTilling(Player.VoxSelector.VoxelUnderMouse);

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 12));
        } 

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnBegin()
        {
           
        }
    }
}
