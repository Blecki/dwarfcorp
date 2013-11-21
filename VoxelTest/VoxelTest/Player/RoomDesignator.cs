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
using Newtonsoft.Json;


namespace DwarfCorp
{
     [JsonObject(IsReference = true)]
    public class RoomBuildDesignation
    {
        [JsonIgnore]
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
            if(IsBuilt)
            {
                return;
            }

            foreach(VoxelBuildDesignation vox in VoxelBuildDesignations)
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

            foreach(VoxelBuildDesignation vox in VoxelBuildDesignations)
            {
                components.Add(vox.Voxel.GetBoundingBox());
            }

            return LinearMathHelpers.GetBoundingBox(components);
        }

        public bool IsResourceSatisfied(string name)
        {
            int required = GetNumRequiredResources(name);
            int current = 0;

            if(PutResources.ContainsKey(name))
            {
                current = (int) PutResources[name].NumResources;
            }

            return current >= required;
        }

        public int GetNumRequiredResources(string name)
        {
            if(ToBuild.RoomType.RequiredResources.ContainsKey(name))
            {
                return Math.Max((int) (ToBuild.RoomType.RequiredResources[name].NumResources * VoxelBuildDesignations.Count), 1);
            }
            else
            {
                return 0;
            }
        }

        public string GetTextDisplay()
        {
            string toReturn = ToBuild.RoomType.Name;

            foreach(ResourceAmount amount in ToBuild.RoomType.RequiredResources.Values)
            {
                toReturn += "\n";
                int numResource = 0;
                if(PutResources.ContainsKey(amount.ResourceType.ResourceName))
                {
                    numResource = (int) (PutResources[amount.ResourceType.ResourceName].NumResources);
                }
                toReturn += amount.ResourceType.ResourceName + " : " + numResource + "/" + Math.Max((int) (amount.NumResources * VoxelBuildDesignations.Count), 1);
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
            if(BuildDesignation.PutResources.ContainsKey(resource))
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
            foreach(string s in ToBuild.RoomType.RequiredResources.Keys)
            {
                if(!BuildDesignation.PutResources.ContainsKey(s))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
                else if(BuildDesignation.PutResources[s].NumResources < Math.Max((int) (ToBuild.RoomType.RequiredResources[s].NumResources * BuildDesignation.VoxelBuildDesignations.Count), 1))
                {
                    return ToBuild.RoomType.RequiredResources[s].ResourceType;
                }
            }

            return null;
        }

        public bool MeetsBuildRequirements()
        {
            bool toReturn = true;
            foreach(string s in ToBuild.RoomType.RequiredResources.Keys)
            {
                if(!BuildDesignation.PutResources.ContainsKey(s))
                {
                    return false;
                }
                else
                {
                    toReturn = toReturn && (BuildDesignation.PutResources[s].NumResources >= Math.Max((int) (ToBuild.RoomType.RequiredResources[s].NumResources * BuildDesignation.VoxelBuildDesignations.Count), 1));
                }
            }

            return toReturn;
        }
    }

    [JsonObject(IsReference = true)]
    public class RoomDesignator
    {
        public List<Room> DesignatedRooms { get; set; }
        public List<RoomBuildDesignation> BuildDesignations { get; set; }
        public RoomType CurrentRoomType { get; set; }
        public GameMaster Master { get; set; }


        public List<Room> FilterRoomsByType(string type)
        {
            return DesignatedRooms.Where(r => r.RoomType.Name == type).ToList();
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
                SimpleDrawing.DrawBox(room.GetBoundingBox(), Color.White, 0.1f, true);
            }


            foreach(RoomBuildDesignation roomDesignation in BuildDesignations)
            {
                BoundingBox roomBox = roomDesignation.GetBoundingBox();
                roomBox.Max = new Vector3(roomBox.Max.X, roomBox.Max.Y + 0.1f, roomBox.Max.Z);

                SimpleDrawing.DrawBox(roomBox, Color.White, 0.1f, true);
                List<VoxelBuildDesignation> removals = new List<VoxelBuildDesignation>();
                foreach(VoxelBuildDesignation des in roomDesignation.VoxelBuildDesignations)
                {
                    SimpleDrawing.DrawBox(des.Voxel.GetBoundingBox(), Color.LightBlue, 0.05f, true);
                    BoundingBox centerBox = des.Voxel.GetBoundingBox();
                    centerBox.Min += new Vector3(0.7f, 1.1f, 0.7f);
                    centerBox.Max += new Vector3(-0.7f, 0.2f, -0.7f);
                    SimpleDrawing.DrawBox(centerBox, Color.LightBlue, 0.01f, true);

                    if(des.Voxel.GetVoxel(this.Master.Chunks, false) == null)
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
            if(Master.CurrentTool != GameMaster.ToolMode.Build || CurrentRoomType == null || Master.GodMode.IsActive)
            {
                return;
            }

            foreach(Voxel v in refs.Select(r => r.GetVoxel(Master.Chunks, false)).Where(v => v != null && v.RampType == RampType.None).Where(v => v != null))
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
                                Room toBuild = new Room(true, refs, CurrentRoomType, Master.Chunks);
                                DesignatedRooms.Add(toBuild);
                                RoomBuildDesignation buildDesignation = new RoomBuildDesignation(toBuild, this.Master);
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