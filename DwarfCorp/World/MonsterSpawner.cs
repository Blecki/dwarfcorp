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
        private Random Random = new Random();

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

                pos = MathFunctions.Clamp(pos, World.ChunkManager.Bounds);

                if (World.Overworld.Map.GetBiomeAt(pos, World.Overworld.InstanceSettings.Origin).HasValue(out var biome))
                {
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

                    if (testCreature.GetRoot().GetComponent<CreatureAI>().HasValue(out var testCreatureAI)
                        && !testCreatureAI.Movement.IsSessile
                        && World.CanSpawnWithoutExceedingSpeciesLimit(testCreatureAI.Stats.Species))
                    {
                        var vox = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(pos)));

                        if (!vox.IsValid)
                        {
                            testCreature.GetRoot().Delete();
                            tries++;
                            continue;
                        }

                        if (testCreature.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                            physics.LocalTransform = Matrix.CreateTranslation(vox.GetBoundingBox().Center() + Vector3.Up * 1.5f);
                    }
                    else
                    {
                        testCreature.GetRoot().Delete();
                        tries++;
                        continue;
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
            return new SpawnEvent()
            {
                NumCreatures = num,
                SpawnFaction = spawnFaction,
                TargetFaction = targetFaction,
                WorldLocation = GetRandomWorldEdge(World),
                Attack = attack
            };
        }

        public List<CreatureAI> Spawn(SpawnEvent spawnEvent)
        {
            var bodies = spawnEvent.SpawnFaction.GenerateRandomSpawn(spawnEvent.NumCreatures, spawnEvent.WorldLocation);
            var toReturn = new List<CreatureAI>();
            foreach (GameComponent body in bodies)
            {
                foreach (CreatureAI creature in body.EnumerateAll().OfType<CreatureAI>().ToList())
                {
                   
                    if (spawnEvent.Attack)
                    {
                        var enemyMinion = spawnEvent.TargetFaction.GetNearestMinion(creature.Position);
                        if (enemyMinion != null)
                            creature.AssignTask(new KillEntityTask(enemyMinion.Physics, KillEntityTask.KillType.Auto));
                    }
                    toReturn.Add(creature);
                }
            }

            return toReturn;
        }

        public void OnVoxelChanged(VoxelChangeEvent e)
        {
            if (e.Type == VoxelChangeEventType.Explored && e.Voxel.IsEmpty)
            {
                var below = VoxelHelpers.GetVoxelBelow(e.Voxel);
                if (below.IsValid && !below.IsEmpty && Library.GetBiome("Cave").HasValue(out BiomeData caveBiome) && Library.GetBiome("Hell").HasValue(out BiomeData hellBiome))
                {
                    var biome = (e.Voxel.Coordinate.Y <= 10) ? hellBiome : caveBiome;
                    if (e.Voxel.Coordinate.Y > 5 && Random.NextDouble() < 0.5f)
                        GenerateCaveFlora(below, biome);
                    if (e.Voxel.Coordinate.Y > 5 && Random.NextDouble() < 0.5f)
                        GenerateCaveFauna(below, biome);
                }
            }
        }

        private void GenerateCaveFlora(VoxelHandle CaveFloor, BiomeData Biome)
        {
            foreach (var floraType in Biome.Vegetation)
            {
                if (Random.NextDouble() > floraType.SpawnProbability)
                    continue;

                //if (Settings.NoiseGenerator.Noise(CaveFloor.Coordinate.X / floraType.ClumpSize, floraType.NoiseOffset, CaveFloor.Coordinate.Z / floraType.ClumpSize) < floraType.ClumpThreshold)
                //continue;

                var plantSize = MathFunctions.Rand() * floraType.SizeVariance + floraType.MeanSize;
                var lambdaFloraType = floraType;

                var blackboard = new Blackboard();
                blackboard.SetData("Scale", plantSize);

                EntityFactory.CreateEntity<GameComponent>(
                    lambdaFloraType.Name,
                    CaveFloor.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                    blackboard);

                break; // Don't risk spawning multiple plants in the same spot.
            }
        }

        private void GenerateCaveFauna(VoxelHandle CaveFloor, BiomeData Biome)
        {
            var spawnLikelihood = (World.Overworld.Difficulty.CombatModifier + 0.1f);

            foreach (var animalType in Biome.Fauna)
            {
                if (!(Random.NextDouble() < animalType.SpawnProbability * spawnLikelihood))
                    continue;

                var lambdaAnimalType = animalType;

                EntityFactory.CreateEntity<GameComponent>(lambdaAnimalType.Name, CaveFloor.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f));

                break; // Prevent spawning multiple animals in same spot.
            }
        }

    }
}
