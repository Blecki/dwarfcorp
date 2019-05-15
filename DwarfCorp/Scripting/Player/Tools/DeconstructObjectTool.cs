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
        [ToolFactory("DeconstructObjects")] // Todo: Normalize name
        private static PlayerTool _factory(GameMaster Master)
        {
            return new DeconstructObjectTool(Master);
        }

        public DeconstructObjectTool(GameMaster Master)
        {
            Player = Master;
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }


        public bool CanDestroy(GameComponent body)
        {
            return body.Tags.Any(tag => tag == "Deconstructable") && !body.IsReserved;
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
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


        private List<GameComponent> selectedBodies = new List<GameComponent>();

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
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

        public override void Render3D(DwarfGame game, DwarfTime time)
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

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }


    }

}
