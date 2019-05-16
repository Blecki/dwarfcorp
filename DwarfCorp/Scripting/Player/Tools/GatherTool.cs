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
        private static PlayerTool _factory(WorldManager World)
        {
            return new GatherTool(World);
        }

        public GatherTool(WorldManager World)
        {
            this.World = World;
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
                c.Parent == World.ComponentManager.RootComponent;
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            List<Task> assignments = new List<Task>();

            foreach (var resource in bodies.Where(body => CanGather(body)))
            {
                if (World.ChunkManager.IsAboveCullPlane(resource.BoundingBox)) continue;

                if (button == InputManager.MouseButton.Left)
                {
                    assignments.Add(new GatherItemTask(resource));
                }
                else
                {
                    var designation = World.PlayerFaction.Designations.GetEntityDesignation(resource, DesignationType.Gather);
                    if (designation != null)
                        World.Master.TaskManager.CancelTask(designation.Task);
                }
            }

            World.Master.TaskManager.AddTasks(assignments);

            OnConfirm(Faction.FilterMinionsWithCapability(World.PlayerFaction.SelectedMinions, Task.TaskCategory.Gather));
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
            if (World.Master.IsCameraRotationModeActive())
                return;

            World.Master.VoxSelector.Enabled = false;
            World.Master.BodySelector.Enabled = true;
            World.Master.BodySelector.AllowRightClickSelection = true;

            if (World.IsMouseOverGui)
                World.SetMouse(World.MousePointer);
            else
                World.SetMouse(new Gui.MousePointer("mouse", 1, 6));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            NamedImageFrame frame = new NamedImageFrame("newgui/pointers", 32, 6, 0);

            foreach (var body in World.Master.BodySelector.CurrentBodies)
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
