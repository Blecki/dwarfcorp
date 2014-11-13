using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;


namespace DwarfCorp
{
    /// <summary>
    /// The BuildRoom designator keeps track of voxels selected by the player and turns
    /// them into BuildRoom designations (so that dwarves can build BuildRoom).
    /// </summary>
    [JsonObject(IsReference = true)]
    public class RoomBuilder
    {
        public List<Room> DesignatedRooms { get; set; }
        public List<BuildRoomOrder> BuildDesignations { get; set; }
        public RoomData CurrentRoomData { get; set; }
        public Faction Faction { get; set; }


        public List<Room> FilterRoomsByType(string type)
        {
            return DesignatedRooms.Where(r => r.RoomData.Name == type).ToList();
        }

        public RoomBuilder()
        {
            
        }

        public RoomBuilder(Faction faction)
        {
            DesignatedRooms = new List<Room>();
            BuildDesignations = new List<BuildRoomOrder>();
            CurrentRoomData = RoomLibrary.GetData("BedRoom");
            Faction = faction;
        }


        public bool IsInRoom(Voxel v)
        {
            Voxel vRef = v;
            return DesignatedRooms.Any(r => r.ContainsVoxel(vRef)) || Faction.IsInStockpile(v);
        }

        public bool IsBuildDesignation(Voxel v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).Any(buildDesignation => buildDesignation.Voxel.Equals(v));
        }

        public bool IsBuildDesignation(Room r)
        {
            return BuildDesignations.Any(room => room.ToBuild == r);
        }

        public BuildVoxelOrder GetBuildDesignation(Room v)
        {
            return (from room in BuildDesignations
                where room.ToBuild == v
                where room.VoxelOrders.Count > 0
                select room.VoxelOrders[0]).FirstOrDefault();
        }

