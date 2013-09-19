using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace DwarfCorp
{
    public class RoomBuildDesignation
    {
        public Room ToBuild { get; set; }
        public Dictionary<string, ResourceAmount> PutResources { get; set; }
        public List<VoxelBuildDesignation> VoxelBuildDesignations { get; set; }
        public GameMaster Master { get; set; }

        public bool IsBuilt { get; set; }

        public RoomBuildDesignation(Room toBuild, GameMaster master)
        {
            ToBuild = toBuild;
            PutResources = new Dictionary<string, ResourceAmount>();
            VoxelBuildDesignations = new List<VoxelBuildDesignation>();
            IsBuilt = false;
            Master = master;
        }


        public void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            foreach (VoxelBuildDesignation vox in VoxelBuildDesignations)
            {
                ToBuild.AddVoxel(vox.Voxel);
            }
            IsBuilt = true;
            ToBuild.IsBuilt = true;
            RoomLibrary.GenerateRoomComponentsTemplate(ToBuild, Master.Components, Master.Content, Master.Graphics);
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> components = new List<BoundingBox>();

            foreach (VoxelBuildDesignation vox in VoxelBuildDesignations)
            {
                components.Add(vox.Voxel.GetBoundingBox());
            }

            return LinearMathHelpers.GetBoundingBox(components);
        }

        public bool IsResourceSatisfied(string name)
        {
            int required = GetNumRequiredResources(name);
            int current = 0;

            if (PutResources.ContainsKey(name))
            {
                current = (int)PutResources[name].NumResources;
            }

            return current >= required;
            
        }

        public int GetNumRequiredResources(string name)
        {
            if (ToBuild.RoomType.RequiredResources.ContainsKey(name))
            {
                return Math.Max((int)(ToBuild.RoomType.RequiredResources[name].NumResources * VoxelBuildDesignations.Count), 1);
            }
            else
            {
                return 0;
            }
        }

        public string GetTextDisplay()
        {
            string toReturn = ToBuild.RoomType.Name;

            foreach (ResourceAmount amount in ToBuild.RoomType.RequiredResources.Values)
            {
                toReturn += "\n";
                int numResource = 0;
                if (PutResources.ContainsKey(amount.ResourceType.ResourceName))
                {
                    numResource = (int)(PutResources[amount.ResourceType.ResourceName].NumResources);
                }
                toReturn += amount.ResourceType.ResourceName + " : " + numResource + "/" + Math.Max((int)(amount.NumResources * VoxelBuildDesignations.Count), 1);
            }

            return toReturn;
        }
    }

    public class VoxelBuildDesignation
    {
        public Room ToBuild { get; set; }
        public VoxelRef Voxel { get; set; }
        public RoomBuildDesignation BuildDesignation { get; set; }

        public VoxelBuildDesignation(RoomBuildDesignation buildDesignation, Room toBuild, VoxelRef voxel)
        {
            BuildDesignation = buildDesignation;
            ToBuild = toBuild;
            Voxel = voxel;
        }

        public void AddResource(string resource)
        {
            if (BuildDesignation.PutResources.ContainsKey(resource))
            {
                ResourceAmount amount = BuildDesignation.PutResources[resource];
                amount.NumResources++;
            }
            else
            {
                ResourceAmount amount = new ResourceAmount();
                amount.NumResources++;
                amount.ResourceType = ResourceLibrary.Resources[resource];

                BuildDesignation.PutResources[resource] = amount;
            }

        }

        public void Build()
        {
            BuildDesignation.Build();
        }

        public Resource GetNextRequiredResource()
        {
            IEnumerable<string> randomKeys = Datastructures.RandomKeys<string, ResourceAmount>(ToBuild.RoomType.RequiredResources);
            foreach (string s in ToBuild.RoomType.RequiredResources.Keys)
            {
                if (!BuildDesignation.PutResources.ContainsKey(s))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
                else if (BuildDesignation.PutResources[s].NumResources < Math.Max((int)(ToBuild.RoomType.RequiredResources[s].NumResources * BuildDesignation.VoxelBuildDesignations.Count), 1))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
            }

            return null;

        }

        public bool MeetsBuildRequirements()
        {
            bool toReturn = true;
            foreach (string s in ToBuild.RoomType.RequiredResources.Keys)
            {
                if (!BuildDesignation.PutResources.ContainsKey(s))
                {
                    return false;
                }
                else
                {
                    toReturn = toReturn && (BuildDesignation.PutResources[s].NumResources >= Math.Max((int)(ToBuild.RoomType.RequiredResources[s].NumResources * BuildDesignation.VoxelBuildDesignations.Count), 1));
                }
            }

            return toReturn;
        }

    }


    public class RoomDesignator
    {
        public List<Room> DesignatedRooms { get; set; }
        public List<RoomBuildDesignation> BuildDesignations { get; set; }
        public RoomType CurrentRoomType { get; set; }
        public GameMaster Master { get; set; }


        public List<Room> FilterRoomsByType(string type)
        {
            List<Room> toReturn = new List<Room>();
            foreach (Room r in DesignatedRooms)
            {
                if (r.RoomType.Name == type)
                {
                    toReturn.Add(r);
                }
            }

            return toReturn;
        }

        public RoomDesignator(GameMaster master)
        {
            DesignatedRooms = new List<Room>();
            BuildDesignations = new List<RoomBuildDesignation>();
            CurrentRoomType = RoomLibrary.GetType("BedRoom");
            Master = master;
            Master.VoxSelector.Selected += VoxelsSelected;
        }


        public bool IsInRoom(Voxel v)
        {
            foreach (Room r in DesignatedRooms)
            {
                if (r.Voxels.Contains(v.GetReference()))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsBuildDesignation(Voxel v)
        {
            foreach (RoomBuildDesignation room in BuildDesignations)
            {
                foreach (VoxelBuildDesignation buildDesignation in room.VoxelBuildDesignations)
                {
                    if ((buildDesignation.Voxel.WorldPosition - v.Position).LengthSquared() < 0.1f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsBuildDesignation(Room r)
        {
            foreach (RoomBuildDesignation room in BuildDesignations)
            {
                if (room.ToBuild == r)
                {
                    return true;
                }
            }

            return false;
        }

        public VoxelBuildDesignation GetBuildDesignation(Room v)
        {
            foreach (RoomBuildDesignation room in BuildDesignations)
            {
                if (room.ToBuild == v)
                {
                    if (room.VoxelBuildDesignations.Count > 0)
                    {
                        return room.VoxelBuildDesignations[0];
                    }
                }
            }

            return null;
        }

        public VoxelBuildDesignation GetBuildDesignation(Voxel v)
        {
            foreach (RoomBuildDesignation room in BuildDesignations)
            {
                foreach (VoxelBuildDesignation buildDesignation in room.VoxelBuildDesignations)
                {
                    if ((buildDesignation.Voxel.WorldPosition - v.Position).LengthSquared() < 0.1f)
                    {
                        return buildDesignation;
                    }
                }
            }

            return null;
        }

        public RoomBuildDesignation GetMostLikelyDesignation(Voxel v)
        {

            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            foreach (RoomBuildDesignation room in BuildDesignations)
            {
                foreach (VoxelBuildDesignation buildDesignation in room.VoxelBuildDesignations)
                {
                    if (larger.Intersects(buildDesignation.Voxel.GetBoundingBox()))
                    {
                        return room;
                    }
                }
            }

            return null;
        }

        public Room GetMostLikelyRoom(Voxel v)
        {
            foreach (Room r in DesignatedRooms)
            {
                if (r.IsInRoom(v.GetReference()))
                {
                    return r;
                }
            }

            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            foreach (RoomBuildDesignation room in BuildDesignations)
            {
                foreach (VoxelBuildDesignation buildDesignation in room.VoxelBuildDesignations)
                {
                    if (larger.Intersects(buildDesignation.Voxel.GetBoundingBox()))
                    {
                        return buildDesignation.ToBuild;
                    }
                }
            }

            return null;
        }

        public List<VoxelBuildDesignation> GetDesignationsAssociatedWithRoom(Room room)
        {
            List<VoxelBuildDesignation> toReturn = new List<VoxelBuildDesignation>();

            foreach (RoomBuildDesignation roomDesignation in BuildDesignations)
            {
                foreach (VoxelBuildDesignation des in roomDesignation.VoxelBuildDesignations)
                {
                    if (des.ToBuild == room)
                    {
                        toReturn.Add(des);
                    }
                }
            }

            return toReturn;
        }

        public void Render(GameTime game, GraphicsDevice graphics)
        {

            foreach (Room room in DesignatedRooms)
            {
                SimpleDrawing.DrawBox(room.GetBoundingBox(), Color.White, 0.1f, true);
            }


            foreach (RoomBuildDesignation roomDesignation in BuildDesignations)
            {
                BoundingBox roomBox = roomDesignation.GetBoundingBox();
                roomBox.Max = new Vector3(roomBox.Max.X, roomBox.Max.Y + 0.1f, roomBox.Max.Z);

                SimpleDrawing.DrawBox(roomBox, Color.White, 0.1f, true);
                List<VoxelBuildDesignation> removals = new List<VoxelBuildDesignation>();
                foreach (VoxelBuildDesignation des in roomDesignation.VoxelBuildDesignations)
                {
                    SimpleDrawing.DrawBox(des.Voxel.GetBoundingBox(), Color.LightBlue, 0.05f, true);
                    BoundingBox centerBox = des.Voxel.GetBoundingBox();
                    centerBox.Min += new Vector3(0.7f, 1.1f, 0.7f);
                    centerBox.Max += new Vector3(-0.7f, 0.2f, -0.7f);
                    SimpleDrawing.DrawBox(centerBox, Color.LightBlue, 0.01f, true);

                    if (des.Voxel.GetVoxel(this.Master.Chunks, false) == null)
                    {
                        removals.Add(des);
                    }
                }

                foreach (VoxelBuildDesignation des in removals)
                {
                    roomDesignation.VoxelBuildDesignations.Remove(des);
                }

                Vector3 textLocation = (roomBox.Max - roomBox.Min) / 2.0f + roomBox.Min + new Vector3(0, 2.0f, 0);
                Drawer2D.DrawTextBox(roomDesignation.GetTextDisplay(), textLocation);
            }
        }

        public void CheckRemovals()
        {
            List<RoomBuildDesignation> toRemove = new List<RoomBuildDesignation>();
            foreach (RoomBuildDesignation build in BuildDesignations)
            {
                if (build.IsBuilt)
                {
                    toRemove.Add(build);
                }
            }

            foreach (RoomBuildDesignation build in toRemove)
            {
                BuildDesignations.Remove(build);
            }


        }

        public void OnVoxelDestroyed(Voxel voxDestroyed)
        {
            List<Room> toDestroy = new List<Room>();
            foreach (Room r in DesignatedRooms)
            {
                List<VoxelRef> removals = new List<VoxelRef>();
                foreach (VoxelRef v in r.Voxels)
                {
                    if (v.GetVoxel(PlayState.chunkManager, false) == voxDestroyed)
                    {
                        removals.Add(v);
                    }
                }

                foreach (VoxelRef v in removals)
                {
                    r.Voxels.Remove(v);
                }

                if (r.Voxels.Count == 0)
                {
                    toDestroy.Add(r);
                }
            }

            foreach (Room r in toDestroy)
            {
                DesignatedRooms.Remove(r);
            }
        }

        public void Update(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            game.IsMouseVisible = true;
        }

        public void VoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            if (Master.CurrentTool != GameMaster.ToolMode.Build || CurrentRoomType == null || Master.GodMode.IsActive)
            {
                return;
            }

            foreach (VoxelRef r in refs)
            {
                Voxel v = r.GetVoxel(Master.Chunks, false);
                
                if (v == null || v.RampType != RampType.None)
                {
                    continue;
                }

                if (v != null)
                {
                    if (button == InputManager.MouseButton.Left)
                    {
                        if (!IsBuildDesignation(v) && !IsInRoom(v))
                        {
                            RoomBuildDesignation existingRoom = GetMostLikelyDesignation(v);

                            if (existingRoom != null)
                            {
                                existingRoom.VoxelBuildDesignations.Add(new VoxelBuildDesignation(existingRoom, existingRoom.ToBuild, v.GetReference()));
                            }
                            else
                            {
                                Room toBuild = new Room(true, refs, CurrentRoomType, Master.Chunks);
                                DesignatedRooms.Add(toBuild);
                                RoomBuildDesignation buildDesignation = new RoomBuildDesignation(toBuild, this.Master);
                                buildDesignation.VoxelBuildDesignations.Add(new VoxelBuildDesignation(buildDesignation, toBuild, v.GetReference()));
                                BuildDesignations.Add(buildDesignation);
                            }
                        }
                    }
                    else if (button == InputManager.MouseButton.Right)
                    {
                        if (IsBuildDesignation(v))
                        {
                            VoxelBuildDesignation vox = GetBuildDesignation(v);
                            vox.BuildDesignation.VoxelBuildDesignations.Remove(vox);
                        }
                        else if (IsInRoom(v))
                        {
                            Room existingRoom = GetMostLikelyRoom(v);
                            DesignatedRooms.Remove(existingRoom);

                            List<VoxelBuildDesignation> existingDesignations = GetDesignationsAssociatedWithRoom(existingRoom);
                            RoomBuildDesignation roomDes = null;
                            foreach (VoxelBuildDesignation des in existingDesignations)
                            {
                                des.BuildDesignation.VoxelBuildDesignations.Remove(des);
                                roomDes = des.BuildDesignation;
                            }

                            BuildDesignations.Remove(roomDes);

                            existingRoom.ClearRoom();
                        }
                    }
                }
            }
        }
    }
}
