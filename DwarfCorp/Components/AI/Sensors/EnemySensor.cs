﻿using System;
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
    public class EnemySensor : GameComponent
    {
        private const float SenseTime = 5.0f;

        public delegate void EnemySensed(List<CreatureAI> enemies);

        public event EnemySensed OnEnemySensed;

        public Faction Allies { get; set; }
        public CreatureAI Creature { get; set; }
        public List<CreatureAI> Enemies { get; set; }
        public Timer SenseTimer { get; set; }
        public bool DetectCloaked { get; set; }

        public EnemySensor() : base()
        {
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            SenseTimer = new Timer(SenseTime, false, Timer.TimerMode.Game);
            CollisionType = CollisionType.None;
        }

        public EnemySensor(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            Tags.Add("Sensor");
            SenseTimer = new Timer(SenseTime, false, Timer.TimerMode.Game);
            CollisionType = CollisionType.None;
        }


        public void Sense()
        {
            if (Name == "turret-sensor")
            {
                var x = 5;
            }

            if (!Active) return;

            if (Creature != null)
                Allies = Creature.Faction;

            // Don't sense enemies if we're inside the ground??
            var currentVoxel = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Position));
            if (!(currentVoxel.IsValid && currentVoxel.IsEmpty))
                return;

            var sensed = new List<CreatureAI>();

            var myRoot = GetRoot();

            foreach (var body in Manager.World.EnumerateIntersectingRootObjects(BoundingBox, b => !Object.ReferenceEquals(b, myRoot)))
            {
                if (body.GetComponent<Flammable>().HasValue(out var flames) && flames.IsOnFire)
                {
                    if (GetRoot().GetComponent<CreatureAI>().HasValue(out var myAI))
                    {
                        var task = new FleeEntityTask(body, 5)
                        {
                            Priority = TaskPriority.Urgent,
                            AutoRetry = false,
                            ReassignOnDeath = false
                        };

                        if (!myAI.HasTaskWithName(task))
                            myAI.AssignTask(task);

                        continue;
                    }
                }

                if (body.GetComponent<CreatureAI>().HasValue(out var minion))
                {
                    if (!minion.Active)
                        continue;

                    if (!DetectCloaked && minion.Creature.IsCloaked)
                        continue;

                    else if (DetectCloaked && minion.Creature.IsCloaked)
                        minion.Creature.IsCloaked = false;

                    if (World.Overworld.GetPolitics(Allies.ParentFaction, minion.Faction.ParentFaction).GetCurrentRelationship() != Relationship.Hateful)
                        continue;

                    if (!VoxelHelpers.DoesRayHitSolidVoxel(Manager.World.ChunkManager, Position, minion.Position))
                        sensed.Add(minion);
                }
            }

            if (sensed != null && sensed.Count > 0 && OnEnemySensed != null)
                OnEnemySensed.Invoke(sensed);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (World.Overworld.Difficulty.CombatModifier == 0) return; // Disable enemy sensors on peaceful difficulty.

            SenseTimer.Update(gameTime);

            if (SenseTimer.HasTriggered)
            {
                Enemies.Clear();
                try
                {
                    Sense();
                }
                catch (Exception e)
                {
                    Program.CaptureException(e);
                }
            }

            Enemies.RemoveAll(ai => ai.IsDead);
        }

        private void EnemySensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            Enemies = enemies;
        }
    }
}