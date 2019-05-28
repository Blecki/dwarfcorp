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
    public class Faction // Todo: Need to trim and refactor, see if can be split into normal faction / player faction.
    {
        public OverworldFaction ParentFaction; // Todo: To fix load, has to save name and reclaim from Overworld when deserialized.

        public DwarfBux TradeMoney { get; set; }
        public Point Center { get; set; }
        public int TerritorySize { get; set; }
        public Company Economy { get; set; }
        public List<TradeEnvoy> TradeEnvoys { get; set; }
        public List<WarParty> WarParties { get; set; }
        public List<GameComponent> OwnedObjects { get; set; }
        public List<CreatureAI> Minions { get; set; }
        public Timer HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
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
                return Library.GetRace(_race);
            }
            set
            {
                _race = value.Name;
            }
        }

        public List<Creature> Threats = new List<Creature>();

        public List<CreatureAI> SelectedMinions { get; set; }
        public bool InteractiveFaction = false;

        [JsonIgnore]
        public WorldManager World { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ctx.Context as WorldManager;
            Threats.RemoveAll(threat => threat == null || threat.IsDead);
            Minions.RemoveAll(minion => minion == null || minion.IsDead);
        }

        public Faction()
        {
        }

        public Faction(WorldManager World, OverworldFaction descriptor)
        {
            this.World = World;
            ParentFaction = descriptor;

            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            TradeEnvoys = new List<TradeEnvoy>();
            WarParties = new List<WarParty>();
            OwnedObjects = new List<GameComponent>();
            TradeMoney = 0.0m;
            Race = Library.GetRace(descriptor.Race);
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

            Designations.CleanupDesignations();

           

            if (HandleThreatsTimer == null)
                HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);

            HandleThreatsTimer.Update(time);
            if (HandleThreatsTimer.HasTriggered)
                HandleThreats();

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

        public IEnumerable<Zone> EnumerateZones() // Todo: Belongs to world manager??
        {
            if (World.RoomBuilder != null) 
                foreach (var room in World.RoomBuilder.Zones) 
                    yield return room;
            yield break;
        }

        public bool AddResources(ResourceAmount resources)
        {
            var amount = new ResourceAmount(resources.Type, resources.Count);
            var resource = ResourceLibrary.GetResourceByName(amount.Type);
            foreach (Stockpile stockpile in EnumerateZones().Where(s => s is Stockpile && (s as Stockpile).IsAllowed(resources.Type)))
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


        public List<Zone> GetIntersectingRooms(BoundingBox v)
        {
            return EnumerateZones().Where(room => room.Intersects(v)).ToList();
        }

        public bool HasFreeStockpile()
        {
            return EnumerateZones().Any(s => s.IsBuilt && !s.IsFull());
        }

        public bool HasFreeStockpile(ResourceAmount toPut)
        {
            return EnumerateZones().Any(s => s.IsBuilt && !s.IsFull() && s is Stockpile && (s as Stockpile).IsAllowed(toPut.Type));
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
                return 0;
            else
            {
                float costA = (pos - a.GetBoundingBox().Center()).LengthSquared();
                float costB = (pos - b.GetBoundingBox().Center()).LengthSquared();

                if (costA < costB)
                    return -1;
                else
                    return 1;
            }
        }

        public Dictionary<string, Pair<ResourceAmount>> ListResourcesInStockpilesPlusMinions()
        {
            var stocks = ListResources();
            var toReturn = new Dictionary<string, Pair<ResourceAmount>>();

            foreach (var pair in stocks)
                toReturn[pair.Key] = new Pair<ResourceAmount>(pair.Value, new ResourceAmount(pair.Value.Type, 0));

            foreach (var creature in Minions)
            {
                var inventory = creature.Creature.Inventory;
                foreach (var i in inventory.Resources)
                {
                    var resource = i.Resource;
                    if (toReturn.ContainsKey(resource))
                        toReturn[resource].Second.Count += 1;
                    else
                        toReturn[resource] = new Pair<ResourceAmount>(new ResourceAmount(resource, 0), new ResourceAmount(resource));
                }
            }

            return toReturn;
        }

        public Dictionary<string, ResourceAmount> ListResources()
        {
            var toReturn = new Dictionary<string, ResourceAmount>();

            foreach (var stockpile in EnumerateZones())
            {
                foreach (var resource in stockpile.Resources.Enumerate())
                {
                    if (resource.Count == 0)
                        continue;

                    if (toReturn.ContainsKey(resource.Type))
                        toReturn[resource.Type].Count += resource.Count;
                    else
                        toReturn[resource.Type] = new ResourceAmount(resource);
                }
            }

            return toReturn;
        }

        public void RecomputeCachedVoxelstate()
        {
            foreach (var type in Library.EnumerateVoxelTypes())
            {
                bool nospecialRequried = type.BuildRequirements.Count == 0;
                CachedCanBuildVoxel[type.Name] = type.IsBuildable && ((nospecialRequried && World.HasResources(type.ResourceToRelease)) || (!nospecialRequried && HasResourcesCached(type.BuildRequirements)));
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
                    return false;

                if (CachedResourceTagCounts[resource] == 0)
                    return false;
            }

            return true;
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
            var rooms = EnumerateZones().Where(room => room.Type.Name == "Balloon Port").ToList();
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
            Debug.Assert(newMinion != null);

            newMinion.Stats.AllowedTasks = currentApplicant.Class.Actions;
            newMinion.Stats.LevelIndex = currentApplicant.LevelIndex - 1;
            newMinion.Stats.LevelUp(newMinion);
            newMinion.Stats.FullName = currentApplicant.Name;
            newMinion.AI.AddMoney(currentApplicant.Level.Pay * 4m);
            newMinion.AI.Biography = currentApplicant.Biography;

            World.MakeAnnouncement(
                new Gui.Widgets.QueuedAnnouncement
                {
                    Text = String.Format("{0} was hired as a {1}.", currentApplicant.Name, currentApplicant.Level.Name),
                    ClickAction = (gui, sender) => newMinion.AI.ZoomToMe()
                });

            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
        }

        public void FireEmployee(CreatureAI Employee)
        {
            Minions.Remove(Employee);
            SelectedMinions.Remove(Employee);
            AddMoney(-(decimal)(Employee.Stats.CurrentLevel.Pay * 4));
        }

        public GameComponent DispatchBalloon()
        {
            var rooms = EnumerateZones().Where(room => room.Type.Name == "Balloon Port").ToList();

            if (rooms.Count == 0)
                return null;

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


        public Zone GetNearestRoom(Vector3 position)
        {
            Zone desiredRoom = null;
            float nearestDistance = float.MaxValue;

            foreach (var room in EnumerateZones())
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
