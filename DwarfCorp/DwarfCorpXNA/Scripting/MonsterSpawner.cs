// MonsterSpawner.cs
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
            public bool Attack;
        }

        public List<Faction> SpawnFactions = new List<Faction>();
 
        public WorldManager World { get; set; }
        public Timer MigrationTimer = new Timer(1200, false);

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

                var biome = Overworld.GetBiomeAt(pos);
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


                var count = Creature.GetNumSpecies(testCreatureAI.Creature.Species);

                testCreature.GetRoot().Delete();

                if (count < 30)
                {
                    var randomNum = Math.Min(MathFunctions.RandInt(1, 3), 30 - count);
                    for (int i = 0; i < randomNum; i++)
                    {
                        var randompos = MathFunctions.Clamp(pos + MathFunctions.RandVector3Cube() * 2, World.ChunkManager.Bounds);
                        var vox = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(pos)));
                        if (!vox.IsValid)
                            continue;
                        EntityFactory.CreateEntity<GameComponent>(randomFauna.Name, vox.GetBoundingBox().Center() + Vector3.Up * 1.5f);
                    }
                }
                break;
            }
        }

        public SpawnEvent GenerateSpawnEvent(Faction spawnFaction, Faction targetFaction, int num, bool attack=true)
        {
            float padding = 2.0f;
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
            List<Body> bodies = 
            spawnEvent.SpawnFaction.GenerateRandomSpawn(spawnEvent.NumCreatures, spawnEvent.WorldLocation);
            List<CreatureAI> toReturn = new List<CreatureAI>();
            foreach (Body body in bodies)
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
