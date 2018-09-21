// BuildTool.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
    /// <summary>
    /// Using this tool, the player can specify regions of voxels to be
    /// turned into rooms.
    /// </summary>
    public class DestroyZoneTool : PlayerTool
    {
        private Faction Faction { get { return Player.Faction; } }
        private WorldManager World { get { return Player.World; } }

        public override void OnVoxelsSelected(List<VoxelHandle> Voxels, InputManager.MouseButton button)
        {
            var v = Player.VoxSelector.VoxelUnderMouse;

            if (Faction.RoomBuilder.IsBuildDesignation(v))
            {
                BuildVoxelOrder vox = Faction.RoomBuilder.GetBuildDesignation(v);
                if (vox != null && vox.Order != null)
                {
                    vox.Order.Destroy();
                    if (vox.Order.DisplayWidget != null)
                        World.Gui.DestroyWidget(vox.Order.DisplayWidget);
                    Faction.RoomBuilder.BuildDesignations.Remove(vox.Order);
                    Faction.RoomBuilder.DesignatedRooms.Remove(vox.Order.ToBuild);
                }
            }
            else if (Faction.RoomBuilder.IsInRoom(v))
            {
                Room existingRoom = Faction.RoomBuilder.GetMostLikelyRoom(v);

                if (existingRoom != null)
                    World.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                    {
                        Text = "Do you want to destroy this " + existingRoom.RoomData.Name + "?",
                        OnClose = (sender) => DestroyRoom((sender as Gui.Widgets.Confirm).DialogResult, existingRoom, Faction, World)
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
            Player.VoxSelector.Clear();
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;
            Player.VoxSelector.DrawVoxel = true;
            Player.VoxSelector.DrawBox = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));
        }

        public override void Render(DwarfGame game, DwarfTime time)
        {
            var v = Player.VoxSelector.VoxelUnderMouse;
            if (v.IsValid && !v.IsEmpty)
            {
                var room = Faction.RoomBuilder.GetRoomThatContainsVoxel(v);
                if (room != null)
                    Drawer3D.DrawBox(room.GetBoundingBox(), GameSettings.Default.Colors.GetColor("Positive", Color.Green), 0.2f, true);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            var v = Player.VoxSelector.VoxelUnderMouse;

            if (Faction.RoomBuilder.IsBuildDesignation(v))
            {
                var order = Faction.RoomBuilder.GetBuildDesignation(v);
                if (order == null || order.Order == null)
                    return;

                if (!order.Order.IsBuilt)
                    order.Order.SetTint(GameSettings.Default.Colors.GetColor("Negative", Color.Red));
                else
                    order.ToBuild.SetTint(GameSettings.Default.Colors.GetColor("Negative", Color.Red));
            }
            else if (Faction.RoomBuilder.IsInRoom(v))
            {
                Room existingRoom = Faction.RoomBuilder.GetMostLikelyRoom(v);
                if (existingRoom != null)
                    existingRoom.SetTint(GameSettings.Default.Colors.GetColor("Negative", Color.Red));
            }
        }

        
    }
}