        public BuildVoxelOrder GetBuildDesignation(Voxel v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).FirstOrDefault(buildDesignation => (buildDesignation.Voxel.Position - v.Position).LengthSquared() < 0.1f);
        }

        public BuildRoomOrder GetMostLikelyDesignation(Voxel v)
        {
            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            return (from room in BuildDesignations
                from buildDesignation in room.VoxelOrders
                where larger.Intersects(buildDesignation.Voxel.GetBoundingBox())
                select room).FirstOrDefault();
        }

        public Room GetMostLikelyRoom(Voxel v)
        {
            Voxel vRef = v;
            foreach(Room r in DesignatedRooms.Where(r => r.ContainsVoxel(vRef)))
            {
                return r;
            }

            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            return (from room in BuildDesignations
                from buildDesignation in room.VoxelOrders
                where larger.Intersects(buildDesignation.Voxel.GetBoundingBox())
                select buildDesignation.ToBuild).FirstOrDefault();
        }

        public List<BuildVoxelOrder> GetDesignationsAssociatedWithRoom(Room room)
        {
            return (from roomDesignation in BuildDesignations
                from des in roomDesignation.VoxelOrders
                where des.ToBuild == room
                select des).ToList();
        }

        public void Render(GameTime game, GraphicsDevice graphics)
        {
            foreach(Room room in DesignatedRooms)
            {
                if(room.IsBuilt)
                    Drawer3D.DrawBox(room.GetBoundingBox(), Color.White, 0.1f, true);
            }


            foreach(BuildRoomOrder roomDesignation in BuildDesignations)
            {
                BoundingBox roomBox = roomDesignation.GetBoundingBox();
                roomBox.Max = new Vector3(roomBox.Max.X, roomBox.Max.Y + 0.1f, roomBox.Max.Z);

                Drawer3D.DrawBox(roomBox, Color.White, 0.1f, true);
                List<BuildVoxelOrder> removals = new List<BuildVoxelOrder>();
                foreach(BuildVoxelOrder des in roomDesignation.VoxelOrders)
                {
                    Drawer3D.DrawBox(des.Voxel.GetBoundingBox(), Color.LightBlue, 0.05f, true);
                    BoundingBox centerBox = des.Voxel.GetBoundingBox();
                    centerBox.Min += new Vector3(0.7f, 1.1f, 0.7f);
                    centerBox.Max += new Vector3(-0.7f, 0.2f, -0.7f);
                    Drawer3D.DrawBox(centerBox, Color.LightBlue, 0.01f, true);

                    if (des.Voxel.IsEmpty)
                    {
                        removals.Add(des);
                    }
                }

                foreach(BuildVoxelOrder des in removals)
                {
                    roomDesignation.VoxelOrders.Remove(des);
                }

                Vector3 textLocation = (roomBox.Max - roomBox.Min) / 2.0f + roomBox.Min + new Vector3(0, 2.0f, 0);
                Drawer2D.DrawTextBox(roomDesignation.GetTextDisplay(), textLocation);
            }
        }

        public void CheckRemovals()
        {
            List<BuildRoomOrder> toRemove = BuildDesignations.Where(build => build.IsBuilt).ToList();

            foreach(BuildRoomOrder build in toRemove)
            {
                BuildDesignations.Remove(build);
            }
        }

        public void OnVoxelDestroyed(Voxel voxDestroyed)
        {
            List<Room> toDestroy = new List<Room>();
            Voxel vRef = voxDestroyed;

            lock (DesignatedRooms)
            {
                foreach (Room r in DesignatedRooms)
                {
                    r.RemoveVoxel(vRef);
                    if (r.Voxels.Count == 0)
                    {
                        toDestroy.Add(r);
                    }
                }

                foreach (Room r in toDestroy)
                {
                    DesignatedRooms.Remove(r);
                    r.Destroy();
                }
            }
        }

        public void Update(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            PlayState.GUI.IsMouseVisible = true;
        }

        private void BuildNewVoxels(IEnumerable<Voxel> refs)
        {
            BuildRoomOrder order = null;
            IEnumerable<Voxel> designations = refs as IList<Voxel> ?? refs.ToList();
            IEnumerable<Voxel> nonEmpty = designations.Select(r => r).Where(v => !v.IsEmpty);
            foreach(Voxel v in nonEmpty)
            {
                if(IsBuildDesignation(v) || IsInRoom(v))
                {
                    continue;
                }

                if(order == null)
                {
                    order = GetMostLikelyDesignation(v);
                }

                if (order != null)
                {
                    order.VoxelOrders.Add(new BuildVoxelOrder(order, order.ToBuild, v));
                }
                else
                {
                    if(CurrentRoomData != RoomLibrary.GetData("Stockpile"))
                    {
                        Room toBuild = RoomLibrary.CreateRoom(CurrentRoomData.Name, designations.ToList(), true);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildRoomOrder(toBuild, this.Faction);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild, v));
                        BuildDesignations.Add(order);
                    }
                    else
                    {
                        Stockpile toBuild = new Stockpile("Stockpile " + Stockpile.NextID(), PlayState.ChunkManager);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildStockpileOrder(toBuild, this.Faction);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild, v));
                        BuildDesignations.Add(order);
                    }
                }
            }

            if(order != null)
            {
                TaskManager.AssignTasks(new List<Task>()
                {
                    new BuildRoomTask(order)
                }, Faction.FilterMinionsWithCapability(PlayState.Master.SelectedMinions, GameMaster.ToolMode.Build));
            }
        }

        public void VoxelsSelected(List<Voxel> refs, InputManager.MouseButton button)
        {
            if(CurrentRoomData == null)
            {
                return;
            }

            if(button == InputManager.MouseButton.Left)
            {
                if (Faction.FilterMinionsWithCapability(Faction.SelectedMinions, GameMaster.ToolMode.Build).Count == 0)
                {
                    PlayState.GUI.ToolTipManager.Popup("None of the selected units can build rooms.");
                }
                else if (CurrentRoomData.Verify(refs, Faction))
                {
                    BuildNewVoxels(refs);    
                }
            }
            else
            {
                DeleteVoxels(refs);
            }
        }

        private void DeleteVoxels(IEnumerable<Voxel> refs )
        {
            foreach(Voxel v in refs.Select(r => r).Where(v => !v.IsEmpty))
            {
                if(IsBuildDesignation(v))
                {
                    BuildVoxelOrder vox = GetBuildDesignation(v);
                    vox.ToBuild.Destroy();
                    BuildDesignations.Remove(vox.Order);
                }
                else if(IsInRoom(v))
                {
                    Room existingRoom = GetMostLikelyRoom(v);

                    if (existingRoom == null)
                    {
                        continue;
                    }

                    Dialog destroyDialog = Dialog.Popup(PlayState.GUI, "Destroy room?",
                        "Do you want to destroy this " + existingRoom.RoomData.Name + "?", Dialog.ButtonType.OkAndCancel);
                    destroyDialog.OnClosed += (status) => destroyDialog_OnClosed(status, existingRoom);
                    break;
                }
            }
        }

        void destroyDialog_OnClosed(Dialog.ReturnStatus status, Room room)
        {
            if (status == Dialog.ReturnStatus.Ok)
            {
                DesignatedRooms.Remove(room);

                List<BuildVoxelOrder> existingDesignations = GetDesignationsAssociatedWithRoom(room);
                BuildRoomOrder buildRoomDes = null;
                foreach (BuildVoxelOrder des in existingDesignations)
                {
                    des.Order.VoxelOrders.Remove(des);
                    buildRoomDes = des.Order;
                }

                BuildDesignations.Remove(buildRoomDes);

                room.Destroy();
            }
        }
    }

}