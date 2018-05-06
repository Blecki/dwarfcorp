// RoomBuilder.cs
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
        private List<Body> displayObjects = null;
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
            if (displayObjects != null)
            {
                foreach (var body in displayObjects)
                {
                    body.Delete();
                }
                displayObjects.Clear();
            }
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
            if (displayObjects != null)
            {
                foreach (var thing in displayObjects)
                {
                    thing.Delete();
                }
            }
        }


        public bool IsInRoom(VoxelHandle v)
        {
            return DesignatedRooms.Any(r => r.ContainsVoxel(v)) || Faction.IsInStockpile(v);
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
                                resourceList.Append(resource.NumResources);
                                resourceList.Append(" ");
                                resourceList.Append(resource.ResourceType);
                            }
                            buildOrder.DisplayWidget = World.Gui.RootItem.AddChild(new Gui.Widget()
                            {
                                Border = "border-dark",
                                TextColor = Color.White.ToVector4(),
                                Text = String.Format("Need {0} to build this {1}", resourceList, buildOrder.ToBuild.RoomData.Name),
                                Rect = new Rectangle(0, 0, 200, 40),
                                Font = "font8",
                                TextVerticalAlign = Gui.VerticalAlign.Center,
                                TextHorizontalAlign = Gui.HorizontalAlign.Center
                            });
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
                            var projection = Faction.World.Camera.Project(center);
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
            BuildRoomOrder order = null;
            foreach(var v in designations.Where(v => v.IsValid && !v.IsEmpty))
            {
                if(IsBuildDesignation(v) || IsInRoom(v))
                    continue;

                // This check should be rendered pointless by the call to Verify made just
                // before calling this function.
                var above = VoxelHelpers.GetVoxelAbove(v);
                if (above.IsValid && !above.IsEmpty)
                    continue;

                if(order == null)
                    order = GetMostLikelyDesignation(v);

                if (order != null && order.ToBuild.RoomData == CurrentRoomData && !order.IsBuilt)
                {
                    order.VoxelOrders.Add(new BuildVoxelOrder(order, order.ToBuild, v));
                }
                else
                {
                    if (CurrentRoomData == RoomLibrary.GetData("Stockpile"))
                    {
                        Stockpile toBuild = new Stockpile(Faction, World);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildStockpileOrder(toBuild, this.Faction);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild, v));
                        BuildDesignations.Add(order);
                    }
                    else if (CurrentRoomData == RoomLibrary.GetData("Treasury"))
                    {
                        Treasury toBuild = new Treasury(Faction, World);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildRoomOrder(toBuild, this.Faction, Faction.World);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild, v));
                        BuildDesignations.Add(order);
                    }
                    else
                    {
                        Room toBuild = RoomLibrary.CreateRoom(Faction, CurrentRoomData.Name, designations.ToList(), true, World);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildRoomOrder(toBuild, Faction, Faction.World);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild, v));
                        BuildDesignations.Add(order);
                    }
                }

                if (order != null && (order.ToBuild.RoomData != CurrentRoomData || order.IsBuilt))
                {
                    order = null;
                }
            }

            if (order != null)
            {
                order.WorkObjects.AddRange(Fence.CreateFences(World.ComponentManager,
                    ContentPaths.Entities.DwarfObjects.constructiontape,
                    order.VoxelOrders.Select(o => o.Voxel),
                    true));
                foreach (var obj in order.WorkObjects)
                {
                    obj.Manager.RootComponent.AddChild(obj);
                }
                World.Master.TaskManager.AddTask(new BuildRoomTask(order));
                /*
                TaskManager.AssignTasks(new List<Task>()
                {
                    new BuildRoomTask(order)
                }, Faction.FilterMinionsWithCapability(World.Master.SelectedMinions, GameMaster.ToolMode.BuildZone));
                */
            }
        }

        private void SetDisplayColor(Body body, Color color)
        {
            foreach (var sprite in body.EnumerateAll().OfType<Tinter>())
            {
                sprite.VertexColorTint = color;
                sprite.Stipple = true;
            }
        }

        public void OnVoxelsDragged(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            if (Faction == null)
            {
                Faction = World.PlayerFaction;
            }

            if (displayObjects != null)
            {
                foreach (var thing in displayObjects)
                {
                    thing.Delete();
                }
            }

            foreach (BuildRoomOrder order in BuildDesignations)
            {
                order.SetTint(Color.White);
            }

            foreach (Room room in Faction.GetRooms())
            {
                room.SetTint(Color.White);
            }

            if (CurrentRoomData == null)
            {
                return;
            }

            if (button == InputManager.MouseButton.Left)
            {
                World.Tutorial("build " + CurrentRoomData.Name);
                if (CurrentRoomData.Verify(refs, Faction, World))
                {
                    List<Quantitiy<Resource.ResourceTags>> requirements =
                        CurrentRoomData.GetRequiredResources(refs.Count, Faction);

                    string tip = "Needs ";


                    if (requirements.Count == 0)
                    {
                        tip = "";
                    }
                    int i = 0;
                    foreach (var requirement in requirements)
                    {
                        i++;
                        tip += requirement.NumResources.ToString();
                        tip += " ";
                        tip += requirement.ResourceType;
                        tip += "\n";
                    }

                    World.ShowToolPopup("Release to build here.");

                    displayObjects = RoomLibrary.GenerateRoomComponentsTemplate(CurrentRoomData, refs,
                        World.ComponentManager, 
                        World.ChunkManager.Content,
                        GameState.Game.GraphicsDevice);

                    foreach(Body thing in displayObjects)
                    {
                        thing.SetFlagRecursive(GameComponent.Flag.ShouldSerialize, false);
                        thing.SetFlagRecursive(GameComponent.Flag.Active, false);
                        SetDisplayColor(thing, Color.Green);
                    }
                }
            }
            else
            {
                foreach (var v in refs.Where(v => v.IsValid && !v.IsEmpty))
                {
                    if (IsBuildDesignation(v))
                    {
                        var order = GetBuildDesignation(v);
                        if (order == null || order.Order == null)
                        {
                            // TODO(mklingen): Don't know how this could happen, but we got a crash here...
                            continue;
                        }
                        if (!order.Order.IsBuilt)
                        {
                            order.Order.SetTint(Color.Red);
                        }
                        else
                        {
                            order.ToBuild.SetTint(Color.Red);
                        }
                        break;
                    }
                    else if (IsInRoom(v))
                    {
                        Room existingRoom = GetMostLikelyRoom(v);
                        if (existingRoom == null)
                        {
                            continue;
                        }
                        existingRoom.SetTint(Color.Red);
                        break;
                    }
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

            if (displayObjects != null)
            {
                foreach (var thing in displayObjects)
                {
                    thing.Delete();
                }
            }

            if(button == InputManager.MouseButton.Left)
            {
                if (CurrentRoomData.Verify(refs, Faction, World))
                {
                    BuildNewVoxels(refs);    
                }
            }
            else
            {
                DeleteVoxels(refs);
            }
        }

        private void DeleteVoxels(IEnumerable<VoxelHandle> Voxels )
        {
            if (Voxels == null)
            {
                return;
            }

            foreach(var v in Voxels.Where(v => v.IsValid && !v.IsEmpty))
            {
                if(IsBuildDesignation(v))
                {
                    BuildVoxelOrder vox = GetBuildDesignation(v);
                    if (vox != null && vox.Order != null)
                    {
                        vox.Order.Destroy();
                        BuildDesignations.Remove(vox.Order);
                    }
                }
                else if(IsInRoom(v))
                {
                    Room existingRoom = GetMostLikelyRoom(v);

                    if (existingRoom == null)
                    {
                        continue;
                    }

                    World.Gui.ShowModalPopup(new Gui.Widgets.Confirm
                        {
                            Text = "Do you want to destroy this " + existingRoom.RoomData.Name + "?",
                            OnClose = (sender) => destroyDialog_OnClosed((sender as Gui.Widgets.Confirm).DialogResult, existingRoom)
                        });

                    break;
                }
            }
        }

        void destroyDialog_OnClosed(Gui.Widgets.Confirm.Result status, Room room)
        {
            if (status == Gui.Widgets.Confirm.Result.OKAY)
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
