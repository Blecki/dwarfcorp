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
    public class ChopTool : PlayerTool
    {
        [ToolFactory("Chop")]
        private static PlayerTool _factory(GameMaster Master)
        {
            return new ChopTool(Master);
        }

        public ChopTool(GameMaster Master)
        {
            Player = Master;
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            if (bodies == null)
                return;

            var treesPicked = bodies.Where(c => c != null && c.Tags.Contains("Vegetation"));

            if (treesPicked.Any())
                Player.World.ShowTooltip("Click to harvest this plant. Right click to cancel.");
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

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            NamedImageFrame frame = new NamedImageFrame("newgui/pointers", 32, 5, 0);
            foreach (GameComponent tree in Player.BodySelector.CurrentBodies)
            {
                if (tree.Tags.Contains("Vegetation"))
                {
                    Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                    Drawer2D.DrawSprite(frame, tree.BoundingBox.Center(), Vector2.One * 0.5f, Vector2.Zero, new Color(255, 255, 255, 100));
                }
            }
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }


        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            var plantsPicked = bodies.Where(c => c.Tags.Contains("Vegetation"));

            if (button == InputManager.MouseButton.Left)
            {
                List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Player.World.PlayerFaction.SelectedMinions, Task.TaskCategory.Chop);
                List<Task> tasks = new List<Task>();

                foreach (var plant in plantsPicked)
                {
                    if (!plant.IsVisible) continue;
                    if (Player.World.ChunkManager.IsAboveCullPlane(plant.BoundingBox)) continue;
                    tasks.Add(new ChopEntityTask(plant));
                }

                Player.TaskManager.AddTasks(tasks);
                if (tasks.Count > 0 && minions.Count > 0)
                    OnConfirm(minions);
            }
            else if (button == InputManager.MouseButton.Right)
            {
                foreach (var plant in plantsPicked)
                {
                    if (!plant.IsVisible) continue;
                    if (Player.World.ChunkManager.IsAboveCullPlane(plant.BoundingBox)) continue;
                    var designation = Player.World.PlayerFaction.Designations.GetEntityDesignation(plant, DesignationType.Chop);
                    if (designation != null)
                        Player.TaskManager.CancelTask(designation.Task);
                }
            }
        }
    }
}
