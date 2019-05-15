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
    public class GatherTool : PlayerTool
    {
        [ToolFactory("Gather")]
        private static PlayerTool _factory(GameMaster Master)
        {
            return new GatherTool(Master);
        }

        public GatherTool(GameMaster Master)
        {
            Player = Master;
        }

        public GatherTool()
        {

        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        public bool CanGather(GameComponent c)
        {
            return c.Tags.Contains("Resource") &&
                c.Active &&
                c.IsVisible &&
                c.Parent == Player.World.ComponentManager.RootComponent;
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            List<Task> assignments = new List<Task>();

            foreach (var resource in bodies.Where(body => CanGather(body)))
            {
                if (Player.World.ChunkManager.IsAboveCullPlane(resource.BoundingBox)) continue;

                if (button == InputManager.MouseButton.Left)
                {
                    assignments.Add(new GatherItemTask(resource));
                }
                else
                {
                    var designation = Player.Faction.Designations.GetEntityDesignation(resource, DesignationType.Gather);
                    if (designation != null)
                        Player.TaskManager.CancelTask(designation.Task);
                }
            }

            Player.TaskManager.AddTasks(assignments);

            OnConfirm(Faction.FilterMinionsWithCapability(Player.World.Master.SelectedMinions, Task.TaskCategory.Gather));
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            DefaultOnMouseOver(bodies);
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
                return;

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 6));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            NamedImageFrame frame = new NamedImageFrame("newgui/pointers", 32, 6, 0);

            foreach (var body in Player.BodySelector.CurrentBodies)
            {
                if (body.Tags.Contains("Resource"))
                {
                    Drawer2D.DrawText(body.Name, body.Position, Color.White, Color.Black);
                    BoundingBox bounds = body.BoundingBox;
                    Drawer3D.DrawBox(bounds, Color.Orange, 0.02f, false);
                    Drawer2D.DrawSprite(frame, body.Position + Vector3.One * 0.5f, Vector2.One * 0.5f, Vector2.Zero, new Color(255, 255, 255, 100));
                }
            }
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

    }
}
