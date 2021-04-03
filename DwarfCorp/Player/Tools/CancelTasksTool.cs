using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class CancelTasksTool : PlayerTool
    {
        [ToolFactory("CancelTasks")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new CancelTasksTool(World);
        }

        public CancelTasksTool(WorldManager World)
        {
            this.World = World;
        }

        public Gui.Widgets.CancelToolOptions Options;

        public override void OnBegin(Object Arguments)
        {
            Options = Arguments as Gui.Widgets.CancelToolOptions;
            World.Tutorial("cancel-tasks");
            World.UserInterface.VoxSelector.SelectionColor = Color.Red;
            World.UserInterface.VoxSelector.DrawBox = true;
            World.UserInterface.VoxSelector.DrawVoxel = true;
        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.Clear();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            if (Options.Voxels.CheckState)
                foreach (var r in refs)
                {
                    if (r.IsValid)
                    {
                        var designations = World.PersistentData.Designations.EnumerateDesignations(r).ToList();
                        foreach (var des in designations)
                            if (des.Task != null)
                                World.TaskManager.CancelTask(des.Task);
                    }
                }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World == null || World.UserInterface == null || World.UserInterface.VoxSelector == null || World.UserInterface.BodySelector == null)
                return;
            if (Options == null || Options.Voxels == null || Options.Entities == null)
                return;

            if (World.UserInterface.IsCameraRotationModeActive())
            {
                World.UserInterface.VoxSelector.Enabled = false;
                World.UserInterface.BodySelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                return;
            }

            World.UserInterface.VoxSelector.Enabled = Options.Voxels.CheckState;
            World.UserInterface.BodySelector.Enabled = Options.Entities.CheckState;
            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 0, 0));

        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            if (Options.Entities.CheckState)
                foreach (var body in bodies)
                {
                    foreach (var des in World.PersistentData.Designations.EnumerateEntityDesignations(body).ToList())
                        if (des.Task != null)
                            World.TaskManager.CancelTask(des.Task);
                }
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
    }
}
