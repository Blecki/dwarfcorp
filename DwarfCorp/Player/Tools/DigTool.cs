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
        private static PlayerTool _factory(WorldManager World)
        {
            return new DigTool(World);
        }

        public DigTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnBegin(Object Arguments)
        {
            World.UserInterface.VoxSelector.SelectionColor = Color.White;
            World.UserInterface.VoxSelector.DrawBox = true;
            World.UserInterface.VoxSelector.DrawVoxel = true;
            World.Tutorial("mine");
        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.Clear();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {

            if (button == InputManager.MouseButton.Left)
            {
                int count = World.PersistentData.Designations.EnumerateDesignations(DesignationType.Dig).Count();

                World.Tutorial("slice");
                List<Task> assignments = new List<Task>();
                foreach (var v in refs)
                {
                    if (!v.IsValid || (v.IsEmpty && v.IsExplored) || v.Type.IsInvincible)
                        continue;

                    var boundingBox = v.GetBoundingBox().Expand(-0.1f, 0.1f, -0.1f);
                    var reject = false;
                    foreach (var entity in World.EnumerateIntersectingRootObjects(boundingBox, CollisionType.Static))
                    {
                        foreach (var component in entity.EnumerateAll().Where(e => e.BoundingBox.Intersects(boundingBox)).OfType<IVoxelListener>())
                        {
                            reject = true;
                            break;
                        }
                        if (reject)
                            break;
                    }
                    if (reject)
                        continue;

                    if (count >= GameSettings.Current.MaxVoxelDesignations)
                    {
                        World.UserInterface.ShowToolPopup("Too many dig designations!");
                        break;
                    }

                    // Todo: Should this be removed from the existing compound task and put in the new one?
                    if (!World.PersistentData.Designations.IsVoxelDesignation(v, DesignationType.Dig) && !(World.IsInZone(v) || World.IsBuildDesignation(v)))
                    {
                        var task = new KillVoxelTask(v);
                        task.Hidden = true;
                        assignments.Add(task);
                        count++;
                    }

                }

                if (assignments.Count > 0)
                {
                    World.TaskManager.AddTasks(assignments);

                    var compoundTask = new CompoundTask("DIG A HOLE", TaskCategory.Dig, TaskPriority.Medium);
                    compoundTask.AddSubTasks(assignments);
                    World.TaskManager.AddTask(compoundTask);

                    var minions = Faction.FilterMinionsWithCapability(World.PersistentData.SelectedMinions, TaskCategory.Dig);
                    OnConfirm(minions);
                }
            }
            else
            {
                foreach (var r in refs)
                {
                    if (r.IsValid)
                    {
                        if (World.PersistentData.Designations.GetVoxelDesignation(r, DesignationType.Dig).HasValue(out var designation) && designation.Task != null) // Todo: Is this necessary?
                            World.TaskManager.CancelTask(designation.Task);
                    }
                }
            }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            //throw new NotImplementedException();
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsCameraRotationModeActive())
            {
                World.UserInterface.VoxSelector.Enabled = false;
                World.UserInterface.BodySelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                return;
            }

            World.UserInterface.VoxSelector.Enabled = true;

            if (World.UserInterface.VoxSelector.VoxelUnderMouse.IsValid && !World.UserInterface.IsMouseOverGui)
            {
                World.UserInterface.ShowTooltip(World.UserInterface.VoxSelector.VoxelUnderMouse.IsExplored ? World.UserInterface.VoxSelector.VoxelUnderMouse.Type.Name : "???");
            }

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 1));

            World.UserInterface.BodySelector.Enabled = false;
            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
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
