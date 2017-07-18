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


        public bool IsInRoom(TemporaryVoxelHandle v)
        {
            return DesignatedRooms.Any(r => r.ContainsVoxel(v)) || Faction.IsInStockpile(v);
        }

        public bool IsBuildDesignation(TemporaryVoxelHandle v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).Any(buildDesignation => buildDesignation.Voxel.tvh == v);
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
            return BuildDesignations.SelectMany(room => room.VoxelOrders).FirstOrDefault(buildDesignation => (buildDesignation.Voxel.WorldPosition - v.WorldPosition).LengthSquared() < 0.1f);
        }

        public BuildRoomOrder GetMostLikelyDesignation(TemporaryVoxelHandle v)
        {
            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            return (from room in BuildDesignations
                from buildDesignation in room.VoxelOrders
                where larger.Intersects(buildDesignation.Voxel.GetBoundingBox())
                select room).FirstOrDefault();
        }

        public Room GetMostLikelyRoom(VoxelHandle v)
        {
            VoxelHandle vRef = v;
            foreach(Room r in DesignatedRooms.Where(r => r.ContainsVoxel(vRef.tvh)))
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

        public void Render(DwarfTime game, GraphicsDevice graphics)
        {
            /*
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
                    Drawer3D.DrawBox(des.DestinationVoxel.GetBoundingBox(), Color.LightBlue, 0.05f, true);
                    BoundingBox centerBox = des.DestinationVoxel.GetBoundingBox();
                    centerBox.Min += new Vector3(0.7f, 1.1f, 0.7f);
                    centerBox.Max += new Vector3(-0.7f, 0.2f, -0.7f);
                    Drawer3D.DrawBox(centerBox, Color.LightBlue, 0.01f, true);

                    if (des.DestinationVoxel.IsEmpty)
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
             */
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
            VoxelHandle vRef = voxDestroyed;

            lock (DesignatedRooms)
            {
                List<Room> toCheck = new List<Room>();
                toCheck.AddRange(DesignatedRooms);
                foreach (Room r in toCheck)
                {
                    r.RemoveVoxel(vRef.tvh);
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

        public void Update(MouseState mouseState, KeyboardState keyState, DwarfGame game, DwarfTime time)
        {
            if (Faction == null)
            {
                Faction = World.PlayerFaction;
            }

            World.SetMouse(World.MousePointer);
        }

        private void BuildNewVoxels(IEnumerable<TemporaryVoxelHandle> designations)
        {
            BuildRoomOrder order = null;
            foreach(var v in designations.Where(v => v.IsValid && !v.IsEmpty))
            {
                if(IsBuildDesignation(v) || IsInRoom(v))
                    continue;

                var above = VoxelHelpers.GetVoxelAbove(v);
                if (above.IsValid && !v.IsEmpty)
                    continue;

                if(order == null)
                    order = GetMostLikelyDesignation(v);

                if (order != null)
                {
                    order.VoxelOrders.Add(new BuildVoxelOrder(order, order.ToBuild, 
                        new VoxelHandle(v.Coordinate.GetLocalVoxelCoordinate(), v.Chunk)));
                }
                else
                {
                    if(CurrentRoomData != RoomLibrary.GetData("Stockpile"))
                    {
                        Room toBuild = RoomLibrary.CreateRoom(Faction, CurrentRoomData.Name, designations.ToList(), true, World);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildRoomOrder(toBuild, Faction, Faction.World);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild,
                            new VoxelHandle(v.Coordinate.GetLocalVoxelCoordinate(), v.Chunk)));
                        BuildDesignations.Add(order);
                    }
                    else
                    {
                        Stockpile toBuild = new Stockpile(Faction, World);
                        DesignatedRooms.Add(toBuild);
                        order = new BuildStockpileOrder(toBuild, this.Faction);
                        order.VoxelOrders.Add(new BuildVoxelOrder(order, toBuild,
                            new VoxelHandle(v.Coordinate.GetLocalVoxelCoordinate(), v.Chunk)));
                        BuildDesignations.Add(order);
                    }
                }
            }

            if(order != null)
            {
                order.WorkObjects.AddRange(Fence.CreateFences(World.ComponentManager,
                    ContentPaths.Entities.DwarfObjects.constructiontape,
                    order.VoxelOrders.Select(o => o.Voxel.tvh),
                    true));
                foreach (var obj in order.WorkObjects)
                {
                    obj.Manager.RootComponent.AddChild(obj);
                }
                TaskManager.AssignTasks(new List<Task>()
                {
                    new BuildRoomTask(order)
                }, Faction.FilterMinionsWithCapability(World.Master.SelectedMinions, GameMaster.ToolMode.Build));
            }
        }

        private void SetDisplayColor(Body body, Color color)
        {
            foreach (var sprite in body.EnumerateAll().OfType<Tinter>())
                sprite.VertexColorTint = color;
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
                if (Faction.FilterMinionsWithCapability(Faction.SelectedMinions, GameMaster.ToolMode.Build).Count == 0)
                {
                    World.ShowToolPopup("None of the selected units can build rooms.");
                }
                else if (CurrentRoomData.Verify(refs, Faction, World))
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

                    displayObjects = RoomLibrary.GenerateRoomComponentsTemplate(CurrentRoomData, refs, Faction.Components, 
                        World.ChunkManager.Content, World.ChunkManager.Graphics);

                    foreach(Body thing in displayObjects)
                    {
                        thing.SetFlagRecursive(GameComponent.Flag.Active, false);
                        SetDisplayColor(thing, Color.Green);
                    }
                }
            }
            else
            {
                foreach (VoxelHandle v in refs.Where(v => !v.IsEmpty))
                {
                    if (IsBuildDesignation(v.tvh))
                    {
                        var order = GetBuildDesignation(v);
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
                    else if (IsInRoom(v.tvh))
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
                if (Faction.FilterMinionsWithCapability(Faction.SelectedMinions, GameMaster.ToolMode.Build).Count == 0)
                {
                    World.ShowToolPopup("None of the selected units can build rooms.");
                }
                else if (CurrentRoomData.Verify(refs, Faction, World))
                {
                    BuildNewVoxels(refs.Select(v => v.tvh));    
                }
            }
            else
            {
                DeleteVoxels(refs);
            }
        }

        private void DeleteVoxels(IEnumerable<VoxelHandle> refs )
        {
            foreach(VoxelHandle v in refs.Select(r => r).Where(v => !v.IsEmpty))
            {
                if(IsBuildDesignation(v.tvh))
                {
                    BuildVoxelOrder vox = GetBuildDesignation(v);
                    vox.Order.Destroy();
                    BuildDesignations.Remove(vox.Order);
                }
                else if(IsInRoom(v.tvh))
                {
                    Room existingRoom = GetMostLikelyRoom(v);

                    if (existingRoom == null)
                    {
                        continue;
                    }

                    World.Gui.ShowDialog(new Gui.Widgets.Confirm
                        {
                            Text = "Do you want to destroy this" + existingRoom.RoomData.Name + "?",
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
