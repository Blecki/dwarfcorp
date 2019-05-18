using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Diagnostics;

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
        public Company Economy { get; set; }
        public List<TradeEnvoy> TradeEnvoys { get; set; }
        public List<WarParty> WarParties { get; set; }
        public List<GameComponent> OwnedObjects { get; set; }
        public List<Stockpile> Stockpiles { get; set; }
        public List<CreatureAI> Minions { get; set; }
        public RoomBuilder RoomBuilder { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public Timer HandleThreatsTimer { get; set; }
        public Timer HandleStockpilesTimer { get; set; }
        public DesignationSet Designations = new DesignationSet();
        public Dictionary<ulong, VoxelHandle> GuardedVoxels = new Dictionary<ulong, VoxelHandle>();
        public Dictionary<Resource.ResourceTags, int> CachedResourceTagCounts = new Dictionary<Resource.ResourceTags, int>();
        public Dictionary<string, bool> CachedCanBuildVoxel = new Dictionary<string, bool>();
        public bool ClaimsColony = false;
        public bool IsMotherland = false;
        public float DistanceToCapital = 0.0f;

        public struct ApplicantArrival
        {
            public Applicant Applicant;
            public DateTime ArrivalTime;
        }

        public List<ApplicantArrival> NewArrivals = new List<ApplicantArrival>();



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

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ((WorldManager)ctx.Context);
            HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
            HandleStockpilesTimer = new Timer(5.5f, false, Timer.TimerMode.Real);
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
            HandleStockpilesTimer = new Timer(5.5f, false, Timer.TimerMode.Real);
        }

        public Faction(WorldManager world)
        {
            HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
            HandleStockpilesTimer = new Timer(5.5f, false, Timer.TimerMode.Real);
            World = world;
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            Stockpiles = new List<Stockpile>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            OwnedObjects = new List<GameComponent>();
            RoomBuilder = new RoomBuilder(this, world);
            IsRaceFaction = false;
            TradeMoney = 0.0m;
            GoodWill = 0.0f;
        }

        public Faction(OverworldMetaData.FactionDescriptor descriptor)
        {
            Threats = new List<Creature>();
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            Stockpiles = new List<Stockpile>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            OwnedObjects = new List<GameComponent>();
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

            if (this == World.PlayerFaction) // Todo: This sucks.
            {
                #region Mourn dead minions
                if (Minions.Any(m => m.IsDead && m.TriggersMourning))
                {
                    foreach (var minion in Minions)
                    {
                        minion.Creature.AddThought(Thought.ThoughtType.FriendDied);

                        if (!minion.IsDead) continue;

                        World.MakeAnnouncement(String.Format("{0} ({1}) died!", minion.Stats.FullName, minion.Stats.CurrentClass.Name));
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
                        World.Tutorial("death");
                    }
                }

                #endregion

                #region Free stuck minions
                foreach (var minion in Minions)
                {
                    if (minion == null) throw new InvalidProgramException("Null minion?");
                    if (minion.Stats == null) throw new InvalidProgramException("Minion has null status?");

                    if (minion.Stats.IsAsleep)
                        continue;

                    if (minion.CurrentTask == null)
                        continue;

                    if (minion.Stats.IsTaskAllowed(Task.TaskCategory.Dig))
                        minion.Movement.SetCan(MoveType.Dig, GameSettings.Default.AllowAutoDigging);

                    minion.ResetPositionConstraint();
                }
                #endregion
            }

            Minions.RemoveAll(m => m.IsDead);
            SelectedMinions.RemoveAll(m => m.IsDead);

            if (this == World.PlayerFaction)
            {
                foreach (var body in OwnedObjects)
                    if (body.ReservedFor != null && body.ReservedFor.IsDead)
                        body.ReservedFor = null;

                foreach (var m in Minions)
                {
                    var selectionCircle = m.GetRoot().GetComponent<SelectionCircle>();
                    if (selectionCircle != null)
                        selectionCircle.SetFlagRecursive(GameComponent.Flag.Visible, false);
                    m.Creature.Sprite.DrawSilhouette = false;
                };

                foreach (var creature in SelectedMinions)
                {
                    var selectionCircle = creature.GetRoot().GetComponent<SelectionCircle>();
                    if (selectionCircle == null)
                        selectionCircle = creature.GetRoot().AddChild(new SelectionCircle(creature.Manager)) as SelectionCircle;
                    selectionCircle.SetFlag(GameComponent.Flag.ShouldSerialize, false);
                    selectionCircle.SetFlagRecursive(GameComponent.Flag.Visible, true);
                    creature.Creature.Sprite.DrawSilhouette = true;
                }
            }

            foreach (Room zone in GetRooms())
                zone.ZoneBodies.RemoveAll(body => body.IsDead);

            Designations.CleanupDesignations();

            foreach (var zone in RoomBuilder.DesignatedRooms)
                zone.Update();

            if (HandleThreatsTimer == null)
                HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);

            if (HandleStockpilesTimer == null)
                HandleStockpilesTimer = new Timer(5.5f, false, Timer.TimerMode.Real);

            HandleThreatsTimer.Update(time);
            if (HandleThreatsTimer.HasTriggered)
                HandleThreats();

            HandleStockpilesTimer.Update(time);
            if (HandleStockpilesTimer.HasTriggered)
            {
                if (this == World.PlayerFaction)
                {
                    foreach (var stockpile in Stockpiles)
                    {
                        foreach (var blacklist in stockpile.BlacklistResources)
                        {
                            foreach (var resourcePair in stockpile.Resources.Resources)
                            {
                                if (resourcePair.Value.Count == 0)
                                    continue;

                                var resourceType = ResourceLibrary.GetResourceByName(resourcePair.Key);
                                if (resourceType.Tags.Any(tag => tag == blacklist))
                                {
                                    var transferTask = new TransferResourcesTask(stockpile.ID, resourcePair.Value.CloneResource());
                                    if (World.TaskManager.HasTask(transferTask))
                                        continue;
                                    World.TaskManager.AddTask(transferTask);
                                }
                            }
                        }
                    }
                }
            }

            if (World.ComponentManager.NumComponents() > 0)
                OwnedObjects.RemoveAll(obj => obj.IsDead || obj.Parent == null || !obj.Manager.HasComponent(obj.GlobalID));

            foreach (var applicant in NewArrivals)
                if (World.Time.CurrentDate >= applicant.ArrivalTime)
                    HireImmediately(applicant.Applicant);

            NewArrivals.RemoveAll(a => World.Time.CurrentDate >= a.ArrivalTime);
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
                        //var g = new KillEntityTask(threat.Physics, KillEntityTask.KillType.Auto);
                        //Designations.AddEntityDesignation(threat.Physics, DesignationType.Attack, null, g);
                        //tasks.Add(g);
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

            TaskManager.AssignTasksGreedy(tasks, Minions);
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
                foreach (var resource in s.Resources.Enumerate())
                {
                    var resourceType = ResourceLibrary.GetResourceByName(resource.Type);

                    foreach (var tag in resourceType.Tags)
                    {
                        if (CachedResourceTagCounts.ContainsKey(tag))
                        {
                            CachedResourceTagCounts[tag] -= resource.Count;
                            Trace.Assert(CachedResourceTagCounts[tag] >= 0);
                        }
                    }
                }
                RecomputeCachedVoxelstate();
                Stockpiles.Remove(s);
                s.Destroy();
            }
        }

        public int ComputeRemainingStockpileSpace()
        {
            return Stockpiles.Where(pile => !((pile is Graveyard))).Sum(pile => pile.Resources.MaxResources - pile.Resources.CurrentResourceCount);
        }

        public int ComputeTotalStockpileSpace()
        {
            return Stockpiles.Where(pile => !((pile is Graveyard))).Sum(pile => pile.Resources.MaxResources);
        }

        public bool AddResources(ResourceAmount resources)
        {
            ResourceAmount amount = new ResourceAmount(resources.Type, resources.Count);
            var resource = ResourceLibrary.GetResourceByName(amount.Type);
            foreach (Stockpile stockpile in Stockpiles.Where(s => s.IsAllowed(resources.Type)))
            {
                int space = stockpile.Resources.MaxResources - stockpile.Resources.CurrentResourceCount;

                if (space >= amount.Count)
                {
                    stockpile.Resources.AddResource(amount);
                    stockpile.HandleBoxes();
                    foreach (var tag in resource.Tags)
                    {
                        if (!CachedResourceTagCounts.ContainsKey(tag))
                        {
                            CachedResourceTagCounts[tag] = 0;
                        }
                        CachedResourceTagCounts[tag] += amount.Count;
                    }
                    RecomputeCachedVoxelstate();
                    return true;
                }
                else
                {
                    var amountToMove = space;
                    stockpile.Resources.AddResource(new ResourceAmount(resources.Type, amountToMove));
                    amount.Count -= amountToMove;

                    stockpile.HandleBoxes();
                    foreach (var tag in resource.Tags)
                    {
                        if (!CachedResourceTagCounts.ContainsKey(tag))
                        {
                            CachedResourceTagCounts[tag] = 0;
                        }
                        CachedResourceTagCounts[tag] += amountToMove;
                    }
                    RecomputeCachedVoxelstate();
                    if (amount.Count == 0)
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

        public bool HasFreeStockpile(ResourceAmount toPut)
        {
            return Stockpiles.Any(s => s.IsBuilt && !s.IsFull() && s.IsAllowed(toPut.Type));
        }

        public GameComponent FindNearestItemWithTags(string tag, Vector3 location, bool filterReserved, GameComponent queryObject)
        {
            GameComponent closestItem = null;
            float closestDist = float.MaxValue;

            if (OwnedObjects == null)
                return null;

            foreach (GameComponent i in OwnedObjects)
            {
                if (i == null) continue;
                if (i.IsDead) continue;
                if (i.IsReserved && filterReserved && i.ReservedFor != queryObject) continue;
                if (i.Tags == null || !(i.Tags.Any(t => tag == t))) continue;

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
                toReturn[pair.Key] = new Pair<ResourceAmount>(pair.Value, new ResourceAmount(pair.Value.Type, 0));
            }
            foreach (var creature in Minions)
            {
                var inventory = creature.Creature.Inventory;
                foreach (var i in inventory.Resources)
                {
                    var resource = i.Resource;
                    if (toReturn.ContainsKey(resource))
                    {
                        toReturn[resource].Second.Count += 1;
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
                foreach (ResourceAmount resource in stockpile.Resources.Enumerate())
                {
                    if (resource.Count == 0)
                    {
                        continue;
                    }

                    if (toReturn.ContainsKey(resource.Type))
                    {
                        toReturn[resource.Type].Count += resource.Count;
                    }
                    else
                    {
                        toReturn[resource.Type] = new ResourceAmount(resource);
                    }
                }
            }
            return toReturn;
        }

        public IEnumerable<KeyValuePair<Stockpile, ResourceAmount>> GetStockpilesContainingResources(Vector3 biasPos, IEnumerable<ResourceAmount> required)
        {
            foreach (var amount in required)
            {
                int numGot = 0;
                String selectedString = null;
                foreach (Stockpile stockpile in Stockpiles.OrderBy(s => (s.GetBoundingBox().Center() - biasPos).LengthSquared()))
                {
                    if (numGot >= amount.Count)
                        break;
                    foreach (var resource in stockpile.Resources.Enumerate().Where(sResource => sResource.Type == amount.Type))
                    {
                        int amountToRemove = global::System.Math.Min(resource.Count, amount.Count - numGot);
                        if (amountToRemove <= 0) continue;
                        numGot += amountToRemove;
                        yield return new KeyValuePair<Stockpile, ResourceAmount>(stockpile, new ResourceAmount(resource.Type, amountToRemove));
                    }
                }
            }
        }


        public IEnumerable<KeyValuePair<Stockpile, ResourceAmount>> GetStockpilesContainingResources(List<Quantitiy<Resource.ResourceTags>> tags, bool allowHeterogenous = false)
        {
            foreach (var tag in tags)
            {
                int numGot = 0;
                String selectedString = null;
                foreach (Stockpile stockpile in Stockpiles)
                {
                    if (numGot >= tag.Count)
                        break;
                    foreach (var resource in stockpile.Resources.Enumerate().Where(sResource => ResourceLibrary.GetResourceByName(sResource.Type).Tags.Contains(tag.Type)))
                    {
                        if (!allowHeterogenous && selectedString != null && selectedString != resource.Type)
                            continue;
                        int amountToRemove = global::System.Math.Min(resource.Count, tag.Count - numGot);
                        if (amountToRemove <= 0) continue;
                        numGot += amountToRemove;
                        yield return new KeyValuePair<Stockpile, ResourceAmount>(stockpile, new ResourceAmount(resource.Type, amountToRemove));
                    }
                }
            }
        }

        public List<ResourceAmount> GetResourcesWithTags(List<Quantitiy<Resource.ResourceTags>> tags, bool allowHeterogenous = false)
        {
            Dictionary<Resource.ResourceTags, int> tagsRequired = new Dictionary<Resource.ResourceTags, int>();
            Dictionary<Resource.ResourceTags, int> tagsGot = new Dictionary<Resource.ResourceTags, int>();
            Dictionary<String, ResourceAmount> amounts = new Dictionary<String, ResourceAmount>();

            foreach (Quantitiy<Resource.ResourceTags> quantity in tags)
            {
                tagsRequired[quantity.Type] = quantity.Count;
                tagsGot[quantity.Type] = 0;
            }

            Random r = new Random();

            foreach (Stockpile stockpile in Stockpiles)
            {
                foreach (ResourceAmount resource in stockpile.Resources.Enumerate().OrderBy(x => r.Next()))
                {
                    foreach (var requirement in tagsRequired)
                    {
                        int got = tagsGot[requirement.Key];

                        if (requirement.Value <= got) continue;

                        if (!ResourceLibrary.GetResourceByName(resource.Type).Tags.Contains(requirement.Key)) continue;

                        int amountToRemove = global::System.Math.Min(resource.Count, requirement.Value - got);

                        if (amountToRemove <= 0) continue;

                        tagsGot[requirement.Key] += amountToRemove;

                        if (amounts.ContainsKey(resource.Type))
                        {
                            amounts[resource.Type].Count += amountToRemove;
                        }
                        else
                        {
                            amounts[resource.Type] = new ResourceAmount(resource.Type, amountToRemove);
                        }
                    }
                }
            }

            if (allowHeterogenous)
            {
                return amounts.Values.ToList();
            }

            List<ResourceAmount> toReturn = new List<ResourceAmount>();

            foreach (var requirement in tagsRequired)
            {
                ResourceAmount maxAmount = null;
                foreach (var pair in amounts)
                {
                    if (!ResourceLibrary.GetResourceByName(pair.Key).Tags.Contains(requirement.Key)) continue;
                    if (maxAmount == null || pair.Value.Count > maxAmount.Count)
                    {
                        maxAmount = pair.Value;
                    }
                }
                if (maxAmount != null)
                {
                    toReturn.Add(maxAmount);
                }
            }
            return toReturn;
        }


        public void RecomputeCachedVoxelstate()
        {
            foreach (var type in Library.EnumerateVoxelTypes())
            {
                bool nospecialRequried = type.BuildRequirements.Count == 0;
                CachedCanBuildVoxel[type.Name] = type.IsBuildable && ((nospecialRequried && HasResources(type.ResourceToRelease)) ||
                    (!nospecialRequried && HasResourcesCached(type.BuildRequirements)));
            }
        }

        public void RecomputeCachedResourceState()
        {
            CachedResourceTagCounts.Clear();
            foreach (var resource in ListResources())
            {
                var type = ResourceLibrary.GetResourceByName(resource.Key);
               
                foreach(var tag in type.Tags)
                {
                    Trace.Assert(type.Tags.Count(t => t == tag) == 1);
                    if (!CachedResourceTagCounts.ContainsKey(tag))
                    {
                        CachedResourceTagCounts[tag] = resource.Value.Count;
                    }
                    else
                    {
                        CachedResourceTagCounts[tag] += resource.Value.Count;
                    }
                }
            }
            RecomputeCachedVoxelstate();
        }

        public bool CanBuildVoxel(VoxelType type)
        {
            return CachedCanBuildVoxel[type.Name];
        }

        public bool HasResourcesCached(IEnumerable<Resource.ResourceTags> resources)
        {
            foreach (var resource in resources)
            {
                if (!CachedResourceTagCounts.ContainsKey(resource))
                {
                    return false;
                }

                if (CachedResourceTagCounts[resource] == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasResourcesCached(IEnumerable<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach(var resource in resources)
            {
                if (!CachedResourceTagCounts.ContainsKey(resource.Type))
                {
                    return false;
                }

                if (CachedResourceTagCounts[resource.Type] < resource.Count)
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasResources(IEnumerable<Quantitiy<Resource.ResourceTags>> resources, bool allowHeterogenous = false)
        {
            foreach (Quantitiy<Resource.ResourceTags> resource in resources)
            {
                int count = Stockpiles.Sum(stock => stock.Resources.GetResourceCount(resource.Type, allowHeterogenous));

                if (count < resource.Count)
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
                int count = Stockpiles.Sum(stock => stock.Resources.GetResourceCount(resource.Type));

                if (count < resources.Where(r => r.Type == resource.Type).Sum(r => r.Count))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasResources(String resource)
        {
            return HasResources(new List<ResourceAmount>() { new ResourceAmount(resource) });
        }

        public bool RemoveResources(ResourceAmount resources, Vector3 position, Stockpile stock, bool createItems = true)
        {
            if (!stock.Resources.HasResource(resources))
            {
                return false;
            }

            List<Vector3> positions = new List<Vector3>();
            var resourceType = ResourceLibrary.GetResourceByName(resources.Type);
            int num = stock.Resources.RemoveMaxResources(resources, resources.Count);
            stock.HandleBoxes();
            foreach (var tag in resourceType.Tags)
            {
                if (CachedResourceTagCounts.ContainsKey(tag))
                {
                    CachedResourceTagCounts[tag] -= num;
                    Trace.Assert(CachedResourceTagCounts[tag] >= 0);
                }
            }
            if (stock.Boxes.Count > 0)
            {
                for (int i = 0; i < num; i++)
                {
                    positions.Add(stock.Boxes[stock.Boxes.Count - 1].LocalTransform.Translation);
                }
            }

            if (createItems)
            {
                foreach (Vector3 vec in positions)
                {
                    GameComponent newEntity =
                        EntityFactory.CreateEntity<GameComponent>(resources.Type + " Resource",
                            vec + MathFunctions.RandVector3Cube() * 0.5f);

                    TossMotion toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f),
                        2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                    newEntity.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                    newEntity.AnimationQueue.Add(toss);
                    toss.OnComplete += () => toss_OnComplete(newEntity);

                }
            }
            RecomputeCachedVoxelstate();
            return true;

        }

        public bool RemoveResources(List<ResourceAmount> resources, Vector3 position, bool createItems = true)
        {
            Dictionary<String, ResourceAmount> amounts = new Dictionary<String, ResourceAmount>();

            foreach (ResourceAmount resource in resources)
            {
                if (!amounts.ContainsKey(resource.Type))
                {
                    amounts.Add(resource.Type, new ResourceAmount(resource));
                }
                else
                {
                    amounts[resource.Type].Count += resource.Count;
                }
            }

            if (!HasResources(amounts.Values))
            {
                return false;
            }


            List<Stockpile> stockpilesCopy = new List<Stockpile>(Stockpiles.Where(s => resources.All(r => s.IsAllowed(r.Type))));
            stockpilesCopy.Sort((a, b) => CompareZones(a, b, position));


            foreach (ResourceAmount resource in resources)
            {
                int count = 0;
                List<Vector3> positions = new List<Vector3>();
                var resourceType = ResourceLibrary.GetResourceByName(resource.Type);
                foreach (Stockpile stock in stockpilesCopy)
                {
                    int num = stock.Resources.RemoveMaxResources(resource, resource.Count - count);
                    stock.HandleBoxes();
                    foreach(var tag in resourceType.Tags)
                    {
                        if (CachedResourceTagCounts.ContainsKey(tag))
                        {
                            CachedResourceTagCounts[tag] -= num;
                            Trace.Assert(CachedResourceTagCounts[tag] >= 0);
                        }
                    }
                    if (stock.Boxes.Count > 0)
                    {
                        for (int i = 0; i < num; i++)
                        {
                            positions.Add(stock.Boxes[stock.Boxes.Count - 1].LocalTransform.Translation);
                        }
                    }

                    count += num;

                    if (count >= resource.Count)
                    {
                        break;
                    }

                }

                if (createItems)
                {
                    foreach (Vector3 vec in positions)
                    {
                        GameComponent newEntity =
                            EntityFactory.CreateEntity<GameComponent>(resource.Type + " Resource",
                                vec + MathFunctions.RandVector3Cube() * 0.5f);

                        TossMotion toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f),
                            2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                        newEntity.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                        newEntity.AnimationQueue.Add(toss);
                        toss.OnComplete += () => toss_OnComplete(newEntity);

                    }
                }

            }
            RecomputeCachedVoxelstate();
            return true;
        }

        void toss_OnComplete(GameComponent entity)
        {
            entity.Die();

        }

        public DateTime Hire(Applicant currentApplicant, int delay)
        {
            var startDate = World.Time.CurrentDate;
            if (NewArrivals.Count > 0)
                startDate = NewArrivals.Last().ArrivalTime;

            NewArrivals.Add(new ApplicantArrival()
            {
                Applicant = currentApplicant,
                ArrivalTime = startDate+ new TimeSpan(0, delay, 0, 0, 0)
            });

            AddMoney(-(decimal)(currentApplicant.Level.Pay * 4));
            return NewArrivals.Last().ArrivalTime;
        }

        public void HireImmediately(Applicant currentApplicant)
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomData.Name == "Balloon Port").ToList();
            Vector3 spawnLoc = World.Renderer.Camera.Position;
            if (rooms.Count > 0)
            {
                spawnLoc = rooms.First().GetBoundingBox().Center() + Vector3.UnitY * 15;
            }

            var dwarfPhysics = DwarfFactory.GenerateDwarf(
                    spawnLoc,
                    World.ComponentManager, currentApplicant.ClassName, currentApplicant.LevelIndex, currentApplicant.Gender, currentApplicant.RandomSeed);
            World.ComponentManager.RootComponent.AddChild(dwarfPhysics);
            var newMinion = dwarfPhysics.EnumerateAll().OfType<Dwarf>().FirstOrDefault();
            global::System.Diagnostics.Debug.Assert(newMinion != null);

            newMinion.Stats.AllowedTasks = currentApplicant.Class.Actions;
            newMinion.Stats.LevelIndex = currentApplicant.LevelIndex - 1;
            newMinion.Stats.LevelUp(newMinion);
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

        public GameComponent DispatchBalloon()
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomData.Name == "Balloon Port").ToList();

            if (rooms.Count == 0)
            {
                return null;
            }

            Vector3 pos = rooms.First().GetBoundingBox().Center();
            return Balloon.CreateBalloon(pos + new Vector3(0, 1000, 0), pos + Vector3.UnitY * 15, World.ComponentManager, this);
        }

        public List<GameComponent> GenerateRandomSpawn(int numCreatures, Vector3 position)
        {
            if (Race.CreatureTypes.Count == 0)
            {
                return new List<GameComponent>();
            }

            List<GameComponent> toReturn = new List<GameComponent>();
            for (int i = 0; i < numCreatures; i++)
            {
                string creature = Race.CreatureTypes[MathFunctions.Random.Next(Race.CreatureTypes.Count)];
                Vector3 offset = MathFunctions.RandVector3Cube() * 2;

                var voxelUnder = VoxelHelpers.FindFirstVoxelBelowIncludingWater(new VoxelHandle(
                    World.ChunkManager, GlobalVoxelCoordinate.FromVector3(position + offset)));
                if (voxelUnder.IsValid)
                {
                    var body = EntityFactory.CreateEntity<GameComponent>(creature, voxelUnder.WorldPosition + new Vector3(0.5f, 1, 0.5f));
                    var ai = body.EnumerateAll().OfType<CreatureAI>().FirstOrDefault();

                    if (ai != null)
                    {
                        ai.Faction.Minions.Remove(ai);

                        Minions.Add(ai);
                        ai.Creature.Faction = this;
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
                amounts += amount.Count;
            }

            return amounts;
        }

        public List<ResourceAmount> ListResourcesWithTag(Resource.ResourceTags tag, bool allowHeterogenous = true)
        {
            Dictionary<string, ResourceAmount> resources = ListResources();
            if (allowHeterogenous)
            {
                return (from pair in resources
                        where ResourceLibrary.GetResourceByName(pair.Value.Type).Tags.Contains(tag)
                        select pair.Value).ToList();
            }
            ResourceAmount maxAmount = null;
            foreach (var pair in resources)
            {
                var resource = ResourceLibrary.GetResourceByName(pair.Value.Type);
                if (!resource.Tags.Contains(tag)) continue;
                if (maxAmount == null || pair.Value.Count > maxAmount.Count)
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
        }

        public void AddMoney(DwarfBux money)
        {
            if (money == 0.0m)
                return;

                Economy.Funds += money;
        }

        public int CalculateSupervisionCap()
        {
            return Minions.Sum(c => c.Stats.CurrentClass.Managerial ? (int)c.Stats.Intelligence : 0) + 4;
        }

        public int CalculateSupervisedEmployees()
        {
            return Minions.Where(c => !c.Stats.CurrentClass.Managerial).Count() + NewArrivals.Where(c => !c.Applicant.Class.Managerial).Count();
        }

        public void PayEmployees()
        {
            DwarfBux total = 0;
            bool noMoney = false;
            foreach (CreatureAI creature in Minions)
            {
                if (creature.Stats.IsOverQualified)
                    creature.Creature.AddThought(Thought.ThoughtType.IsOverQualified);

                var thoughts = creature.Physics.GetComponent<DwarfThoughts>();

                if (thoughts != null)
                    thoughts.Thoughts.RemoveAll(thought => thought.Description.Contains("paid"));

                DwarfBux pay = creature.Stats.CurrentLevel.Pay;
                total += pay;

                if (total >= Economy.Funds)
                {
                    if (!noMoney)
                    {
                        World.MakeAnnouncement("We have no money!");
                        World.Tutorial("money");
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                    }
                    noMoney = true;
                }
                else
                {
                    creature.Creature.AddThought(Thought.ThoughtType.GotPaid);
                }

                creature.AssignTask(new ActWrapperTask(new GetMoneyAct(creature, pay)) { AutoRetry = true, Name = "Get paid.", Priority = Task.PriorityType.High });
            }

            World.MakeAnnouncement(String.Format("We paid our employees {0} today.",
                total));
            SoundManager.PlaySound(ContentPaths.Audio.change, 0.15f);
            World.Tutorial("pay");
        }

        public bool AreAllEmployeesAsleep()
        {
            return (Minions.Count > 0) && Minions.All(minion => !minion.Active || ((!minion.Stats.Species.CanSleep || minion.Creature.Stats.IsAsleep) && !minion.IsDead));
        }
    }
}
