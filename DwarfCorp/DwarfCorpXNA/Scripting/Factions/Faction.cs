// Faction.cs
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
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A faction is an independent collection of creatures, tied to an economy, rooms, and designations.
    /// Examples might be the player's dwarves, or the faction of goblins.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Faction
    {
        public DwarfBux TradeMoney { get; set; }
        public Point Center { get; set; }
        public int TerritorySize { get; set; }
        public Economy Economy { get; set; }
        public List<TradeEnvoy> TradeEnvoys { get; set; }
        public List<WarParty> WarParties { get; set; }
        public Dictionary<ulong, BuildOrder> DigOrders { get; set; }
        public List<Body> OwnedObjects { get; set; }
        public List<Stockpile> Stockpiles { get; set; }
        public List<CreatureAI> Minions { get; set; }
        public RoomBuilder RoomBuilder { get; set; }
        public CraftBuilder CraftBuilder { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public List<FarmTile> FarmTiles = new List<FarmTile>();
        public List<VoxelDesignation> VoxelDesignations = new List<VoxelDesignation>();
        public List<PutDesignation> PutDesignations = new List<PutDesignation>();
        [JsonProperty] // Todo: Replace with more effecient data structure?
        private List<EntityDesignation> EntityDesignations = new List<EntityDesignation>();

        // Todo: When converting to new save system, it can take care of this.
        [JsonProperty]
        private string _race;

        [JsonIgnore]
        public Race Race
        {
            get
            {
                return RaceLibrary.FindRace(_race);
            }
            set
            {
                _race = value.Name;
            }
        }

        #region Designations

        public class VoxelDesignation
        {
            public VoxelHandle Voxel;
            public DesignationType Type;
            public Object Tag;
        }


        public void AddVoxelDesignation(VoxelHandle Voxel, DesignationType Type, Object Tag)
        {
            VoxelDesignations.Add(new VoxelDesignation
            {
                Voxel = Voxel,
                Type = Type,
                Tag = Tag
            });

            if (World.PlayerFaction == this)
                World.DesignationDrawer.HiliteVoxel(Voxel.Coordinate, Type);
        }

        public void RemoveVoxelDesignation(VoxelHandle Voxel, DesignationType Type)
        {
            VoxelDesignations.RemoveAll(d => d.Voxel == Voxel && d.Type == Type);
            if (World.PlayerFaction == this)
                World.DesignationDrawer.UnHiliteVoxel(Voxel.Coordinate, Type);
        }

        public Object GetVoxelDesignation(VoxelHandle Voxel, DesignationType Type)
        {
            var r = VoxelDesignations.FirstOrDefault(d => d.Voxel == Voxel && d.Type == Type);
            if (r != null) return r.Tag;
            return null;
        }

        public bool IsVoxelDesignation(VoxelHandle Voxel, DesignationType Type)
        {
            return VoxelDesignations.Any(d => d.Voxel == Voxel && d.Type == Type);
        }

        // This is temporary until all factions get a 'DesignationDrawer' instead.
        public class PutDesignation
        {
            public VoxelHandle Voxel;
            public VoxelType Type;
        }

        public bool IsPutDesignation(GlobalVoxelCoordinate Location)
        {
            return PutDesignations.Any(d => d.Voxel.Coordinate == Location);
        }

        public bool IsPutDesignation(VoxelHandle reference)
        {
            foreach (var put in PutDesignations)
            {
                if (put.Voxel == reference)
                    return true;
            }

            return false;
        }

        public PutDesignation GetPutDesignation(GlobalVoxelCoordinate Location)
        {
            return PutDesignations.FirstOrDefault(d => d.Voxel.Coordinate == Location);
        }

        // Todo: %KILL%
        public PutDesignation GetPutDesignation(VoxelHandle v)
        {
            foreach (var put in PutDesignations)
            {
                if (put.Voxel == v)
                    return put;
            }

            return null;
        }

        public void AddPutDesignation(PutDesignation des)
        {
            PutDesignations.Add(des);
            if (World.PlayerFaction == this)
                World.DesignationDrawer.HiliteVoxel(des.Voxel.Coordinate, DesignationType.Put, des.Type);
        }

        public void RemovePutDesignation(VoxelHandle v)
        {
            var des = GetPutDesignation(v);

            if (des != null)
            {
                PutDesignations.Remove(des);
                if (World.PlayerFaction == this)
                    World.DesignationDrawer.UnHiliteVoxel(des.Voxel.Coordinate, DesignationType.Put);
            }
        }


        public bool IsFarmDesignation(GlobalVoxelCoordinate Location)
        {
            return FarmTiles.Any(d => d.Voxel.Coordinate == Location);
        }

        public bool IsFarmDesignation(VoxelHandle reference)
        {
            foreach (var put in FarmTiles)
            {
                if (put.Voxel == reference)
                    return true;
            }

            return false;
        }

        public FarmTile GetFarmDesignation(GlobalVoxelCoordinate Location)
        {
            return FarmTiles.FirstOrDefault(d => d.Voxel.Coordinate == Location);
        }

        // Todo: %KILL%
        public FarmTile GetFarmDesignation(VoxelHandle v)
        {
            foreach (var put in FarmTiles)
            {
                if (put.Voxel == v)
                    return put;
            }

            return null;
        }

        public FarmTile AddFarmDesignation(VoxelHandle v, DesignationType Type)
        {
            var existing = GetFarmDesignation(v);
            if (existing != null)
            {
                if (World.PlayerFaction == this)
                    World.DesignationDrawer.UnHiliteVoxel(v.Coordinate, existing.ActiveDesignations);
            }
            else
            {
                if (Type != DesignationType.Farm)
                    throw new InvalidOperationException();

                existing = new FarmTile { Voxel = v };
                FarmTiles.Add(existing);
            }

            if (World.PlayerFaction == this)
                World.DesignationDrawer.HiliteVoxel(v.Coordinate, Type);

            existing.ActiveDesignations = Type;

            return existing;
        }

        // Todo: Hacks with the des type. Kill!
        //      - Since planting does not create farm tiles, does it need to support multiple types?
        public void RemoveFarmDesignation(VoxelHandle v, DesignationType Type)
        {
            var des = GetFarmDesignation(v);

            if (des != null)
            {
                if (des.ActiveDesignations == Type)
                {
                    if (World.PlayerFaction == this)
                    {
                        World.DesignationDrawer.UnHiliteVoxel(des.Voxel.Coordinate, Type);

                        if (Type != DesignationType.Farm)
                            World.DesignationDrawer.HiliteVoxel(v.Coordinate, DesignationType.Farm);
                    }

                    des.ActiveDesignations = DesignationType.Farm;

                    if (Type == DesignationType.Farm)
                        FarmTiles.Remove(des);
                }
            }
        }


        private class EntityDesignation
        {
            public Body Body;
            public DesignationType Type;
        }


        public enum AddEntityDesignationResult
        {
            AlreadyExisted,
            Added
        }

        public enum RemoveEntityDesignationResult
        {
            DidntExist,
            Removed
        }

        public AddEntityDesignationResult AddEntityDesignation(Body Entity, DesignationType Type)
        {
            if (EntityDesignations.Count(e => Object.ReferenceEquals(e.Body, Entity) && e.Type == Type) == 0)
            {
                EntityDesignations.Add(new EntityDesignation
                {
                    Body = Entity,
                    Type = Type
                });
                if (this == World.PlayerFaction)
                    World.DesignationDrawer.HiliteEntity(Entity, Type);
                return AddEntityDesignationResult.Added;
            }
            return AddEntityDesignationResult.AlreadyExisted;
        }

        public RemoveEntityDesignationResult RemoveEntityDesignation(Body Entity, DesignationType Type)
        {
            if (EntityDesignations.RemoveAll(e => Object.ReferenceEquals(e.Body, Entity) && e.Type == Type) != 0)
            {
                if (this == World.PlayerFaction)
                    World.DesignationDrawer.UnHiliteEntity(Entity, Type);
                return RemoveEntityDesignationResult.Removed;
            }
            return RemoveEntityDesignationResult.DidntExist;
        }

        public bool IsDesignation(Body Entity, DesignationType Type)
        {
            return EntityDesignations.Count(e => Object.ReferenceEquals(e.Body, Entity) && e.Type == Type) != 0;
        }

        #endregion

        public TaskManager TaskManager { get; set; }
        public List<Creature> Threats { get; set; }

        public string Name { get; set; }
        public List<CreatureAI> SelectedMinions { get; set; }
        public bool IsRaceFaction { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        public List<Treasury> Treasurys = new List<Treasury>();

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
        }

        public Faction()
        {

        }

        public Faction(WorldManager world)
        {
            World = world;
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            TaskManager = new TaskManager();
            Stockpiles = new List<Stockpile>();
            DigOrders = new Dictionary<ulong, BuildOrder>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            OwnedObjects = new List<Body>();
            RoomBuilder = new RoomBuilder(this, world);
            CraftBuilder = new CraftBuilder(this, world);
            IsRaceFaction = false;
            TradeMoney = 0.0m;
        }

        public Faction(OverworldFile.OverworldData.FactionDescriptor descriptor)
        {
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            TaskManager = new TaskManager();
            Stockpiles = new List<Stockpile>();
            DigOrders = new Dictionary<ulong, BuildOrder>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            OwnedObjects = new List<Body>();
            IsRaceFaction = false;
            TradeMoney = 0.0m;
            PrimaryColor = descriptor.PrimaryColory;
            SecondaryColor = descriptor.SecondaryColor;
            Name = descriptor.Name;
            Race = RaceLibrary.FindRace(descriptor.Race);
            Center = new Point(descriptor.CenterX, descriptor.CenterY);
        }


        private ulong GetVoxelQuickCompare(VoxelHandle V)
        {
            var coord = V.Coordinate.GetGlobalChunkCoordinate();
            var index = VoxelConstants.DataIndexOf(V.Coordinate.GetLocalVoxelCoordinate());

            ulong q = 0;
            q |= (((ulong)coord.X & 0xFFFF) << 48);
            q |= (((ulong)coord.Y & 0xFFFF) << 32);
            q |= (((ulong)coord.Z & 0xFFFF) << 16);
            q |= ((ulong)index & 0xFFFF);
            return q;
        }

        public static List<CreatureAI> FilterMinionsWithCapability(List<CreatureAI> minions, GameMaster.ToolMode action)
        {
            return minions.Where(creature => creature.Stats.CurrentClass.HasAction(action)).ToList();
        }

        public void Update(DwarfTime time)
        {
            RoomBuilder.Faction = this;
            CraftBuilder.Faction = this;
            RoomBuilder.CheckRemovals();

            Minions.RemoveAll(m => m.IsDead);
            SelectedMinions.RemoveAll(m => m.IsDead);

            foreach (var m in Minions)
            {
                m.Creature.SelectionCircle.IsVisible = false;
                m.Creature.Sprite.DrawSilhouette = false;
            };

            foreach (CreatureAI creature in SelectedMinions)
            {
                creature.Creature.SelectionCircle.IsVisible = true;
                creature.Creature.Sprite.DrawSilhouette = true;
            }

            foreach (Room zone in GetRooms())
            {
                zone.ZoneBodies.RemoveAll(body => body.IsDead);
            }

            List<ulong> removalKeys = new List<ulong>();
            foreach (var kvp in DigOrders)
            {
                var v = kvp.Value.Vox;
                if (v.IsValid && (v.IsEmpty || v.Health <= 0.0f || v.Type.Name == "empty" || v.Type.IsInvincible))
                {
                    if (this == World.PlayerFaction)
                        World.DesignationDrawer.UnHiliteVoxel(kvp.Value.Vox.Coordinate, DesignationType.Dig);
                    removalKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < removalKeys.Count; i++)
            {
                DigOrders.Remove(removalKeys[i]);
            }

            EntityDesignations.RemoveAll(b =>
            {
                if (b.Body.IsDead)
                {
                    if (this == World.PlayerFaction)
                        World.DesignationDrawer.UnHiliteEntity(b.Body, b.Type);
                }
                return b.Body.IsDead;
            });

            VoxelDesignations.RemoveAll(v => !v.Voxel.IsValid || v.Voxel.IsEmpty);

            foreach (var zone in RoomBuilder.DesignatedRooms)
            {
                zone.Update();
            }
            HandleThreats();

            OwnedObjects.RemoveAll(obj => obj.IsDead);

        }

        public bool IsTaskAssigned(Task task)
        {
            return Minions.Any(minion => minion.Tasks.Contains(task));
        }

        public CreatureAI GetNearestMinion(Vector3 location)
        {
            float closestDist = float.MaxValue;
            CreatureAI closestMinion = null;
            foreach (CreatureAI minion in Minions)
            {
                float dist = (minion.Position - location).LengthSquared();
                if (!(dist < closestDist)) continue;
                closestDist = dist;
                closestMinion = minion;
            }

            return closestMinion;
        }

        public void HandleThreats()
        {
            List<Task> tasks = new List<Task>();
            List<Creature> threatsToRemove = new List<Creature>();
            foreach (Creature threat in Threats)
            {
                if (threat != null && !threat.IsDead)
                {
                    Task g = new KillEntityTask(threat.Physics, KillEntityTask.KillType.Auto);

                    if (!IsTaskAssigned(g))
                    {
                        AddEntityDesignation(threat.Physics, DesignationType.Attack);
                        tasks.Add(g);
                    }
                    else
                    {
                        threatsToRemove.Add(threat);
                    }
                }
                else
                {
                    threatsToRemove.Add(threat);
                }
            }

            foreach (Creature threat in threatsToRemove)
            {
                Threats.Remove(threat);
            }

            TaskManager.AssignTasks(tasks, Minions);
        }

        public void AssignGather(IEnumerable<Body> items)
        {
            var tasks = items
                .Where(i => AddEntityDesignation(i, DesignationType.Gather) == AddEntityDesignationResult.Added)
                .Select(i => new GatherItemTask(i) as Task)
                .ToList();

            foreach (CreatureAI creature in Minions)
                foreach (var task in tasks)
                    creature.AssignTask(task);
        }

        public List<Room> GetRooms()
        {
            return RoomBuilder.DesignatedRooms;
        }

        public void OnVoxelDestroyed(VoxelHandle V)
        {
            if (!V.IsValid || V.IsEmpty)
                return;

            RoomBuilder.OnVoxelDestroyed(V);

            var toRemove = new List<Stockpile>();
            foreach (var s in new List<Stockpile>(Stockpiles))
            {
                if (s.ContainsVoxel(V))
                    s.RemoveVoxel(V);

                if (s.Voxels.Count == 0)
                    toRemove.Add(s);
            }

            foreach (Stockpile s in toRemove)
            {
                Stockpiles.Remove(s);
                s.Destroy();
            }
        }

        public int ComputeStockpileSpace()
        {
            return Stockpiles.Sum(pile => pile.Resources.MaxResources - pile.Resources.CurrentResourceCount);
        }

        public BuildOrder GetDigDesignation(VoxelHandle vox)
        {
            BuildOrder returnOrder;
            if (DigOrders.TryGetValue(GetVoxelQuickCompare(vox), out returnOrder))
                return returnOrder;
            return new BuildOrder();
        }

        public void AddDigDesignation(BuildOrder order)
        {
            if (!order.Vox.IsValid) return;
            DigOrders.Add(GetVoxelQuickCompare(order.Vox), order);
            if (this == World.PlayerFaction)
                World.DesignationDrawer.HiliteVoxel(order.Vox.Coordinate, DesignationType.Dig);
        }

        public void RemoveDigDesignation(VoxelHandle vox)
        {
            var qc = GetVoxelQuickCompare(vox);
            if (DigOrders.ContainsKey(qc))
            {
                DigOrders.Remove(qc);
                if (this == World.PlayerFaction)
                	World.DesignationDrawer.UnHiliteVoxel(vox.Coordinate, DesignationType.Dig);
            }
        }

        public bool IsDigDesignation(VoxelHandle vox)
        {
            return DigOrders.ContainsKey(GetVoxelQuickCompare(vox));
        }

        public bool AddResources(ResourceAmount resources)
        {
            ResourceAmount amount = new ResourceAmount(resources.ResourceType, resources.NumResources);
            foreach (Stockpile stockpile in Stockpiles)
            {
                int space = stockpile.Resources.MaxResources - stockpile.Resources.CurrentResourceCount;

                if (space >= amount.NumResources)
                {
                    stockpile.Resources.AddResource(amount);
                    stockpile.HandleBoxes();
                    return true;
                }
                else
                {
                    stockpile.Resources.AddResource(amount);
                    amount.NumResources -= space;
                    stockpile.HandleBoxes();
                    if (amount.NumResources == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Room GetNearestRoomOfType(string typeName, Vector3 position)
        {
            List<Room> rooms = GetRooms();
            Room desiredRoom = null;
            float nearestDistance = float.MaxValue;

            foreach (Room room in rooms)
            {
                if (room.RoomData.Name != typeName || !room.IsBuilt) continue;
                float dist =
                    (room.GetNearestVoxel(position).WorldPosition - position).LengthSquared();

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    desiredRoom = room;
                }
            }


            return desiredRoom;
        }


        public Stockpile GetNearestStockpile(Vector3 position, Func<Stockpile, bool> predicate)
        {
            Stockpile nearest = null;

            float closestDist = float.MaxValue;
            foreach (Stockpile stockpile in Stockpiles.Where(predicate))
            {
                if (!stockpile.IsBuilt)
                {
                    continue;
                }
                float dist = (stockpile.GetBoundingBox().Center() - position).LengthSquared();

                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = stockpile;
                }
            }

            return nearest;
        }

        public Stockpile GetIntersectingStockpile(BoundingBox v)
        {
            return Stockpiles.FirstOrDefault(pile => pile.Intersects(v));
        }

        public List<Stockpile> GetIntersectingStockpiles(BoundingBox v)
        {
            return Stockpiles.Where(pile => pile.Intersects(v)).ToList();
        }

        public List<Room> GetIntersectingRooms(BoundingBox v)
        {
            return RoomBuilder.DesignatedRooms.Where(room => room.Intersects(v)).ToList();
        }

        public bool IsInStockpile(VoxelHandle v)
        {
            return Stockpiles.Any(s => s.ContainsVoxel(v));
        }

        public Body GetRandomGatherDesignationWithTag(string tag)
        {
            var des = EntityDesignations.Where(d => d.Type == DesignationType.Gather &&
                d.Body.Tags.Contains(tag)).ToList();
            return des.Count == 0 ? null : des[MathFunctions.Random.Next(0, des.Count)].Body;
        }


        public bool HasFreeStockpile()
        {
            return Stockpiles.Any(s => s.IsBuilt && !s.IsFull());
        }

        public bool HasFreeTreasury()
        {
            return Treasurys.Any(s => s.IsBuilt && !s.IsFull());
        }

        public bool HasFreeStockpile(ResourceAmount toPut)
        {
            return Stockpiles.Any(s => s.IsBuilt && !s.IsFull() && s.IsAllowed(toPut.ResourceType));
        }

        public bool HasFreeTreasury(DwarfBux toPut)
        {
            return Treasurys.Any(s => s.IsBuilt && !s.IsFull());
        }

        public Body FindNearestItemWithTags(string tag, Vector3 location, bool filterReserved)
        {
            Body closestItem = null;
            float closestDist = float.MaxValue;

            foreach (Body i in OwnedObjects)
            {
                if (i == null || i.IsDead || (i.IsReserved && filterReserved) || !(i.Tags.Any(t => tag == t))) continue;
                float d = (i.GlobalTransform.Translation - location).LengthSquared();
                if (!(d < closestDist)) continue;
                closestDist = d;
                closestItem = i;
            }

            return closestItem;
        }

        public int CompareZones(Zone a, Zone b, Vector3 pos)
        {
            if (a == b)
            {
                return 0;
            }
            else
            {

                float costA = (pos - a.GetBoundingBox().Center()).LengthSquared();
                float costB = (pos - b.GetBoundingBox().Center()).LengthSquared();

                if (costA < costB)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        public Dictionary<string, Pair<ResourceAmount>> ListResourcesInStockpilesPlusMinions()
        {
            Dictionary<string, ResourceAmount> stocks = ListResources();
            Dictionary<string, Pair<ResourceAmount>> toReturn = new Dictionary<string, Pair<ResourceAmount>>();
            foreach (var pair in stocks)
            {
                toReturn[pair.Key] = new Pair<ResourceAmount>(pair.Value, new ResourceAmount(pair.Value.ResourceType, 0));
            }
            foreach (var creature in Minions)
            {
                var inventory = creature.Creature.Inventory;
                foreach (var i in inventory.Resources)
                {
                    var resource = i.Resource;
                    if (toReturn.ContainsKey(resource))
                    {
                        toReturn[resource].Second.NumResources += 1;
                    }
                    else
                    {
                        toReturn[resource] = new Pair<ResourceAmount>(new ResourceAmount(resource, 0), new ResourceAmount(resource));
                    }
                }
            }
            return toReturn;
        }

        public Dictionary<string, ResourceAmount> ListResources()
        {
            Dictionary<string, ResourceAmount> toReturn = new Dictionary<string, ResourceAmount>();

            foreach (Stockpile stockpile in Stockpiles)
            {
                foreach (ResourceAmount resource in stockpile.Resources)
                {
                    if (resource.NumResources == 0)
                    {
                        continue;
                    }

                    if (toReturn.ContainsKey(resource.ResourceType))
                    {
                        toReturn[resource.ResourceType].NumResources += resource.NumResources;
                    }
                    else
                    {
                        toReturn[resource.ResourceType] = new ResourceAmount(resource);
                    }
                }
            }
            return toReturn;
        }

        public List<ResourceAmount> GetResourcesWithTags(List<Quantitiy<Resource.ResourceTags>> tags, bool allowHeterogenous = false)
        {
            Dictionary<Resource.ResourceTags, int> tagsRequired = new Dictionary<Resource.ResourceTags, int>();
            Dictionary<Resource.ResourceTags, int> tagsGot = new Dictionary<Resource.ResourceTags, int>();
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> amounts = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();

            foreach (Quantitiy<Resource.ResourceTags> quantity in tags)
            {
                tagsRequired[quantity.ResourceType] = quantity.NumResources;
                tagsGot[quantity.ResourceType] = 0;
            }

            Random r = new Random();

            foreach (Stockpile stockpile in Stockpiles)
            {
                foreach (ResourceAmount resource in stockpile.Resources.OrderBy(x => r.Next()))
                {
                    foreach (var requirement in tagsRequired)
                    {
                        int got = tagsGot[requirement.Key];

                        if (requirement.Value <= got) continue;

                        if (!ResourceLibrary.GetResourceByName(resource.ResourceType).Tags.Contains(requirement.Key)) continue;

                        int amountToRemove = System.Math.Min(resource.NumResources, requirement.Value - got);

                        if (amountToRemove <= 0) continue;

                        tagsGot[requirement.Key] += amountToRemove;

                        if (amounts.ContainsKey(resource.ResourceType))
                        {
                            amounts[resource.ResourceType].NumResources += amountToRemove;
                        }
                        else
                        {
                            amounts[resource.ResourceType] = new ResourceAmount(resource.ResourceType, amountToRemove);
                        }
                    }
                }
            }

            if (allowHeterogenous)
            {
                return amounts.Values.ToList();
            }

            ResourceAmount maxAmount = null;
            foreach (var pair in amounts)
            {
                if (maxAmount == null || pair.Value.NumResources > maxAmount.NumResources)
                {
                    maxAmount = pair.Value;
                }
            }
            if (maxAmount != null)
            {
                return new List<ResourceAmount>() { maxAmount };
            }
            return new List<ResourceAmount>();
        }

        public bool HasResources(IEnumerable<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach (Quantitiy<Resource.ResourceTags> resource in resources)
            {
                int count = Stockpiles.Sum(stock => stock.Resources.GetResourceCount(resource.ResourceType));

                if (count < resource.NumResources)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasResources(IEnumerable<ResourceAmount> resources)
        {
            foreach (ResourceAmount resource in resources)
            {
                int count = Stockpiles.Sum(stock => stock.Resources.GetResourceCount(resource.ResourceType));

                if (count < resource.NumResources)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasResources(ResourceLibrary.ResourceType resource)
        {
            return HasResources(new List<ResourceAmount>() { new ResourceAmount(resource) });
        }

        public bool RemoveResources(List<ResourceAmount> resources, Vector3 position, bool createItems = true)
        {
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> amounts = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();

            foreach (ResourceAmount resource in resources)
            {
                if (!amounts.ContainsKey(resource.ResourceType))
                {
                    amounts.Add(resource.ResourceType, new ResourceAmount(resource));
                }
                else
                {
                    amounts[resource.ResourceType].NumResources += resource.NumResources;
                }
            }

            if (!HasResources(amounts.Values))
            {
                return false;
            }


            List<Stockpile> stockpilesCopy = new List<Stockpile>(Stockpiles);
            stockpilesCopy.Sort((a, b) => CompareZones(a, b, position));


            foreach (ResourceAmount resource in resources)
            {
                int count = 0;
                List<Vector3> positions = new List<Vector3>();
                foreach (Stockpile stock in stockpilesCopy)
                {
                    int num = stock.Resources.RemoveMaxResources(resource, resource.NumResources - count);
                    stock.HandleBoxes();
                    if (stock.Boxes.Count > 0)
                    {
                        for (int i = 0; i < num; i++)
                        {
                            positions.Add(stock.Boxes[stock.Boxes.Count - 1].LocalTransform.Translation);
                        }
                    }

                    count += num;

                    if (count >= resource.NumResources)
                    {
                        break;
                    }

                }

                if (createItems)
                {
                    foreach (Vector3 vec in positions)
                    {
                        Body newEntity =
                            EntityFactory.CreateEntity<Body>(resource.ResourceType + " Resource",
                                vec + MathFunctions.RandVector3Cube() * 0.5f);

                        TossMotion toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f),
                            2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                        newEntity.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                        newEntity.AnimationQueue.Add(toss);
                        toss.OnComplete += () => toss_OnComplete(newEntity);

                    }
                }

            }

            return true;
        }

        void toss_OnComplete(Body entity)
        {
            entity.Die();

        }

        public void Hire(Applicant currentApplicant)
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomData.Name == "BalloonPort").ToList();

            if (rooms.Count == 0)
            {
                return;
            }

            Economy.CurrentMoney -= currentApplicant.Level.Pay * 4m;

            var dwarfPhysics =
                EntityFactory.GenerateDwarf(
                    rooms.First().GetBoundingBox().Center() + Vector3.UnitY * 15,
                    World.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, World.ChunkManager,
                    World.Camera, this, World.PlanService, "Player", currentApplicant.Class, currentApplicant.Level.Index);
            World.ComponentManager.RootComponent.AddChild(dwarfPhysics);
            var newMinion = dwarfPhysics.EnumerateAll().OfType<Dwarf>().FirstOrDefault();
            System.Diagnostics.Debug.Assert(newMinion != null);

            newMinion.Stats.CurrentClass = currentApplicant.Class;
            newMinion.Stats.LevelIndex = currentApplicant.Level.Index - 1;
            newMinion.Stats.LevelUp();
            newMinion.Stats.FullName = currentApplicant.Name;
            newMinion.AI.AddMoney(currentApplicant.Level.Pay * 4m);
            newMinion.AI.Biography = currentApplicant.Biography;

            World.MakeAnnouncement(
                new Gui.Widgets.QueuedAnnouncement
                {
                    Text = String.Format("{0} was hired as a {1}.",
                        currentApplicant.Name, currentApplicant.Level.Name),
                    ClickAction = (gui, sender) => newMinion.AI.ZoomToMe()
                });

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
        }

        public Body DispatchBalloon()
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomData.Name == "BalloonPort").ToList();

            if (rooms.Count == 0)
            {
                return null;
            }

            Vector3 pos = rooms.First().GetBoundingBox().Center();
            return EntityFactory.CreateBalloon(pos + new Vector3(0, 1000, 0), pos + Vector3.UnitY * 15, World.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, new ShipmentOrder(0, null), this);
        }

        public List<Body> GenerateRandomSpawn(int numCreatures, Vector3 position)
        {
            if (Race.CreatureTypes.Count == 0)
            {
                return new List<Body>();
            }

            List<Body> toReturn = new List<Body>();
            for (int i = 0; i < numCreatures; i++)
            {
                string creature = Race.CreatureTypes[MathFunctions.Random.Next(Race.CreatureTypes.Count)];
                Vector3 offset = MathFunctions.RandVector3Cube() * 2;

                var voxelUnder = VoxelHelpers.FindFirstVoxelBelowIncludeWater(new VoxelHandle(
                    World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(position + offset)));
                if (voxelUnder.IsValid)
                {
                    var body = EntityFactory.CreateEntity<Body>(creature, voxelUnder.WorldPosition + new Vector3(0.5f, 1, 0.5f));
                    var ai = body.EnumerateAll().OfType<CreatureAI>().FirstOrDefault();

                    if (ai != null)
                    {
                        ai.Faction.Minions.Remove(ai);

                        Minions.Add(ai);
                        ai.Faction = this;
                        ai.Creature.Allies = Name;
                    }

                    toReturn.Add(body);
                }
            }

            return toReturn;
        }

        public int CountResourcesWithTag(Resource.ResourceTags tag)
        {
            List<ResourceAmount> resources = ListResourcesWithTag(tag);
            int amounts = 0;

            foreach (ResourceAmount amount in resources)
            {
                amounts += amount.NumResources;
            }

            return amounts;
        }

        public List<ResourceAmount> ListResourcesWithTag(Resource.ResourceTags tag, bool allowHeterogenous = true)
        {
            Dictionary<string, ResourceAmount> resources = ListResources();
            if (allowHeterogenous)
            {
                return (from pair in resources
                        where ResourceLibrary.GetResourceByName(pair.Value.ResourceType).Tags.Contains(tag)
                        select pair.Value).ToList();
            }
            ResourceAmount maxAmount = null;
            foreach (var pair in resources)
            {
                var resource = ResourceLibrary.GetResourceByName(pair.Value.ResourceType);
                if (!resource.Tags.Contains(tag)) continue;
                if (maxAmount == null || pair.Value.NumResources > maxAmount.NumResources)
                {
                    maxAmount = pair.Value;
                }
            }
            return maxAmount != null ? new List<ResourceAmount>() { maxAmount } : new List<ResourceAmount>();
        }

        public Room GetNearestRoom(Vector3 position)
        {
            List<Room> rooms = GetRooms();
            Room desiredRoom = null;
            float nearestDistance = float.MaxValue;

            foreach (Room room in rooms)
            {
                if (room.Voxels.Count == 0) continue;
                float dist =
                    (room.GetNearestVoxel(position).WorldPosition - position).LengthSquared();

                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    desiredRoom = room;
                }
            }


            return desiredRoom;
        }

        public void AddMinion(CreatureAI minion)
        {
            Minions.Add(minion);
            Inventory targetInventory = minion.GetRoot().GetComponent<Inventory>();

            if (targetInventory != null)
            {
                targetInventory.OnDeath += resources =>
                {
                    if (resources == null) return;
                    AssignGather(resources);
                };
            }
        }

        public void AddMoney(DwarfBux money)
        {
            if (money == 0.0m)
            {
                return;
            }

            // In this case, we need to remove money from the economy.
            // This means that we first take money from treasuries. If there is any left,
            // we subtract it from the current money count.
            if (money < 0)
            {
                DwarfBux amountLeft = -money;
                foreach (Treasury treasury in Treasurys)
                {
                    DwarfBux amountToTake = System.Math.Min(treasury.Money, amountLeft);
                    treasury.Money -= amountToTake;
                    amountLeft -= amountToTake;
                    Economy.CurrentMoney -= amountToTake;
                }
                Economy.CurrentMoney -= amountLeft;
                Economy.CurrentMoney = System.Math.Max(Economy.CurrentMoney, 0m);
                return;
            }



            // If there are no minions, we add money to treasuries first, then generate random coin piles.
            if (Minions.Count == 0)
            {
                DwarfBux amountRemaining = money;
                foreach (Treasury treasury in Treasurys)
                {
                    if (amountRemaining <= 0)
                        break;

                    DwarfBux maxInTreasury = treasury.Money - treasury.Voxels.Count * Treasury.MoneyPerPile;
                    DwarfBux amountToTake = System.Math.Min(maxInTreasury, amountRemaining);

                    amountRemaining -= amountToTake;
                    treasury.Money += amountToTake;
                    Economy.CurrentMoney += amountToTake;
                }
                if (amountRemaining > 0 && RoomBuilder.DesignatedRooms.Count > 0)
                {
                    // Generate a number of coin piles.
                    for (DwarfBux total = 0m; total < amountRemaining; total += 64m)
                    {
                        Zone randomZone = Datastructures.SelectRandom(RoomBuilder.DesignatedRooms);
                        Vector3 point = MathFunctions.RandVector3Box(randomZone.GetBoundingBox()) +
                                        new Vector3(0, 1.0f, 0);
                        CoinPile pile = EntityFactory.CreateEntity<CoinPile>("Coins Resource", point);
                        pile.Money = 64m;

                        // Special case where we just need to add a little bit of money (less than 64 coins)
                        if (money - total < 64m)
                        {
                            pile.Money = money - total;
                        }
                    }
                }
            }
            // In this case, add money to the wallet of each minion and tell him/her to stock the money.
            else
            {
                int amountPerMinion = (int)(money / (decimal)Minions.Count);
                DwarfBux remaining = money;
                foreach (var minion in Minions)
                {
                    minion.Status.Money += (DwarfBux)amountPerMinion;
                    remaining -= (DwarfBux)amountPerMinion;
                    minion.GatherManager.StockMoneyOrders.Add(new GatherManager.StockMoneyOrder()
                    {
                        Money = amountPerMinion
                    });
                }

                Minions[0].Status.Money += remaining;
                Minions[0].GatherManager.StockMoneyOrders.Add(new GatherManager.StockMoneyOrder()
                {
                    Money = remaining
                });
            }
        }
    }
}
