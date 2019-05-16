using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DigTool : PlayerTool
    {
        [ToolFactory("Dig")]
        private static PlayerTool _factory(GameMaster Master)
        {
            return new DigTool(Master);
        }

        public DigTool(GameMaster Master)
        {
            Player = Master;
        }

        public override void OnBegin()
        {
            Player.VoxSelector.SelectionColor = Color.White;
            Player.VoxSelector.DrawBox = true;
            Player.VoxSelector.DrawVoxel = true;
            Player.World.Tutorial("mine");
        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {

            if (button == InputManager.MouseButton.Left)
            {
                int count = Player.World.PlayerFaction.Designations.EnumerateDesignations(DesignationType.Dig).Count();

                Player.World.Tutorial("slice");
                List<Task> assignments = new List<Task>();
                foreach (var v in refs)
                {
                    if (!v.IsValid || (v.IsEmpty && v.IsExplored) || v.Type.IsInvincible)
                        continue;

                    var boundingBox = v.GetBoundingBox().Expand(-0.1f);
                    var entities = Player.World.EnumerateIntersectingObjects(boundingBox, CollisionType.Static);
                    if (entities.OfType<IVoxelListener>().Any())
                        continue;

                    if (count >= GameSettings.Default.MaxVoxelDesignations)
                    {
                        Player.World.ShowToolPopup("Too many dig designations!");
                        break;
                    }

                    // Todo: Should this be removed from the existing compound task and put in the new one?
                    if (!Player.World.PlayerFaction.Designations.IsVoxelDesignation(v, DesignationType.Dig) && !(Player.World.PlayerFaction.RoomBuilder.IsInRoom(v) || Player.World.PlayerFaction.RoomBuilder.IsBuildDesignation(v)))
                    {
                        var task = new KillVoxelTask(v);
                        task.Hidden = true;
                        assignments.Add(task);
                        count++;
                    }

                }

                Player.TaskManager.AddTasks(assignments);

                var compoundTask = new CompoundTask("DIG A HOLE", Task.TaskCategory.Dig, Task.PriorityType.Medium);
                compoundTask.AddSubTasks(assignments);
                Player.TaskManager.AddTask(compoundTask);

                List<CreatureAI> minions = Faction.FilterMinionsWithCapability(Player.SelectedMinions, Task.TaskCategory.Dig);
                OnConfirm(minions);
            }
            else
            {
                foreach (var r in refs)
                {
                    if (r.IsValid)
                    {
                        var designation = Player.World.PlayerFaction.Designations.GetVoxelDesignation(r, DesignationType.Dig);
                        if (designation != null && designation.Task != null)
                            Player.TaskManager.CancelTask(designation.Task);
                    }
                }
            }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            throw new NotImplementedException();
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

            Player.VoxSelector.Enabled = true;

            if (Player.VoxSelector.VoxelUnderMouse.IsValid && !Player.World.IsMouseOverGui)
            {
                Player.World.ShowTooltip(Player.VoxSelector.VoxelUnderMouse.IsExplored ? Player.VoxSelector.VoxelUnderMouse.Type.Name : "???");
            }

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 1));

            Player.BodySelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }


        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
    }
}
