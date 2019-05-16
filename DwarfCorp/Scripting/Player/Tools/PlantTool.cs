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
        public List<ResourceAmount> RequiredResources { get; set; }

        public PlantTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            World.Master.VoxSelector.SelectionColor = Color.White;
            foreach (var voxel in voxels)
                ValidatePlanting(voxel);
        }

        private bool ValidatePlanting(VoxelHandle voxel)
        {
            if (!voxel.Type.IsSoil)
            {
                World.ShowTooltip("Can only plant on soil!");
                return false;
            }

            if (ResourceLibrary.GetResourceByName(PlantType).Tags.Contains(Resource.ResourceTags.AboveGroundPlant))
            {
                if (voxel.Sunlight == false)
                {
                    World.ShowTooltip("Can only plant " + PlantType + " above ground.");
                    return false;
                }
            }
            else if (ResourceLibrary.GetResourceByName(PlantType).Tags.Contains(Resource.ResourceTags.BelowGroundPlant))
            {
                if (voxel.Sunlight)
                {
                    World.ShowTooltip("Can only plant " + PlantType + " below ground.");
                    return false;
                }
            }

            var designation = World.PlayerFaction.Designations.GetVoxelDesignation(voxel, DesignationType.Plant);

            if (designation != null)
            {
                World.ShowTooltip("You're already planting here.");
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

                World.ShowTooltip("There's something in the way.");
                return false;
            }

            // We have to shrink the bounding box used because for some reason zones overflow their bounds a little during their collision check.
            if (World.PlayerFaction.GetIntersectingRooms(voxel.GetBoundingBox().Expand(-0.2f)).Count > 0)
            {
                World.ShowTooltip("Can't plant inside zones.");
                return false;
            }

            World.ShowTooltip("Click to plant.");

            return true;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
            {
                var goals = new List<PlantTask>();
                int count = World.PlayerFaction.Designations.EnumerateDesignations(DesignationType.Plant).Count();

                foreach (var voxel in voxels)
                {
                    if (count >= 1024)
                    {
                        World.ShowToolPopup("Too many planting tasks.");
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

                World.Master.TaskManager.AddTasks(goals);
                
                OnConfirm(World.PlayerFaction.Minions.Where(minion => minion.Stats.IsTaskAllowed(Task.TaskCategory.Plant)).ToList());
            }
            else if (button == InputManager.MouseButton.Right)
            {
                foreach (var voxel in voxels)
                {
                    var designation = World.PlayerFaction.Designations.GetVoxelDesignation(voxel, DesignationType.Plant);

                    if (designation != null)
                        World.Master.TaskManager.CancelTask(designation.Task);
                }
            }
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {

        }

        public override void OnBegin()
        {
            World.Master.VoxSelector.DrawBox = true;
            World.Master.VoxSelector.DrawVoxel = true;
        }

        public override void OnEnd()
        {
            World.Master.VoxSelector.Clear();
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.Master.IsCameraRotationModeActive())
            {
                World.Master.VoxSelector.Enabled = false;
                World.SetMouse(null);
                World.Master.BodySelector.Enabled = false;
                return;
            }

            World.Master.BodySelector.AllowRightClickSelection = true;

            World.Master.VoxSelector.Enabled = true;
            World.Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            World.Master.BodySelector.Enabled = false;
            ValidatePlanting(World.Master.VoxSelector.VoxelUnderMouse);

            if (World.IsMouseOverGui)
                World.SetMouse(World.MousePointer);
            else
                World.SetMouse(new Gui.MousePointer("mouse", 1, 12));
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }

    }
}
