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
        public String ParentFactionName = "";
        [JsonIgnore] public OverworldFaction ParentFaction;

        public DwarfBux TradeMoney { get; set; }
        public Point Center { get; set; }
        public int TerritorySize { get; set; }
        public Company Economy { get; set; }
        public List<TradeEnvoy> TradeEnvoys = new List<TradeEnvoy>();
        public List<WarParty> WarParties = new List<WarParty>();
        public List<GameComponent> OwnedObjects = new List<GameComponent>();
        public List<CreatureAI> Minions = new List<CreatureAI>();
        public Timer HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);
        public DesignationSet Designations = new DesignationSet(); // Todo: Still want to get this out of faction.
        public Dictionary<ulong, VoxelHandle> GuardedVoxels = new Dictionary<ulong, VoxelHandle>();
        public bool ClaimsColony = false;
        public bool IsMotherland = false;
        public float DistanceToCapital = 0.0f;
        public List<Creature> Threats = new List<Creature>();
        public bool InteractiveFaction = false;

        [JsonIgnore] public Race Race => Library.GetRace(ParentFaction.Race);
        [JsonIgnore] public WorldManager World;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = ctx.Context as WorldManager;
            ParentFaction = World.Settings.Natives.FirstOrDefault(n => n.Name == ParentFactionName);

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

            TradeMoney = 0.0m;
            Center = new Point(descriptor.CenterX, descriptor.CenterY);
        }

        public static List<CreatureAI> FilterMinionsWithCapability(List<CreatureAI> minions, Task.TaskCategory action)
        {
            return minions.Where(creature => creature.Stats.IsTaskAllowed(action)).ToList();
        }

        public void Update(DwarfTime time)
        {
            Minions.RemoveAll(m => m.IsDead);

            Designations.CleanupDesignations();

            if (HandleThreatsTimer == null)
                HandleThreatsTimer = new Timer(1.0f, false, Timer.TimerMode.Real);

            HandleThreatsTimer.Update(time);
            if (HandleThreatsTimer.HasTriggered)
                HandleThreats();

            if (World.ComponentManager.NumComponents() > 0)
                OwnedObjects.RemoveAll(obj => obj.IsDead || obj.Parent == null || !obj.Manager.HasComponent(obj.GlobalID));
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

        public void AddMinion(CreatureAI minion)
        {
            Minions.Add(minion);
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
        public void AddMoney(DwarfBux money)
        {
            if (money == 0.0m)
                return;

            Economy.Funds += money;
        }
    }
}
