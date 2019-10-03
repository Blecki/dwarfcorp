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
        [ToolFactory("Plant")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new PlantTool(World);
        }

        public string PlantType { get; set; }
        public List<ResourceTypeAmount> RequiredResources { get; set; }

        public PlantTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            World.UserInterface.VoxSelector.SelectionColor = Color.White;
            foreach (var voxel in voxels)
                ValidatePlanting(voxel);
        }

        private bool ValidatePlanting(VoxelHandle voxel)
        {
            if (!voxel.Type.IsSoil)
            {
                World.UserInterface.ShowTooltip("Can only plant on soil!");
                return false;
            }

            if (Library.GetResourceType(PlantType).HasValue(out var plantRes))
            {
                if (plantRes.Tags.Contains("AboveGroundPlant"))
                {
                    if (voxel.Sunlight == false)
                    {
                        World.UserInterface.ShowTooltip("Can only plant " + PlantType + " above ground.");
                        return false;
                    }
                }
                else if (plantRes.Tags.Contains("BelowGroundPlant"))
                {
                    if (voxel.Sunlight)
                    {
                        World.UserInterface.ShowTooltip("Can only plant " + PlantType + " below ground.");
                        return false;
                    }
                }
            }

            if (World.PersistentData.Designations.GetVoxelDesignation(voxel, DesignationType.Plant).HasValue(out var designation))
            {
                World.UserInterface.ShowTooltip("You're already planting here.");
                return false;
            }

            var boundingBox = new BoundingBox(voxel.Coordinate.ToVector3() + new Vector3(0.2f, 0.2f, 0.2f), voxel.Coordinate.ToVector3() + new Vector3(0.8f, 0.8f, 0.8f));
            var entities = World.EnumerateIntersectingObjects(boundingBox, CollisionType.Static).OfType<IVoxelListener>();
            if (entities.Any())
            {
                if (Debugger.Switches.DrawToolDebugInfo)
                {
                    Drawer3D.DrawBox(boundingBox, Color.Red, 0.03f, false);
                    foreach (var entity in entities)
                        Drawer3D.DrawBox((entity as GameComponent).GetBoundingBox(), Color.Yellow, 0.03f, false);
                }

                World.UserInterface.ShowTooltip("There's something in the way.");
                return false;
            }

            var voxelBox = voxel.GetBoundingBox().Expand(-0.2f);
            if (World.EnumerateZones().Any(z => z.GetBoundingBox().Expand(0.1f).Intersects(voxelBox)))
            {
                World.UserInterface.ShowTooltip("Can't plant inside zones.");
                return false;
            }

            World.UserInterface.ShowTooltip("Click to plant.");

            return true;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
            {
                var goals = new List<PlantTask>();
                int count = World.PersistentData.Designations.EnumerateDesignations(DesignationType.Plant).Count();

                foreach (var voxel in voxels)
                {
                    if (count >= 1024)
                    {
                        World.UserInterface.ShowToolPopup("Too many planting tasks.");
                        break;
                    }
                    if (ValidatePlanting(voxel))
                    {
                        count++;
                        var farmTile = new Farm
                        {
                            Voxel = voxel,
                            RequiredResources = RequiredResources,
                            SeedString = PlantType
                        };

                        var task = new PlantTask(farmTile, PlantType)
                        {
                            RequiredResources = RequiredResources
                        };

                        if (voxel.Type.Name != "TilledSoil")
                            farmTile.TargetProgress = 200.0f; // Planting on untilled soil takes longer.

                        goals.Add(task);
                    }
                }

                World.TaskManager.AddTasks(goals);
                
                OnConfirm(World.PlayerFaction.Minions.Where(minion => minion.Stats.IsTaskAllowed(TaskCategory.Plant)).ToList());
            }
            else if (button == InputManager.MouseButton.Right)
            {
                foreach (var voxel in voxels)
                    if (World.PersistentData.Designations.GetVoxelDesignation(voxel, DesignationType.Plant).HasValue(out var designation))
                        World.TaskManager.CancelTask(designation.Task);
            }
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {

        }

        public override void OnBegin(Object Arguments)
        {
            World.UserInterface.VoxSelector.DrawBox = true;
            World.UserInterface.VoxSelector.DrawVoxel = true;
        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.Clear();
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsCameraRotationModeActive())
            {
                World.UserInterface.VoxSelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                World.UserInterface.BodySelector.Enabled = false;
                return;
            }

            World.UserInterface.BodySelector.AllowRightClickSelection = true;

            World.UserInterface.VoxSelector.Enabled = true;
            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            World.UserInterface.BodySelector.Enabled = false;
            ValidatePlanting(World.UserInterface.VoxSelector.VoxelUnderMouse);

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 12));
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }

    }
}
