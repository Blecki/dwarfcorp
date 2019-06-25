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
    public class ZoneBuilder
    {
        public List<Zone> Zones { get; set; } // Todo: Track down all references to the room list. Channel all room management through this type.
        public List<BuildZoneOrder> BuildDesignations { get; set; }
        public ZoneType CurrentZoneData { get; set; }

        [JsonIgnore]
        private WorldManager World { get; set; }

        [OnDeserialized]
        public void OnDeserializing(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        public void DestroyBuildDesignation(VoxelHandle V)
        {
            var vox = GetBuildDesignation(V);
            if (vox != null && vox.Order != null)
            {
                vox.Order.Destroy();
                if (vox.Order.DisplayWidget != null)
                    World.UserInterface.Gui.DestroyWidget(vox.Order.DisplayWidget);
                BuildDesignations.Remove(vox.Order);
                Zones.Remove(vox.Order.ToBuild);
            }
        }

        public void DestroyZone(Zone Z)
        {
            Zones.Remove(Z);

            var existingDesignations = GetDesignationsAssociatedWithZone(Z);
            BuildZoneOrder buildRoomDes = null;
            foreach (var des in existingDesignations)
            {
                des.Order.VoxelOrders.Remove(des);
                buildRoomDes = des.Order;
            }

            if (buildRoomDes != null && buildRoomDes.DisplayWidget != null)
                World.UserInterface.Gui.DestroyWidget(buildRoomDes.DisplayWidget);

            BuildDesignations.Remove(buildRoomDes);
            Z.Destroy();
        }

        public void AddZone(Zone Z)
        {
            Zones.Add(Z);
        }

        public Zone FindZone(String ID)
        {
            return Zones.FirstOrDefault(s => s.ID == ID);
        }

        public List<Zone> FilterRoomsByType(string type)
        {
            return Zones.Where(r => r.Type.Name == type).ToList();
        }

        public void End()
        {
            CurrentZoneData = null;
        }

        public ZoneBuilder()
        {
            
        }

        public ZoneBuilder(WorldManager world)
        {
            World = world;
            Zones = new List<Zone>();
            BuildDesignations = new List<BuildZoneOrder>();
            CurrentZoneData = Library.GetZoneType("BedRoom");
        }

        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public bool IsInZone(VoxelHandle v)
        {
            return Zones.Any(r => r.ContainsVoxel(v));
        }

        public Zone GetRoomThatContainsVoxel(VoxelHandle V)
        {
            return Zones.FirstOrDefault(r => r.ContainsVoxel(V));
        }

        public bool IsBuildDesignation(VoxelHandle v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).Any(buildDesignation => buildDesignation.Voxel == v);
        }

        public BuildVoxelOrder GetBuildDesignation(VoxelHandle v)
        {
            return BuildDesignations.SelectMany(room => room.VoxelOrders).FirstOrDefault(buildDesignation => buildDesignation.Voxel == v);
        }

        public BuildZoneOrder GetMostLikelyDesignation(VoxelHandle v)
        {
            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            return (from room in BuildDesignations
                from buildDesignation in room.VoxelOrders
                where larger.Intersects(buildDesignation.Voxel.GetBoundingBox())
                select room).FirstOrDefault();
        }

        public Zone GetMostLikelyZone(VoxelHandle v)
        {
            foreach(var r in Zones.Where(r => r.ContainsVoxel(v)))
                return r;

            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.5f, 0.5f, 0.5f), v.GetBoundingBox().Max + new Vector3(0.5f, 0.5f, 0.5f));

            return (from room in BuildDesignations
                from buildDesignation in room.VoxelOrders
                where larger.Intersects(buildDesignation.Voxel.GetBoundingBox())
                select buildDesignation.ToBuild).FirstOrDefault();
        }

        public List<BuildVoxelOrder> GetDesignationsAssociatedWithZone(Zone room)
        {
            return (from roomDesignation in BuildDesignations
                from des in roomDesignation.VoxelOrders
                where des.ToBuild == room
                select des).ToList();
        }

        public void Render(DwarfTime game, GraphicsDevice graphics)
        {
            
        }

        public void OnVoxelDestroyed(VoxelHandle voxDestroyed)
        {
            var toDestroy = new List<Zone>();

            lock (Zones)
            {
                var toCheck = new List<Zone>();
                toCheck.AddRange(Zones.Where(r => r.IsBuilt));
                foreach (var r in toCheck)
                    if (r.RemoveVoxel(voxDestroyed))
                        toDestroy.Add(r);

                foreach (var r in toDestroy)
                {
                    Zones.Remove(r);
                    r.Destroy();
                }
            }
        }

        public void Update(DwarfTime Time)
        {
            var toRemove = BuildDesignations.Where(build => build.IsBuilt).ToList();

            foreach (var build in toRemove)
            {
                if (build.DisplayWidget != null)
                    World.UserInterface.Gui.DestroyWidget(build.DisplayWidget);
                BuildDesignations.Remove(build);
            }

            foreach (var zone in Zones)
                zone.Update(Time);

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
                        if (!World.HasResources(requiredResources))
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
                                Text = String.Format("Need {0} to build this {1}", resourceList, buildOrder.ToBuild.Type.Name),
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
                        if (World.HasResources(requiredResources))
                        {
                            buildOrder.DisplayWidget.Root.DestroyWidget(buildOrder.DisplayWidget);
                            buildOrder.DisplayWidget = null;
                        }
                        else
                        {
                            var center = buildOrder.GetBoundingBox().Center();
                            var projection = World.Renderer.Camera.Project(center);
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
            var toBuild = Library.CreateZone(CurrentZoneData.Name, World);
            var order = new BuildZoneOrder(toBuild, World);
            BuildDesignations.Add(order);
            Zones.Add(toBuild);

            foreach (var v in designations.Where(v => v.IsValid && !v.IsEmpty))
                order.VoxelOrders.Add(new BuildVoxelOrder(order, order.ToBuild, v));

            order.WorkObjects.AddRange(Fence.CreateFences(World.ComponentManager,
                ContentPaths.Entities.DwarfObjects.constructiontape,
                order.VoxelOrders.Select(o => o.Voxel),
                true));
            foreach (var obj in order.WorkObjects)
                obj.Manager.RootComponent.AddChild(obj);

            World.TaskManager.AddTask(new BuildRoomTask(order, this));
        }

        public void OnVoxelsDragged(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            World.UserInterface.VoxSelector.SelectionColor = Color.White;

            foreach (BuildZoneOrder order in BuildDesignations)
                order.SetTint(Color.White);
            
            foreach (var room in World.EnumerateZones())
                room.SetTint(Color.White);
            
            if (CurrentZoneData == null)
                return;
            
            if (button == InputManager.MouseButton.Left)
            {
                World.Tutorial("build " + CurrentZoneData.Name);

                if (CurrentZoneData.CanBuildHere(refs, World))
                {
                    List<Quantitiy<Resource.ResourceTags>> requirements = CurrentZoneData.GetRequiredResources(refs.Count);

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
                    World.UserInterface.VoxSelector.SelectionColor = GameSettings.Default.Colors.GetColor("Negative", Color.Red);
            }
        }

        public void VoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {
            foreach (BuildZoneOrder order in BuildDesignations)
                order.SetTint(Color.White);

            foreach (var room in World.EnumerateZones()) // Todo: Doesn't this loopback? L-O-L.
                room.SetTint(Color.White);

            if(CurrentZoneData == null)
                return;

            if(button == InputManager.MouseButton.Left)
                if (CurrentZoneData.CanBuildHere(refs, World))
                    BuildNewVoxels(refs);    
        }
    }
}
