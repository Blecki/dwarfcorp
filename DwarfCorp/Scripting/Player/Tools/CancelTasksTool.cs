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
        private static PlayerTool _factory(GameMaster Master)
        {
            return new CancelTasksTool(Master);
        }

        public CancelTasksTool(GameMaster Master)
        {
            Player = Master;
        }

        public Gui.Widgets.CancelToolOptions Options;

        public override void OnBegin()
        {
            Player.World.Tutorial("cancel-tasks");
            Player.VoxSelector.SelectionColor = Color.Red;
            Player.VoxSelector.DrawBox = true;
            Player.VoxSelector.DrawVoxel = true;
        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            if (Options.Voxels.CheckState)
                foreach (var r in refs)
                {
                    if (r.IsValid)
                    {
                        var designations = Player.Faction.Designations.EnumerateDesignations(r).ToList();
                        foreach (var des in designations)
                            if (des.Task != null)
                                Player.TaskManager.CancelTask(des.Task);
                    }
                }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
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

            Player.VoxSelector.Enabled = Options.Voxels.CheckState;
            Player.BodySelector.Enabled = Options.Entities.CheckState;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 0, 0));

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
                    foreach (var des in Player.Faction.Designations.EnumerateEntityDesignations(body).ToList())
                        if (des.Task != null)
                            Player.TaskManager.CancelTask(des.Task);
                }
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
    }
}
