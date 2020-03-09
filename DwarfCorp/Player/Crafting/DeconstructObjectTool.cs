using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class DeconstructObjectTool : PlayerTool
    {
        [ToolFactory("DeconstructObject")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new DeconstructObjectTool(World);
        }

        private List<GameComponent> selectedBodies = new List<GameComponent>();

        public DeconstructObjectTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnBegin(Object Arguments)
        {

        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.Clear();
        }

        public static bool CanDestroy(GameComponent Body)
        {
            return Body.Tags.Any(tag => tag == "Deconstructable") && !Body.IsReserved;
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            if (bodies.Count == 0)
                return;

            foreach (var body in bodies)
                if (body.Tags.Any(tag => tag == "Deconstructable"))
                {
                    if (body.IsReserved)
                    {
                        World.UserInterface.ShowToolPopup(string.Format("Can't destroy this {0}. It is being used.", body.Name));
                        continue;
                    }

                    if (button == InputManager.MouseButton.Left)
                    {
                        var task = new DeconstructObjectTask(body);
                        World.TaskManager.AddTask(task);
                        World.UserInterface.ShowToolPopup("Will destroy this " + body.Name);
                        OnConfirm(World.PersistentData.SelectedMinions);
                    }
                    else if (button == InputManager.MouseButton.Right)
                    {
                        if (World.PersistentData.Designations.GetEntityDesignation(body, DesignationType.Gather).HasValue(out var designation))
                        {
                            World.TaskManager.CancelTask(designation.Task);
                            World.UserInterface.ShowToolPopup("Destroy cancelled for " + body.Name);
                        }
                    }

                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, body.Position, 0.5f);
                }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            DefaultOnMouseOver(bodies);

            foreach (var body in bodies)
                if (body.Tags.Contains("Deconstructable"))
                {
                    if (body.IsReserved)
                    {
                        World.UserInterface.ShowTooltip("Can't destroy this this " + body.Name + "\nIt is being used.");
                        continue;
                    }
                    World.UserInterface.ShowTooltip("Left click to destroy this " + body.Name);
                    //body.SetVertexColorRecursive(Color.Red);
                }

            //foreach (var body in selectedBodies)
            //    if (!bodies.Contains(body))
            //        body.SetVertexColorRecursive(Color.White);

            selectedBodies = bodies.ToList();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (selectedBodies.Count != 0)
                return;

            var v = World.UserInterface.VoxSelector.VoxelUnderMouse;

            if (World.IsBuildDesignation(v))
                World.DestroyBuildDesignation(v);
            else if (World.IsInZone(v))
            {
                var existingRoom = World.GetMostLikelyZone(v);

                if (existingRoom != null)
                    World.UserInterface.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                    {
                        Text = "Do you want to destroy this " + existingRoom.Type.Name + "?",
                        OnClose = (sender) => destroyDialog_OnClosed((sender as Gui.Widgets.Confirm).DialogResult, existingRoom)
                    });
            }
        }

        void destroyDialog_OnClosed(Gui.Widgets.Confirm.Result status, Zone room)
        {
            if (status == Gui.Widgets.Confirm.Result.OKAY)
                World.DestroyZone(room);
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 9));

            World.UserInterface.VoxSelector.Enabled = true;
            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            World.UserInterface.VoxSelector.DrawBox = false;
            World.UserInterface.VoxSelector.DrawVoxel = false;
            World.UserInterface.BodySelector.Enabled = true;
            World.UserInterface.BodySelector.AllowRightClickSelection = true;
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            if (selectedBodies.Count == 0)
            {
                var v = World.UserInterface.VoxSelector.VoxelUnderMouse;
                if (v.IsValid && !v.IsEmpty)
                {
                    var room = World.GetZoneThatContainsVoxel(v);
                    if (room != null)
                        Drawer3D.DrawBox(room.GetBoundingBox(), GameSettings.Current.Colors.GetColor("Positive", Color.Green), 0.2f, true);
                }
            }
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }
    }
}
