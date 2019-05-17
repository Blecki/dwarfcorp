using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System.Text;

namespace DwarfCorp
{
    // Todo: Move storage into faction and logic into BuildZoneTool.
    /// <summary>
    /// The BuildRoom designator keeps track of voxels selected by the player and turns
    /// them into BuildRoom designations (so that dwarves can build BuildRoom).
    /// </summary>
    public class RoomBuilder
    {
        public List<Room> DesignatedRooms { get; set; }
        public List<BuildRoomOrder> BuildDesignations { get; set; }
        public RoomData CurrentRoomData { get; set; }
        public Faction Faction { get; set; }

        [JsonIgnore]
        private WorldManager World { get; set; }

        [OnDeserialized]
        public void OnDeserializing(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        public List<Room> FilterRoomsByType(string type)
        {
            return DesignatedRooms.Where(r => r.RoomData.Name == type).ToList();
        }

        public void End()
        {
            CurrentRoomData = null;
        }

        public RoomBuilder()
        {
            
        }

        public RoomBuilder(Faction faction, WorldManager world)
        {
            World = world;
            DesignatedRooms = new List<Room>();
            BuildDesignations = new List<BuildRoomOrder>();
            CurrentRoomData = RoomLibrary.GetData("BedRoom");
            Faction = faction;
        }


        public void OnEnter()
        {
            if (Faction == null)
            {
                Faction = World.PlayerFaction;
            }
        }

        public void OnExit()
        {
        }


        public bool IsInRoom(VoxelHandle v)
        {
            return DesignatedRooms.Any(r => r.ContainsVoxel(v)) || Faction.IsInStockpile(v);
        }

        public Room GetRoomThatContainsVoxel(VoxelHandle V)
        {
            return DesignatedRooms.FirstOrDefault(r => r.ContainsVoxel(V));
        }

        public bool IsBuildDesignation(VoxelHandle v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).Any(buildDesignation => buildDesignation.Voxel == v);
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

        public BuildVoxelOrder GetBuildDesignation(VoxelHandle v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).FirstOrDefault(buildDesignation => buildDesignation.Voxel == v);
        }

        public BuildRoomOrder GetMostLikelyDesignation(VoxelHandle v)
        {
            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            return (from room in BuildDesignations
                from buildDesignation in room.VoxelOrders
                where larger.Intersects(buildDesignation.Voxel.GetBoundingBox())
                select room).FirstOrDefault();
        }

