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
        private static PlayerTool _factory(GameMaster Master)
        {
            return new PlantTool(Master);
        }

        public string PlantType { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; }

        public PlantTool(GameMaster Master)
        {
            Player = Master;
        }

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
                Player.World.ShowTooltip("Can only plant on soil!");
                return false;
            }

            if (ResourceLibrary.GetResourceByName(PlantType).Tags.Contains(Resource.ResourceTags.AboveGroundPlant))
            {
                if (voxel.Sunlight == false)
                {
                    Player.World.ShowTooltip("Can only plant " + PlantType + " above ground.");
                    return false;
                }
            }
            else if (ResourceLibrary.GetResourceByName(PlantType).Tags.Contains(Resource.ResourceTags.BelowGroundPlant))
            {
                if (voxel.Sunlight)
                {
                    Player.World.ShowTooltip("Can only plant " + PlantType + " below ground.");
                    return false;
                }
            }

            var designation = Player.World.PlayerFaction.Designations.GetVoxelDesignation(voxel, DesignationType.Plant);

            if (designation != null)
            {
                Player.World.ShowTooltip("You're already planting here.");
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
                        Drawer3D.DrawBox((entity as GameComponent).GetBoundingBox(), Color.Yellow, 0.03f, false);
                }

                Player.World.ShowTooltip("There's something in the way.");
                return false;
            }

            // We have to shrink the bounding box used because for some reason zones overflow their bounds a little during their collision check.
            if (Player.World.PlayerFaction.GetIntersectingRooms(voxel.GetBoundingBox().Expand(-0.2f)).Count > 0)
            {
                Player.World.ShowTooltip("Can't plant inside zones.");
                return false;
            }

            Player.World.ShowTooltip("Click to plant.");

            return true;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
            {
                var goals = new List<PlantTask>();
                int count = Player.World.PlayerFaction.Designations.EnumerateDesignations(DesignationType.Plant).Count();

                foreach (var voxel in voxels)
                {
                    if (count >= 1024)
                    {
                        Player.World.ShowToolPopup("Too many planting tasks.");
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

                Player.TaskManager.AddTasks(goals);
                
                OnConfirm(Player.World.PlayerFaction.Minions.Where(minion => minion.Stats.IsTaskAllowed(Task.TaskCategory.Plant)).ToList());
            }
            else if (button == InputManager.MouseButton.Right)
            {
                foreach (var voxel in voxels)
                {
                    var designation = Player.World.PlayerFaction.Designations.GetVoxelDesignation(voxel, DesignationType.Plant);

                    if (designation != null)
                        Player.TaskManager.CancelTask(designation.Task);
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
            Player.VoxSelector.DrawBox = true;
            Player.VoxSelector.DrawVoxel = true;
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

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }

    }
}
