using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
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
        public RoomType CurrentRoomType { get; set; }
        public Faction Faction { get; set; }


        public List<Room> FilterRoomsByType(string type)
        {
            return DesignatedRooms.Where(r => r.RoomType.Name == type).ToList();
        }

        public RoomBuilder()
        {
            
        }

        public RoomBuilder(Faction faction)
        {
            DesignatedRooms = new List<Room>();
            BuildDesignations = new List<BuildRoomOrder>();
            CurrentRoomType = RoomLibrary.GetType("BedRoom");
            Faction = faction;
        }


        public bool IsInRoom(Voxel v)
        {
            VoxelRef vRef = v.GetReference();
            return DesignatedRooms.Any(r => r.ContainsVoxel(vRef)) || Faction.IsInStockpile(v) || Faction.GetIntersectingRooms(v.GetBoundingBox()).Count > 0;
        }

        public bool IsBuildDesignation(Voxel v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).Any(buildDesignation => (buildDesignation.Voxel.WorldPosition - v.Position).LengthSquared() < 0.1f);
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
            return BuildDesignations.SelectMany(room => room.VoxelOrders).FirstOrDefault(buildDesignation => (buildDesignation.Voxel.WorldPosition - v.Position).LengthSquared() < 0.1f);
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
            VoxelRef vRef = v.GetReference();
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

                    if (des.Voxel.GetVoxel(false) == null)
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
            VoxelRef vRef = voxDestroyed.GetReference();

            lock(DesignatedRooms)
            {
                foreach(Room r in DesignatedRooms)
                {
                    r.RemoveVoxel(vRef);
                    if(r.Voxels.Count == 0)
                    {
                        toDestroy.Add(r);
                    }
                }
            }

            foreach(Room r in toDestroy)
            {
                DesignatedRooms.Remove(r);
                r.Destroy();
            }
        }

        public void Update(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            PlayState.GUI.IsMouseVisible = true;
        }

        private void BuildNewVoxels(IEnumerable<VoxelRef> refs)
        {
            BuildRoomOrder order = null;
            foreach(Voxel v in refs.Select(r => r.GetVoxel(false)).Where(v => v != null && v.RampType == RampType.None))
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
                    order.VoxelOrders.Add(new BuildVoxelOrder(order, order.ToBuild, v.GetReference()));
                }
                else
                {
                    if(CurrentRoomType != RoomLibrary.GetType("Stockpile"))
                    {
                        Room toBuild = new Room(true, refs, CurrentRoomType, PlayState.ChunkManager);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildRoomOrder(toBuild, this.Faction);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild, v.GetReference()));
                        BuildDesignations.Add(order);
                    }
                    else
                    {
                        Stockpile toBuild = new Stockpile("Stockpile " + Stockpile.NextID(), PlayState.ChunkManager);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildStockpileOrder(toBuild, this.Faction);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild, v.GetReference()));
                        BuildDesignations.Add(order);
                    }
                }
            }

            if(order != null)
            {
                TaskManager.AssignTasks(new List<Task>()
                {
                    new BuildRoomTask(order)
                }, PlayState.Master.SelectedMinions);
            }
        }

        public void VoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            if(CurrentRoomType == null)
            {
                return;
            }

            if(button == InputManager.MouseButton.Left)
            {
                BuildNewVoxels(refs);
            }
            else
            {
                DeleteVoxels(refs);
            }
        }

        private void DeleteVoxels(IEnumerable<VoxelRef> refs )
        {
            foreach(Voxel v in refs.Select(r => r.GetVoxel(false)).Where(v => v != null && v.RampType == RampType.None))
            {
                if(IsBuildDesignation(v))
                {
                    BuildVoxelOrder vox = GetBuildDesignation(v);
                    vox.Order.VoxelOrders.Remove(vox);
                }
                else if(IsInRoom(v))
                {
                    Room existingRoom = GetMostLikelyRoom(v);
                    DesignatedRooms.Remove(existingRoom);

                    List<BuildVoxelOrder> existingDesignations = GetDesignationsAssociatedWithRoom(existingRoom);
                    BuildRoomOrder buildRoomDes = null;
                    foreach(BuildVoxelOrder des in existingDesignations)
                    {
                        des.Order.VoxelOrders.Remove(des);
                        buildRoomDes = des.Order;
                    }

                    BuildDesignations.Remove(buildRoomDes);

                    existingRoom.Destroy();
                }
                break;
            }
        }
    }

}