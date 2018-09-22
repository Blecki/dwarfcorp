// GatherTool.cs
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
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class DeconstructObjectTool : PlayerTool
    {
        public DeconstructObjectTool()
        {
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }


        public bool CanDestroy(Body body)
        {
            return body.Tags.Any(tag => tag == "Deconstructable") && !body.IsReserved;
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (bodies.Count == 0)
                return;

            foreach (var body in bodies)
            {
                if (body.Tags.Any(tag => tag == "Deconstructable"))
                {
                    if (body.IsReserved)
                    {
                        Player.World.ShowToolPopup(string.Format("Can't destroy this {0}. It is being used.", body.Name));
                        continue;
                    }
                    body.Die();
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, body.Position,
                    0.5f);
                }
            }          
        }


        private List<Body> selectedBodies = new List<Body>();

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            DefaultOnMouseOver(bodies);

            foreach (var body in bodies)
            {
                if (body.Tags.Contains("Deconstructable"))
                {
                    if (body.IsReserved)
                    {
                        Player.World.ShowTooltip("Can't destroy this this " + body.Name + "\nIt is being used.");
                        continue;
                    }
                    Player.World.ShowTooltip("Left click to destroy this " + body.Name);
                    body.SetVertexColorRecursive(Color.Red);
                }
            }

            foreach(var body in selectedBodies)
            {
                if (!bodies.Contains(body))
                {
                    body.SetVertexColorRecursive(Color.White);
                }
            }

            selectedBodies = bodies.ToList();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (selectedBodies.Count != 0)
                return;

            var v = Player.VoxSelector.VoxelUnderMouse;

            if (Player.Faction.RoomBuilder.IsBuildDesignation(v))
            {
                BuildVoxelOrder vox = Player.Faction.RoomBuilder.GetBuildDesignation(v);
                if (vox != null && vox.Order != null)
                {
                    vox.Order.Destroy();
                    if (vox.Order.DisplayWidget != null)
                        Player.World.Gui.DestroyWidget(vox.Order.DisplayWidget);
                    Player.Faction.RoomBuilder.BuildDesignations.Remove(vox.Order);
                    Player.Faction.RoomBuilder.DesignatedRooms.Remove(vox.Order.ToBuild);
                }
            }
            else if (Player.Faction.RoomBuilder.IsInRoom(v))
            {
                Room existingRoom = Player.Faction.RoomBuilder.GetMostLikelyRoom(v);

                if (existingRoom != null)
                    Player.World.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                    {
                        Text = "Do you want to destroy this " + existingRoom.RoomData.Name + "?",
                        OnClose = (sender) => destroyDialog_OnClosed((sender as Gui.Widgets.Confirm).DialogResult, existingRoom)
                    });
            }
        }


        void destroyDialog_OnClosed(Gui.Widgets.Confirm.Result status, Room room)
        {
            if (status == Gui.Widgets.Confirm.Result.OKAY)
            {
                Player.Faction.RoomBuilder.DesignatedRooms.Remove(room);

                List<BuildVoxelOrder> existingDesignations = Player.Faction.RoomBuilder.GetDesignationsAssociatedWithRoom(room);
                BuildRoomOrder buildRoomDes = null;
                foreach (BuildVoxelOrder des in existingDesignations)
                {
                    des.Order.VoxelOrders.Remove(des);
                    buildRoomDes = des.Order;
                }
                if (buildRoomDes != null && buildRoomDes.DisplayWidget != null)
                {
                    Player.World.Gui.DestroyWidget(buildRoomDes.DisplayWidget);
                }
                Player.Faction.RoomBuilder.BuildDesignations.Remove(buildRoomDes);

                room.Destroy();
            }
        }


        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 9));

            Player.VoxSelector.Enabled = true;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            Player.VoxSelector.DrawBox = false;
            Player.VoxSelector.DrawVoxel = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            if (selectedBodies.Count == 0)
            {
                var v = Player.VoxSelector.VoxelUnderMouse;
                if (v.IsValid && !v.IsEmpty)
                {
                    var room = Player.Faction.RoomBuilder.GetRoomThatContainsVoxel(v);
                    if (room != null)
                        Drawer3D.DrawBox(room.GetBoundingBox(), GameSettings.Default.Colors.GetColor("Positive", Color.Green), 0.2f, true);
                }
            }
        }
    }

}
