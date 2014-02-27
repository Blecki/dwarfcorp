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
    /// The room designator keeps track of voxels selected by the player and turns
    /// them into room designations (so that dwarves can build room).
    /// </summary>
    [JsonObject(IsReference = true)]
    public class RoomDesignator
    {
        public List<Room> DesignatedRooms { get; set; }
        public List<RoomBuildDesignation> BuildDesignations { get; set; }
        public RoomType CurrentRoomType { get; set; }
        public Faction Faction { get; set; }


        public List<Room> FilterRoomsByType(string type)
        {
            return DesignatedRooms.Where(r => r.RoomType.Name == type).ToList();
        }

        public RoomDesignator()
        {
            
        }

        public RoomDesignator(Faction faction)
        {
            DesignatedRooms = new List<Room>();
            BuildDesignations = new List<RoomBuildDesignation>();
            CurrentRoomType = RoomLibrary.GetType("BedRoom");
            Faction = faction;
        }


        public bool IsInRoom(Voxel v)
        {
            VoxelRef vRef = v.GetReference();
            return DesignatedRooms.Any(r => r.ContainsVoxel(vRef));
        }

        public bool IsBuildDesignation(Voxel v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelBuildDesignations).Any(buildDesignation => (buildDesignation.Voxel.WorldPosition - v.Position).LengthSquared() < 0.1f);
        }

        public bool IsBuildDesignation(Room r)
        {
            return BuildDesignations.Any(room => room.ToBuild == r);
        }

        public VoxelBuildDesignation GetBuildDesignation(Room v)
        {
            return (from room in BuildDesignations
                where room.ToBuild == v
                where room.VoxelBuildDesignations.Count > 0
                select room.VoxelBuildDesignations[0]).FirstOrDefault();
        }

        public VoxelBuildDesignation GetBuildDesignation(Voxel v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelBuildDesignations).FirstOrDefault(buildDesignation => (buildDesignation.Voxel.WorldPosition - v.Position).LengthSquared() < 0.1f);
        }

        public RoomBuildDesignation GetMostLikelyDesignation(Voxel v)
        {
            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            return (from room in BuildDesignations
                from buildDesignation in room.VoxelBuildDesignations
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
                from buildDesignation in room.VoxelBuildDesignations
                where larger.Intersects(buildDesignation.Voxel.GetBoundingBox())
                select buildDesignation.ToBuild).FirstOrDefault();
        }

        public List<VoxelBuildDesignation> GetDesignationsAssociatedWithRoom(Room room)
        {
            return (from roomDesignation in BuildDesignations
                from des in roomDesignation.VoxelBuildDesignations
                where des.ToBuild == room
                select des).ToList();
        }

        public void Render(GameTime game, GraphicsDevice graphics)
        {
            foreach(Room room in DesignatedRooms)
            {
                Drawer3D.DrawBox(room.GetBoundingBox(), Color.White, 0.1f, true);
            }


            foreach(RoomBuildDesignation roomDesignation in BuildDesignations)
            {
                BoundingBox roomBox = roomDesignation.GetBoundingBox();
                roomBox.Max = new Vector3(roomBox.Max.X, roomBox.Max.Y + 0.1f, roomBox.Max.Z);

                Drawer3D.DrawBox(roomBox, Color.White, 0.1f, true);
                List<VoxelBuildDesignation> removals = new List<VoxelBuildDesignation>();
                foreach(VoxelBuildDesignation des in roomDesignation.VoxelBuildDesignations)
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

                foreach(VoxelBuildDesignation des in removals)
                {
                    roomDesignation.VoxelBuildDesignations.Remove(des);
                }

                Vector3 textLocation = (roomBox.Max - roomBox.Min) / 2.0f + roomBox.Min + new Vector3(0, 2.0f, 0);
                Drawer2D.DrawTextBox(roomDesignation.GetTextDisplay(), textLocation);
            }
        }

        public void CheckRemovals()
        {
            List<RoomBuildDesignation> toRemove = BuildDesignations.Where(build => build.IsBuilt).ToList();

            foreach(RoomBuildDesignation build in toRemove)
            {
                BuildDesignations.Remove(build);
            }
        }

        public void OnVoxelDestroyed(Voxel voxDestroyed)
        {
            List<Room> toDestroy = new List<Room>();
            VoxelRef vRef = voxDestroyed.GetReference();
            foreach(Room r in DesignatedRooms)
            {
                r.RemoveVoxel(vRef);
                if(r.Storage.Count == 0)
                {
                    toDestroy.Add(r);
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
            game.IsMouseVisible = true;
        }

        public void VoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            if(CurrentRoomType == null)
            {
                return;
            }

            foreach (Voxel v in refs.Select(r => r.GetVoxel(false)).Where(v => v != null && v.RampType == RampType.None).Where(v => v != null))
            {
                switch(button)
                {
                    case InputManager.MouseButton.Left:
                        if(!IsBuildDesignation(v) && !IsInRoom(v))
                        {
                            RoomBuildDesignation existingRoom = GetMostLikelyDesignation(v);

                            if(existingRoom != null)
                            {
                                existingRoom.VoxelBuildDesignations.Add(new VoxelBuildDesignation(existingRoom, existingRoom.ToBuild, v.GetReference()));
                            }
                            else
                            {
                                Room toBuild = new Room(true, refs, CurrentRoomType, PlayState.ChunkManager);
                                DesignatedRooms.Add(toBuild);
                                RoomBuildDesignation buildDesignation = new RoomBuildDesignation(toBuild, this.Faction);
                                buildDesignation.VoxelBuildDesignations.Add(new VoxelBuildDesignation(buildDesignation, toBuild, v.GetReference()));
                                BuildDesignations.Add(buildDesignation);
                            }
                        }
                        break;
                    case InputManager.MouseButton.Right:
                        if(IsBuildDesignation(v))
                        {
                            VoxelBuildDesignation vox = GetBuildDesignation(v);
                            vox.BuildDesignation.VoxelBuildDesignations.Remove(vox);
                        }
                        else if(IsInRoom(v))
                        {
                            Room existingRoom = GetMostLikelyRoom(v);
                            DesignatedRooms.Remove(existingRoom);

                            List<VoxelBuildDesignation> existingDesignations = GetDesignationsAssociatedWithRoom(existingRoom);
                            RoomBuildDesignation roomDes = null;
                            foreach(VoxelBuildDesignation des in existingDesignations)
                            {
                                des.BuildDesignation.VoxelBuildDesignations.Remove(des);
                                roomDes = des.BuildDesignation;
                            }

                            BuildDesignations.Remove(roomDes);

                            existingRoom.Destroy();
                        }
                        break;
                }
            }
        }
    }

}