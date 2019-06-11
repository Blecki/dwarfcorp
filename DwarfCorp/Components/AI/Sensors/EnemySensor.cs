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
    public class EnemySensor : GameComponent
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
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            SenseTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
            SenseRadius = 15 * 15;
            CollisionType = CollisionType.None;
        }

        public EnemySensor(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            Tags.Add("Sensor");
            SenseTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
            SenseRadius = 15 * 15;
            CollisionType = CollisionType.None;
        }


        public void Sense()
        {
            if (!Active) return;
            if (World.Overworld.Difficulty == 0) return; // Disable enemy sensors on peaceful difficulty.

            if (Creature != null)
                Allies = Creature.Faction;

            // Don't sense enemies if we're inside the ground??
            var currentVoxel = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Position));
            if (!(currentVoxel.IsValid && currentVoxel.IsEmpty))
                return;

            var sensed = new List<CreatureAI>();

            var myRoot = GetRoot();
            var myAI = GetRoot().GetComponent<CreatureAI>();
            if (myAI == null) return;

            foreach (var body in Manager.World.EnumerateIntersectingObjects(BoundingBox, b => !Object.ReferenceEquals(b, myRoot) && b.IsRoot()))
            {
                var flames = body.GetComponent<Flammable>();

                if (flames != null && flames.IsOnFire)
                {
                    var task = new FleeEntityTask(body, 5)
                    {
                        Priority = Task.PriorityType.Urgent,
                        AutoRetry = false,
                        ReassignOnDeath = false
                    };

                    if (!myAI.HasTaskWithName(task))
                        myAI.AssignTask(task);

                    continue;
                }

                var minion = body.GetComponent<CreatureAI>();
                if (minion == null || !minion.Active)
                    continue;

                if (!DetectCloaked && minion.Creature.IsCloaked)
                    continue;
                else if (DetectCloaked && minion.Creature.IsCloaked)
                    minion.Creature.IsCloaked = false;

                if (World.Diplomacy.GetPolitics(Allies, minion.Faction).GetCurrentRelationship() != Relationship.Hateful)
                    continue;

                float dist = (minion.Position - GlobalTransform.Translation).LengthSquared();

                if (dist < SenseRadius && !VoxelHelpers.DoesRayHitSolidVoxel(Manager.World.ChunkManager, Position, minion.Position))
                    sensed.Add(minion);
            }

            if (sensed.Count > 0)
                OnEnemySensed.Invoke(sensed);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            SenseTimer.Update(gameTime);
            
            if (SenseTimer.HasTriggered)
                Sense();
            Enemies.RemoveAll(ai => ai.IsDead);
        }

        private void EnemySensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            Enemies = enemies;
        }
    }
}