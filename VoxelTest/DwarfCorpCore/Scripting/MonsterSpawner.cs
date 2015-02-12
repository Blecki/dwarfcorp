using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class MonsterSpawner
    {
        public struct SpawnEvent
        {
            public Faction SpawnFaction;
            public Faction TargetFaction;
            public Vector3 WorldLocation;
            public int NumCreatures;
        }

        public List<Faction> SpawnFactions = new List<Faction>();
        public int LastSpawnHour = 0;
        public int SpawnRate = 4;

        public MonsterSpawner()
        {
            SpawnFactions = new List<Faction>()
            {
                PlayState.ComponentManager.Factions.Factions["Goblins"],
                PlayState.ComponentManager.Factions.Factions["Undead"],
                PlayState.ComponentManager.Factions.Factions["Elf"]
            };
        }

        public void Update(DwarfTime t)
        {
            bool shouldSpawn = PlayState.Time.IsNight() && Math.Abs(PlayState.Time.CurrentDate.TimeOfDay.Hours - LastSpawnHour) > SpawnRate;

            if (shouldSpawn)
            {
                int numToSpawn = PlayState.Random.Next(5) + 1;
                Spawn(GenerateSpawnEvent(SpawnFactions[PlayState.Random.Next(SpawnFactions.Count)],
                    PlayState.ComponentManager.Factions.Factions["Player"], numToSpawn));
                LastSpawnHour = PlayState.Time.CurrentDate.TimeOfDay.Hours;
            }
        }

        public SpawnEvent GenerateSpawnEvent(Faction spawnFaction, Faction targetFaction, int num)
        {
            int side = PlayState.Random.Next(4);
            BoundingBox bounds = PlayState.ChunkManager.Bounds;
            Vector3 pos = Vector3.Zero;
            switch (side)
            {
                case 0:
                    pos = new Vector3(bounds.Min.X + 1, bounds.Max.Y - 1, MathFunctions.Rand(bounds.Min.Z, bounds.Max.Z));
                    break;
                case 1:
                    pos = new Vector3(bounds.Max.X - 1, bounds.Max.Y - 1, MathFunctions.Rand(bounds.Min.Z, bounds.Max.Z));
                    break;
                case 2:
                    pos = new Vector3(MathFunctions.Rand(bounds.Min.X, bounds.Max.X), bounds.Max.Y - 1, bounds.Min.Z + 1);
                    break;
                case 3:
                    pos = new Vector3(MathFunctions.Rand(bounds.Min.X, bounds.Max.X), bounds.Max.Y - 1, bounds.Max.Z - 1);
                    break;
            }

            return new SpawnEvent()
            {
                NumCreatures = num,
                SpawnFaction = spawnFaction,
                TargetFaction = targetFaction,
                WorldLocation = pos
            };
        }

        public void Spawn(SpawnEvent spawnEvent)
        {
            List<Body> bodies = 
            spawnEvent.SpawnFaction.GenerateRandomSpawn(spawnEvent.NumCreatures, spawnEvent.WorldLocation);

            foreach (Body body in bodies)
            {
                List<CreatureAI> creatures = body.GetChildrenOfTypeRecursive<CreatureAI>();
                foreach (CreatureAI creature in creatures)
                {
                    CreatureAI enemyMinion = spawnEvent.TargetFaction.GetNearestMinion(creature.Position);
                    if (enemyMinion != null)
                    {
                        creature.Tasks.Add(new KillEntityTask(enemyMinion.Physics));
                    }
                }
            }
        }
    }
}
