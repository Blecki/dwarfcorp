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
        public int LastSpawnHour = 0;
        public int SpawnRate = 4;

        public MonsterSpawner()
        {
            SpawnFactions = new List<Faction>();
        }

        public void Update(DwarfTime t)
        {
            bool shouldSpawn = PlayState.Time.IsNight() && Math.Abs(PlayState.Time.CurrentDate.TimeOfDay.Hours - LastSpawnHour) > SpawnRate;

            if (shouldSpawn)
            {
                /*
                int numToSpawn = PlayState.Random.Next(5) + 1;
                Spawn(GenerateSpawnEvent(SpawnFactions[PlayState.Random.Next(SpawnFactions.Count)],
                    PlayState.ComponentManager.Factions.Factions["Player"], numToSpawn));
                LastSpawnHour = PlayState.Time.CurrentDate.TimeOfDay.Hours;
                 */
            }
        }

        public SpawnEvent GenerateSpawnEvent(Faction spawnFaction, Faction targetFaction, int num, bool attack=true)
        {
            float padding = 2.0f;
            int side = PlayState.Random.Next(4);
            BoundingBox bounds = PlayState.ChunkManager.Bounds;
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
                List<CreatureAI> creatures = body.GetChildrenOfTypeRecursive<CreatureAI>();
                foreach (CreatureAI creature in creatures)
                {
                   
                    if (spawnEvent.Attack)
                    {
                        CreatureAI enemyMinion = spawnEvent.TargetFaction.GetNearestMinion(creature.Position);
                        if (enemyMinion != null)
                        {
                            creature.Tasks.Add(new KillEntityTask(enemyMinion.Physics, KillEntityTask.KillType.Auto));
                        }
                    }
                    else
                    {
                        Room nearestRoom = spawnEvent.TargetFaction.GetNearestRoom(creature.Position);
                        if (nearestRoom != null)
                        {
                            creature.Tasks.Add(new ActWrapperTask(new GoToZoneAct(creature, nearestRoom)));
                        }
                    }
                    toReturn.Add(creature);
                }
            }

            return toReturn;
        }
    }
}
