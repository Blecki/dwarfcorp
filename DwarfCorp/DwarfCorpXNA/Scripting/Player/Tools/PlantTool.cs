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
            Player.VoxSelector.SelectionColor = Color.White;
            foreach (var voxel in voxels)
                ValidatePlanting(voxel);
        }

        private bool ValidatePlanting(VoxelHandle voxel)
        {
            if (!voxel.Type.IsSoil)
            {
                Player.World.ShowToolPopup("Can only plant on soil!");
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
            else if (ResourceLibrary.Resources[PlantType].Tags.Contains(Resource.ResourceTags.BelowGroundPlant))
            {
                if (voxel.SunColor > 0)
                {
                    Player.World.ShowToolPopup("Can only plant " + PlantType + " below ground.");
                    return false;
                }
            }

            var designation = Player.Faction.Designations.GetVoxelDesignation(voxel, DesignationType.Plant);

            if (designation != null)
            {
                Player.World.ShowToolPopup("You're already planting here.");
                return false;
            }

            var boundingBox = new BoundingBox(voxel.Coordinate.ToVector3() + new Vector3(0.2f, 0.2f, 0.2f), voxel.Coordinate.ToVector3() + new Vector3(0.8f, 0.8f, 0.8f));
            var entities = Player.World.EnumerateIntersectingObjects(boundingBox, CollisionType.Static).OfType<IVoxelListener>();
            if (entities.Any())
            {
                if (Debugger.Switches.DrawToolDebugInfo)
                {
                    Drawer3D.DrawBox(boundingBox, Color.Red, 0.03f, false);
                    foreach (var entity in entities)
                        Drawer3D.DrawBox((entity as Body).GetBoundingBox(), Color.Yellow, 0.03f, false);
                }

                Player.World.ShowToolPopup("There's something in the way.");
                return false;
            }

            if (Player.Faction.GetIntersectingRooms(voxel.GetBoundingBox()).Count > 0)
            {
                Player.World.ShowToolPopup("Can't plant inside zones.");
                return false;
            }

            Player.World.ShowToolPopup("Click to plant.");

            return true;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
            {
                var goals = new List<PlantTask>();

                foreach (var voxel in voxels)
                {
                    if (ValidatePlanting(voxel))
                    {
                        var farmTile = new Farm
                        {
                            Voxel = voxel,
                            RequiredResources = RequiredResources,
                            SeedResourceType = PlantType
                        };

                        var task = new PlantTask(farmTile)
                        {
                            Plant = PlantType,
                            RequiredResources = RequiredResources
                        };

                        if (voxel.Type.Name != "TilledSoil")
                            farmTile.TargetProgress = 200.0f; // Planting on untilled soil takes longer.

                        goals.Add(task);
                    }
                }

                Player.TaskManager.AddTasks(goals);
                
                OnConfirm(Player.World.Master.Faction.Minions.Where(minion => minion.Stats.IsTaskAllowed(Task.TaskCategory.Plant)).ToList());
            }
            else if (button == InputManager.MouseButton.Right)
            {
                foreach (var voxel in voxels)
                {
                    var designation = Player.Faction.Designations.GetVoxelDesignation(voxel, DesignationType.Plant);

                    if (designation != null)
                        Player.TaskManager.CancelTask(designation.Task);
                }
            }
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
