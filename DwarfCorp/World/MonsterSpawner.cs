using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class MonsterSpawner
    {
        public struct SpawnEvent
        {
            public Faction SpawnFaction;
            public Faction TargetFaction;
            public Vector3 WorldLocation;
            public int NumCreatures;
            public bool Attack;
        }

        public List<Faction> SpawnFactions = new List<Faction>();
 
        public WorldManager World { get; set; }
        public Timer MigrationTimer = new Timer(120, false);

        public MonsterSpawner(WorldManager world)
        {
            World = world;
            SpawnFactions = new List<Faction>();
        }

        public void Update(DwarfTime t)
        {
            MigrationTimer.Update(t);
            if (MigrationTimer.HasTriggered)
            {
                CreateMigration();
            }
        }

        public void CreateMigration()
        {
            int tries = 0;
            float padding = 2.0f;

            while (tries < 10)
            {
                int side = MathFunctions.Random.Next(4);
                BoundingBox bounds = World.ChunkManager.Bounds;
                Vector3 pos = Vector3.Zero;
                switch (side)
                {
                    case 0:
                        pos = new Vector3(bounds.Min.X + padding, bounds.Max.Y - padding, MathFunctions.Rand(bounds.Min.Z + padding, bounds.Max.Z - padding));
                        break;
                    case 1:
                        pos = new Vector3(bounds.Max.X - padding, bounds.Max.Y - padding, MathFunctions.Rand(bounds.Min.Z + padding, bounds.Max.Z - padding));
                        break;
                    case 2:
                        pos = new Vector3(MathFunctions.Rand(bounds.Min.X + padding, bounds.Max.X - padding), bounds.Max.Y - padding, bounds.Min.Z + padding);
                        break;
                    case 3:
                        pos = new Vector3(MathFunctions.Rand(bounds.Min.X + padding, bounds.Max.X - padding), bounds.Max.Y - padding, bounds.Max.Z - padding);
                        break;
                }

                var biome = World.Overworld.Map.GetBiomeAt(pos, World.Overworld.InstanceSettings.Origin);
                if (biome.Fauna.Count == 0)
                {
                    tries++;
                    continue;
                }

                var randomFauna = Datastructures.SelectRandom(biome.Fauna);
                var testCreature = EntityFactory.CreateEntity<GameComponent>(randomFauna.Name, Vector3.Zero);

                if (testCreature == null)
                {
                    tries++;
                    continue;
                }

                var testCreatureAI = testCreature.GetRoot().GetComponent<CreatureAI>();
                if (testCreatureAI == null || testCreatureAI.Movement.IsSessile)
                {
                    testCreature.GetRoot().Delete();
                    tries++;
                    continue;
                }

                var count = World.GetSpeciesPopulation(testCreatureAI.Stats.CurrentClass);

                testCreature.GetRoot().Delete();

                if (count < 30)
                {
                    var randomNum = Math.Min(MathFunctions.RandInt(1, 3), 30 - count);
                    for (int i = 0; i < randomNum; i++)
                    {
                        var randompos = MathFunctions.Clamp(pos + MathFunctions.RandVector3Cube() * 2, World.ChunkManager.Bounds);
                        var vox = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(pos)));
                        if (!vox.IsValid)
                            continue;
                        EntityFactory.CreateEntity<GameComponent>(randomFauna.Name, vox.GetBoundingBox().Center() + Vector3.Up * 1.5f);
                    }
                }
                break;
            }
        }

        public static Vector3 GetRandomWorldEdge(WorldManager world)
        {
            float padding = 2.0f;
            int side = MathFunctions.Random.Next(4);
            BoundingBox bounds = world.ChunkManager.Bounds;
            Vector3 pos = Vector3.Zero;
            switch (side)
            {
                case 0:
                    pos = new Vector3(bounds.Min.X + padding, bounds.Max.Y - padding, MathFunctions.Rand(bounds.Min.Z + padding, bounds.Max.Z - padding));
                    break;
                case 1:
                    pos = new Vector3(bounds.Max.X - padding, bounds.Max.Y - padding, MathFunctions.Rand(bounds.Min.Z + padding, bounds.Max.Z - padding));
                    break;
                case 2:
                    pos = new Vector3(MathFunctions.Rand(bounds.Min.X + padding, bounds.Max.X - padding), bounds.Max.Y - padding, bounds.Min.Z + padding);
                    break;
                case 3:
                    pos = new Vector3(MathFunctions.Rand(bounds.Min.X + padding, bounds.Max.X - padding), bounds.Max.Y - padding, bounds.Max.Z - padding);
                    break;
            }
            return pos;
        }

        public SpawnEvent GenerateSpawnEvent(Faction spawnFaction, Faction targetFaction, int num, bool attack=true)
        {
            Vector3 pos = GetRandomWorldEdge(World);
            return new SpawnEvent()
            {
                NumCreatures = num,
                SpawnFaction = spawnFaction,
                TargetFaction = targetFaction,
                WorldLocation = pos,
                Attack = attack
            };
        }

        public List<CreatureAI> Spawn(SpawnEvent spawnEvent)
        {
            List<GameComponent> bodies = 
            spawnEvent.SpawnFaction.GenerateRandomSpawn(spawnEvent.NumCreatures, spawnEvent.WorldLocation);
            List<CreatureAI> toReturn = new List<CreatureAI>();
            foreach (GameComponent body in bodies)
            {
                foreach (CreatureAI creature in body.EnumerateAll().OfType<CreatureAI>().ToList())
                {
                   
                    if (spawnEvent.Attack)
                    {
                        CreatureAI enemyMinion = spawnEvent.TargetFaction.GetNearestMinion(creature.Position);
                        if (enemyMinion != null)
                        {
                            creature.AssignTask(new KillEntityTask(enemyMinion.Physics, KillEntityTask.KillType.Auto));
                        }
                    }
                    toReturn.Add(creature);
                }
            }

            return toReturn;
        }
    }
}
