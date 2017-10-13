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
    public class PlantTool : PlayerTool
    {
        public string PlantType { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            int currentAmount = Player.Faction.ListResources()
               .Sum(resource => resource.Key == PlantType && resource.Value.NumResources > 0 ? resource.Value.NumResources : 0);

            if (currentAmount == 0)
            {
                Player.World.ShowToolPopup("Not enough " + PlantType + " in stocks!");
                return;
            }

            foreach (var voxel in voxels)
                ValidatePlanting(voxel);
        }

        private bool ValidatePlanting(VoxelHandle voxel)
        {
            if (voxel.Type.Name != "TilledSoil")
            {
                Player.World.ShowToolPopup("Can only plant on tilled soil!");
                return false;
            }

            if (ResourceLibrary.Resources[PlantType].Tags.Contains(Resource.ResourceTags.AboveGroundPlant))
            {
                if (voxel.SunColor == 0)
                {
                    Player.World.ShowToolPopup("Can only plant " + PlantType + " above ground.");
                    return false;
                }
            }
            else if (
                ResourceLibrary.Resources[PlantType].Tags.Contains(
                    Resource.ResourceTags.BelowGroundPlant))
            {
                if (voxel.SunColor > 0)
                {
                    Player.World.ShowToolPopup("Can only plant " + PlantType + " below ground.");
                    return false;
                }
            }

            var designation = Player.Faction.GetFarmDesignation(voxel);
            if (designation != null && designation.PlantExists())
            {
                Player.World.ShowToolPopup("Something is already planted here!");
                return false;
            }

            var above = VoxelHelpers.GetVoxelAbove(voxel);
            if (above.IsValid && !above.IsEmpty)
            {
                Player.World.ShowToolPopup("Something is blocking the top of this tile.");
                return false;
            }

            if (designation != null && designation.Farmer != null)
            {
                Player.World.ShowToolPopup("This tile is already being worked.");
                return false;
            }

            Player.World.ShowToolPopup("Click to plant.");

            return true;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            List<CreatureAI> minions = Player.World.Master.SelectedMinions.Where(minion => minion.Stats.CurrentClass.HasAction(GameMaster.ToolMode.Farm)).ToList();
            List<FarmTask> goals = new List<FarmTask>();

            int currentAmount = Player.Faction.ListResources()
                .Sum(resource => resource.Key == PlantType && resource.Value.NumResources > 0 ? resource.Value.NumResources : 0);

            foreach (var voxel in voxels)
            {
                if (currentAmount == 0)
                {
                    Player.World.ShowToolPopup("Not enough " + PlantType + " in stocks!");
                    break;
                }

                if (ValidatePlanting(voxel))
                {
                    var existingTile = Player.Faction.GetFarmDesignation(voxel);
                    if (existingTile == null)
                    {
                        existingTile = Player.Faction.AddFarmDesignation(voxel, DesignationType.Plant);
                    }

                    goals.Add(new FarmTask(existingTile)
                    {
                        Mode = FarmAct.FarmMode.Plant,
                        Plant = PlantType,
                        RequiredResources = RequiredResources
                    });
                    
                    currentAmount--;
                }
            }

            TaskManager.AssignTasksGreedy(goals.Cast<Task>().ToList(), minions, 1);
            
            if (Player.World.Paused)
            {
                // Horrible hack to make it work when game is paused. Farmer doesn't get assigned until
                // next update!
                if (minions.Count > 0)
                {
                    foreach (var goal in goals)
                    {
                        goal.FarmToWork.Farmer = minions[0];
                    }
                }
            }

            OnConfirm(minions);
        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {

        }

        public override void OnBegin()
        {

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
            ValidatePlanting(Player.VoxSelector.VoxelUnderMouse);

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
