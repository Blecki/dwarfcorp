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
        public List<Body> OwnedObjects { get; set; }
        public List<Stockpile> Stockpiles { get; set; }
        public List<CreatureAI> Minions { get; set; }
        public RoomBuilder RoomBuilder { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public Timer HandleThreatsTimer { get; set; }
        public DesignationSet Designations = new DesignationSet();
        public Dictionary<ulong, VoxelHandle> GuardedVoxels = new Dictionary<ulong, VoxelHandle>();
        
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

        public List<Creature> Threats { get; set; }

        public string Name { get; set; }
        public List<CreatureAI> SelectedMinions { get; set; }
        public bool IsRaceFaction { get; set; }

        public float GoodWill { get; set; }

        [JsonIgnore]
        public WorldManager World { get; set; }

        public List<Treasury> Treasurys = new List<Treasury>();

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
            HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
            if (Threats == null)
            {
                Threats = new List<Creature>();
            }

            if (Minions == null)
            {
                Minions = new List<CreatureAI>();
            }
            Threats.RemoveAll(threat => threat == null || threat.IsDead);
            Minions.RemoveAll(minion => minion == null || minion.IsDead);
        }

        public Faction()
        {
            HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
        }

        public Faction(WorldManager world)
        {
            HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
            World = world;
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            Stockpiles = new List<Stockpile>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            OwnedObjects = new List<Body>();
            RoomBuilder = new RoomBuilder(this, world);
            IsRaceFaction = false;
            TradeMoney = 0.0m;
            GoodWill = 0.0f;
        }

        public Faction(NewOverworldFile.OverworldData.FactionDescriptor descriptor)
        {
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            Stockpiles = new List<Stockpile>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            OwnedObjects = new List<Body>();
            IsRaceFaction = false;
            TradeMoney = 0.0m;
            GoodWill = descriptor.GoodWill;
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

        public static List<CreatureAI> FilterMinionsWithCapability(List<CreatureAI> minions, Task.TaskCategory action)
        {
            return minions.Where(creature => creature.Stats.IsTaskAllowed(action)).ToList();
        }

        public void Update(DwarfTime time)
        {
            RoomBuilder.Faction = this;
            RoomBuilder.CheckRemovals();

            Minions.RemoveAll(m => m.IsDead);
            SelectedMinions.RemoveAll(m => m.IsDead);

            foreach (var m in Minions)
            {
                m.Creature.SelectionCircle.SetFlagRecursive(GameComponent.Flag.Visible, false);
                m.Creature.Sprite.DrawSilhouette = false;
            };

            foreach (CreatureAI creature in SelectedMinions)
            {
                creature.Creature.SelectionCircle.SetFlagRecursive(GameComponent.Flag.Visible, true);
                creature.Creature.Sprite.DrawSilhouette = true;
            }

            foreach (Room zone in GetRooms())
            {
                zone.ZoneBodies.RemoveAll(body => body.IsDead);
            }

            Designations.CleanupDesignations();

            foreach (var zone in RoomBuilder.DesignatedRooms)
            {
                zone.Update();
            }

            if (HandleThreatsTimer == null)
            {
                HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real
                    );
            }

            HandleThreatsTimer.Update(time);
            if (HandleThreatsTimer.HasTriggered)
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
                    if (!Designations.IsDesignation(threat.Physics, DesignationType.Attack))
                    { 
                        var g = new KillEntityTask(threat.Physics, KillEntityTask.KillType.Auto);
                        Designations.AddEntityDesignation(threat.Physics, DesignationType.Attack, null, g);
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

        public List<Room> GetRooms()
        {
            return RoomBuilder.DesignatedRooms;
        }

        public void OnVoxelDestroyed(VoxelHandle V)
        {
            if (!V.IsValid)
                return;

            RoomBuilder.OnVoxelDestroyed(V);

            var toRemove = new List<Stockpile>();
            foreach (var s in new List<Stockpile>(Stockpiles).Where(stockpile => stockpile.IsBuilt))
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

        public int ComputeRemainingStockpileSpace()
        {
            return Stockpiles.Sum(pile => pile.Resources.MaxResources - pile.Resources.CurrentResourceCount);
        }

        public int ComputeTotalStockpileSpace()
        {
            return Stockpiles.Sum(pile => pile.Resources.MaxResources);
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

        public Body FindNearestItemWithTags(string tag, Vector3 location, bool filterReserved, GameComponent queryObject)
        {
            Body closestItem = null;
            float closestDist = float.MaxValue;

            foreach (Body i in OwnedObjects)
            {
                if (i == null || i.IsDead || (i.IsReserved && filterReserved && i.ReservedFor != queryObject) || !(i.Tags.Any(t => tag == t))) continue;
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
            Dictionary<ResourceType, ResourceAmount> amounts = new Dictionary<ResourceType, ResourceAmount>();

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

        public bool HasResources(ResourceType resource)
        {
            return HasResources(new List<ResourceAmount>() { new ResourceAmount(resource) });
        }

        public bool RemoveResources(List<ResourceAmount> resources, Vector3 position, bool createItems = true)
        {
            Dictionary<ResourceType, ResourceAmount> amounts = new Dictionary<ResourceType, ResourceAmount>();

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


            List<Stockpile> stockpilesCopy = new List<Stockpile>(Stockpiles.Where(s => resources.All(r => s.IsAllowed(r.ResourceType))));
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

            AddMoney(-currentApplicant.Level.Pay * 4m);

            var dwarfPhysics = DwarfFactory.GenerateDwarf(
                    rooms.First().GetBoundingBox().Center() + Vector3.UnitY * 15,
                    World.ComponentManager, "Player", currentApplicant.Class, currentApplicant.Level.Index);
            World.ComponentManager.RootComponent.AddChild(dwarfPhysics);
            var newMinion = dwarfPhysics.EnumerateAll().OfType<Dwarf>().FirstOrDefault();
            System.Diagnostics.Debug.Assert(newMinion != null);

            newMinion.Stats.CurrentClass = currentApplicant.Class;
            newMinion.Stats.AllowedTasks = currentApplicant.Class.Actions;
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
            return Balloon.CreateBalloon(pos + new Vector3(0, 1000, 0), pos + Vector3.UnitY * 15, World.ComponentManager, new ShipmentOrder(0, null), this);
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

                    var tasks = new List<Task>();
                    foreach (var item in resources)
                        World.Master.TaskManager.AddTask(new GatherItemTask(item));
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
                }
                return;
            }

            DwarfBux amountRemaining = money;
            foreach (Treasury treasury in Treasurys)
            {
                if (amountRemaining <= 0)
                    break;

                DwarfBux maxInTreasury = treasury.Voxels.Count * Treasury.MoneyPerPile - treasury.Money;
                DwarfBux amountToTake = System.Math.Min(maxInTreasury, amountRemaining);

                amountRemaining -= amountToTake;
                treasury.Money += amountToTake;
            }
            if (amountRemaining > 0 && RoomBuilder.DesignatedRooms.Count > 0)
            {
                World.MakeAnnouncement("We need more treasuries!", null, () => 
                {
                    DwarfBux remainingSpace = 0;
                    foreach(var treasury in Treasurys)
                    {
                        remainingSpace += treasury.Money - treasury.Voxels.Count * Treasury.MoneyPerPile;
                    }
                    return remainingSpace > 0;
                });
                // Generate a number of coin piles.
                for (DwarfBux total = 0m; total < amountRemaining; total += 1024m)
                {
                    Zone randomZone = Datastructures.SelectRandom(RoomBuilder.DesignatedRooms);
                    Vector3 point = MathFunctions.RandVector3Box(randomZone.GetBoundingBox()) +
                                    new Vector3(0, 1.0f, 0);
                    CoinPile pile = EntityFactory.CreateEntity<CoinPile>("Coins Resource", point);
                    pile.Money = 1024m;

                    // Special case where we just need to add a little bit of money (less than 64 coins)
                    if (money - total < 1024m)
                    {
                        pile.Money = money - total;
                    }
                }
            }

        }

    }
}
