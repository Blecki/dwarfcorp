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
            var v = World.Master.VoxSelector.VoxelUnderMouse;

            if (World.PlayerFaction.RoomBuilder.IsBuildDesignation(v))
            {
                BuildVoxelOrder vox = World.PlayerFaction.RoomBuilder.GetBuildDesignation(v);
                if (vox != null && vox.Order != null)
                {
                    vox.Order.Destroy();
                    if (vox.Order.DisplayWidget != null)
                        World.Gui.DestroyWidget(vox.Order.DisplayWidget);
                    World.PlayerFaction.RoomBuilder.BuildDesignations.Remove(vox.Order);
                    World.PlayerFaction.RoomBuilder.DesignatedRooms.Remove(vox.Order.ToBuild);
                }
            }
            else if (World.PlayerFaction.RoomBuilder.IsInRoom(v))
            {
                Room existingRoom = World.PlayerFaction.RoomBuilder.GetMostLikelyRoom(v);

                if (existingRoom != null)
                    World.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                    {
                        Text = "Do you want to destroy this " + existingRoom.RoomData.Name + "?",
                        OnClose = (sender) => DestroyRoom((sender as Gui.Widgets.Confirm).DialogResult, existingRoom, World.PlayerFaction, World)
                    });
            }
        }

        public static void DestroyRoom(Gui.Widgets.Confirm.Result status, Room room, Faction Faction, WorldManager World)
        {
            if (status == Gui.Widgets.Confirm.Result.OKAY)
            {
                Faction.RoomBuilder.DesignatedRooms.Remove(room);

                List<BuildVoxelOrder> existingDesignations = Faction.RoomBuilder.GetDesignationsAssociatedWithRoom(room);
                BuildRoomOrder buildRoomDes = null;
                foreach (BuildVoxelOrder des in existingDesignations)
                {
                    des.Order.VoxelOrders.Remove(des);
                    buildRoomDes = des.Order;
                }
                if (buildRoomDes != null && buildRoomDes.DisplayWidget != null)
                {
                    World.Gui.DestroyWidget(buildRoomDes.DisplayWidget);
                }
                Faction.RoomBuilder.BuildDesignations.Remove(buildRoomDes);

                room.Destroy();
            }
        }

        public override void OnBegin()
        {
        }

        public override void OnEnd()
        {
            World.Master.VoxSelector.Clear();
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.Master.IsCameraRotationModeActive())
            {
                World.Master.VoxSelector.Enabled = false;
                World.SetMouse(null);
                World.Master.BodySelector.Enabled = false;
                return;
            }

            World.Master.VoxSelector.Enabled = true;
            World.Master.BodySelector.Enabled = false;
            World.Master.VoxSelector.DrawVoxel = true;
            World.Master.VoxSelector.DrawBox = false;
            World.Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            

            if (World.IsMouseOverGui)
                World.SetMouse(World.MousePointer);
            else
                World.SetMouse(new Gui.MousePointer("mouse", 1, 4));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            var v = World.Master.VoxSelector.VoxelUnderMouse;
            if (v.IsValid && !v.IsEmpty)
            {
                var room = World.PlayerFaction.RoomBuilder.GetRoomThatContainsVoxel(v);
                if (room != null)
                    Drawer3D.DrawBox(room.GetBoundingBox(), GameSettings.Default.Colors.GetColor("Positive", Color.Green), 0.2f, true);
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
            var v = World.Master.VoxSelector.VoxelUnderMouse;

            if (World.PlayerFaction.RoomBuilder.IsBuildDesignation(v))
            {
                var order = World.PlayerFaction.RoomBuilder.GetBuildDesignation(v);
                if (order == null || order.Order == null)
                    return;

                if (!order.Order.IsBuilt)
                    order.Order.SetTint(GameSettings.Default.Colors.GetColor("Negative", Color.Red));
                else
                    order.ToBuild.SetTint(GameSettings.Default.Colors.GetColor("Negative", Color.Red));
            }
            else if (World.PlayerFaction.RoomBuilder.IsInRoom(v))
            {
                Room existingRoom = World.PlayerFaction.RoomBuilder.GetMostLikelyRoom(v);
                if (existingRoom != null)
                    existingRoom.SetTint(GameSettings.Default.Colors.GetColor("Negative", Color.Red));
            }
        }

        
    }
}
