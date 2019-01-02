// EnemySensor.cs
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
using LibNoise.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// Component which fires when an enemy creature enters a box. Attached to other components.
    /// REQUIRES that the EnemySensor be attached to a creature
    /// </summary>
    [JsonObject(IsReference = true)]
    public class EnemySensor : Body
    {
        public delegate void EnemySensed(List<CreatureAI> enemies);

        public event EnemySensed OnEnemySensed;

        public Faction Allies { get; set; }
        public CreatureAI Creature { get; set; }
        public List<CreatureAI> Enemies { get; set; }
        public Timer SenseTimer { get; set; }
        public float SenseRadius { get; set; }
        public bool DetectCloaked { get; set; }

        public EnemySensor() : base()
        {
            UpdateRate = 10;
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            SenseTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
            SenseRadius = 15 * 15;
            CollisionType = CollisionType.None;
        }

        public EnemySensor(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            UpdateRate = 10;
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            Tags.Add("Sensor");
            SenseTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
            SenseRadius = 15 * 15;
            CollisionType = CollisionType.None;
        }


        public void Sense()
        {
            if (!Active)
                return;

            if (World.InitialEmbark.Difficulty == 0)
                return;

            if (Allies == null && Creature != null)
            {
                Allies = Creature.Faction;
            }

            List<CreatureAI> sensed = new List<CreatureAI>();

            VoxelHandle currentVoxel = new VoxelHandle(World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(Position));
            if (!(currentVoxel.IsValid && currentVoxel.IsEmpty))
            {
                return;
            }

            foreach (var body in Manager.World.EnumerateIntersectingObjects(BoundingBox, CollisionType.Both))
            {
                Flammable flames = body.GetRoot().GetComponent<Flammable>();
                if (flames != null && flames.Heat > flames.Flashpoint && body != GetRoot())
                {
                    var ai = GetRoot().GetComponent<CreatureAI>();
                    if (ai != null)
                    {
                        var task = new FleeEntityTask(body, 5)
                        {
                            Priority = Task.PriorityType.Urgent,
                            AutoRetry = false,
                            ReassignOnDeath = false
                        };
                        if (!ai.HasTaskWithName(task))
                        {
                            ai.AssignTask(task);
                        }
                    }
                }
                CreatureAI minion = body.GetRoot().GetComponent<CreatureAI>();
                if (minion == null)
                    continue;
                Faction faction = minion.Faction;
                
                if (World.Diplomacy.GetPolitics(Allies, faction).GetCurrentRelationship() !=
                    Relationship.Hateful) continue;

                if (!minion.Active) continue;

                if (!DetectCloaked && minion.Creature.IsCloaked)
                    continue;
                else if (DetectCloaked && minion.Creature.IsCloaked)
                    minion.Creature.IsCloaked = false;

                float dist = (minion.Position - GlobalTransform.Translation).LengthSquared();
                
                if (dist < SenseRadius && !VoxelHelpers.DoesRayHitSolidVoxel(
                    Manager.World.ChunkManager.ChunkData, Position, minion.Position))
                {
                    sensed.Add(minion);
                }
            }


            if (sensed.Count > 0)
            {
                OnEnemySensed.Invoke(sensed);
            }
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            SenseTimer.Update(gameTime);
            
            if (SenseTimer.HasTriggered)
            {
                Sense();
            }
            Enemies.RemoveAll(ai => ai.IsDead);
        }

        private void EnemySensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            Enemies = enemies;
        }

    }

}