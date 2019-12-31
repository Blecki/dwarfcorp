using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DestroyZoneTool : PlayerTool
    {
        [ToolFactory("DestroyZone")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new DestroyZoneTool(World);
        }

        public DestroyZoneTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> Voxels, InputManager.MouseButton button)
        {
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
                        OnClose = (sender) => DestroyRoom((sender as Gui.Widgets.Confirm).DialogResult, existingRoom, World)
                    });
            }
        }

        public static void DestroyRoom(Gui.Widgets.Confirm.Result status, Zone room, WorldManager World)
        {
            if (status == Gui.Widgets.Confirm.Result.OKAY)
                World.DestroyZone(room);
        }

        public override void OnBegin(Object Arguments)
        {
        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.Clear();
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsCameraRotationModeActive())
            {
                World.UserInterface.VoxSelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                World.UserInterface.BodySelector.Enabled = false;
                return;
            }

            World.UserInterface.VoxSelector.Enabled = true;
            World.UserInterface.BodySelector.Enabled = false;
            World.UserInterface.VoxSelector.DrawVoxel = true;
            World.UserInterface.VoxSelector.DrawBox = false;
            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 4));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            var v = World.UserInterface.VoxSelector.VoxelUnderMouse;
            if (v.IsValid && !v.IsEmpty)
            {
                var room = World.GetZoneThatContainsVoxel(v);
                if (room != null)
                    Drawer3D.DrawBox(room.GetBoundingBox(), GameSettings.Current.Colors.GetColor("Positive", Color.Green), 0.2f, true);
            }
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }
               
        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            var v = World.UserInterface.VoxSelector.VoxelUnderMouse;

            if (World.IsBuildDesignation(v))
            {
                var order = World.GetBuildDesignation(v);
                if (order == null || order.Order == null)
                    return;

                if (!order.Order.IsBuilt)
                    order.Order.SetTint(GameSettings.Current.Colors.GetColor("Negative", Color.Red));
                else
                    order.ToBuild.SetTint(GameSettings.Current.Colors.GetColor("Negative", Color.Red));
            }
            else if (World.IsInZone(v))
            {
                var existingRoom = World.GetMostLikelyZone(v);
                if (existingRoom != null)
                    existingRoom.SetTint(GameSettings.Current.Colors.GetColor("Negative", Color.Red));
            }
        }        
    }
}