        public Room GetMostLikelyRoom(VoxelHandle v)
        {
            foreach(Room r in DesignatedRooms.Where(r => r.ContainsVoxel(v)))
                return r;

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

        public void Render(DwarfTime game, GraphicsDevice graphics)
        {
            
        }

        public void CheckRemovals()
        {
            List<BuildRoomOrder> toRemove = BuildDesignations.Where(build => build.IsBuilt).ToList();

            foreach(BuildRoomOrder build in toRemove)
            {
                if (build.DisplayWidget != null)
                {
                    World.UserInterface.Gui.DestroyWidget(build.DisplayWidget);
                }
                BuildDesignations.Remove(build);
            }
        }

        public void OnVoxelDestroyed(VoxelHandle voxDestroyed)
        {
            List<Room> toDestroy = new List<Room>();

            lock (DesignatedRooms)
            {
                List<Room> toCheck = new List<Room>();
                toCheck.AddRange(DesignatedRooms.Where(r => r.IsBuilt));
                foreach (Room r in toCheck)
                {
                    if (r.RemoveVoxel(voxDestroyed))
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

        public void Update()
        {
            if (Faction == null)
            {
                Faction = World.PlayerFaction;
            }

            foreach (var buildOrder in BuildDesignations)
            {
                if (buildOrder.IsBuilt)
                {
                    if (buildOrder.DisplayWidget != null)
                    {
                        buildOrder.DisplayWidget.Root.DestroyWidget(buildOrder.DisplayWidget);
                        buildOrder.DisplayWidget = null;
                    }
                }
                else
                {
                    var requiredResources = buildOrder.ListRequiredResources();
                    if (buildOrder.DisplayWidget == null)
                    {
                        if (!Faction.HasResources(requiredResources))
                        {
                            StringBuilder resourceList = new StringBuilder();
                            foreach (var resource in requiredResources)
                            {
                                resourceList.Append(resource.Count);
                                resourceList.Append(" ");
                                resourceList.Append(resource.Type);
                            }
                            var order = buildOrder;
                            buildOrder.DisplayWidget = World.UserInterface.Gui.RootItem.AddChild(new Gui.Widget()
                            {
                                Border = "border-dark",
                                TextColor = Color.White.ToVector4(),
                                Text = String.Format("Need {0} to build this {1}", resourceList, buildOrder.ToBuild.RoomData.Name),
                                Rect = new Rectangle(0, 0, 200, 40),
                                Font = "font8",
                                TextVerticalAlign = Gui.VerticalAlign.Center,
                                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                                OnClick = (sender, args) =>
                                {
                                    sender.Hidden = true;
                                }
                            });

                            World.UserInterface.Gui.RootItem.SendToBack(buildOrder.DisplayWidget);
                        }
                    }
                    else
                    {
                        if (Faction.HasResources(requiredResources))
                        {
                            buildOrder.DisplayWidget.Root.DestroyWidget(buildOrder.DisplayWidget);
                            buildOrder.DisplayWidget = null;
                        }
                        else
                        {
                            var center = buildOrder.GetBoundingBox().Center();
                            var projection = Faction.World.Renderer.Camera.Project(center);
                            if (projection.Z < 0.9999)
                            {
                                buildOrder.DisplayWidget.Rect = new Rectangle((int)(projection.X - buildOrder.DisplayWidget.Rect.Width / 2),
                                    (int)(projection.Y - buildOrder.DisplayWidget.Rect.Height / 2), 
                                    buildOrder.DisplayWidget.Rect.Width, buildOrder.DisplayWidget.Rect.Height);
                                buildOrder.DisplayWidget.Invalidate();
                            }
                        }
                    }
                }
            }
        }

        private void BuildNewVoxels(IEnumerable<VoxelHandle> designations)
        {
            Room toBuild = RoomLibrary.CreateRoom(Faction, CurrentRoomData.Name, World);
            var order = new BuildRoomOrder(toBuild, Faction, Faction.World);
            BuildDesignations.Add(order);
            DesignatedRooms.Add(toBuild);

            foreach (var v in designations.Where(v => v.IsValid && !v.IsEmpty))
                order.VoxelOrders.Add(new BuildVoxelOrder(order, order.ToBuild, v));

            order.WorkObjects.AddRange(Fence.CreateFences(World.ComponentManager,
                ContentPaths.Entities.DwarfObjects.constructiontape,
                order.VoxelOrders.Select(o => o.Voxel),
                true));
            foreach (var obj in order.WorkObjects)
                obj.Manager.RootComponent.AddChild(obj);

            World.TaskManager.AddTask(new BuildRoomTask(order));
        }

        public void OnVoxelsDragged(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            World.UserInterface.VoxSelector.SelectionColor = Color.White;

            if (Faction == null)
                Faction = World.PlayerFaction;
            
            foreach (BuildRoomOrder order in BuildDesignations)
                order.SetTint(Color.White);
            
            foreach (Room room in Faction.GetRooms())
                room.SetTint(Color.White);
            
            if (CurrentRoomData == null)
                return;
            
            if (button == InputManager.MouseButton.Left)
            {
                World.Tutorial("build " + CurrentRoomData.Name);

                if (CurrentRoomData.Verify(refs, Faction, World))
                {
                    List<Quantitiy<Resource.ResourceTags>> requirements = CurrentRoomData.GetRequiredResources(refs.Count, Faction);

                    string tip = "Needs ";


                    if (requirements.Count == 0)
                    {
                        tip = "";
                    }
                    int i = 0;
                    foreach (var requirement in requirements)
                    {
                        i++;
                        tip += requirement.Count.ToString();
                        tip += " ";
                        tip += requirement.Type;
                        tip += "\n";
                    }

                    World.UserInterface.ShowTooltip("Release to build here.");
                }
                else
                {
                    World.UserInterface.VoxSelector.SelectionColor = GameSettings.Default.Colors.GetColor("Negative", Color.Red);
                }
            }
        }

        public void VoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            foreach (BuildRoomOrder order in BuildDesignations)
            {
                order.SetTint(Color.White);
            }

            foreach (Room room in Faction.GetRooms())
            {
                room.SetTint(Color.White);
            }

            if(CurrentRoomData == null)
            {
                return;
            }

            if(button == InputManager.MouseButton.Left)
            {
                if (CurrentRoomData.Verify(refs, Faction, World))
                {
                    BuildNewVoxels(refs);    
                }
            }
        }
    }
}
