using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
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

        public CreatureAI Creature { get; set; }
        public List<CreatureAI> Enemies { get; set; }
        public Timer SenseTimer { get; set; }
        public float SenseRadius { get; set; }

        public EnemySensor() : base()
        {
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            SenseTimer = new Timer(0.5f, false);
            SenseRadius = 15 * 15;
        }

        public EnemySensor(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Enemies = new List<CreatureAI>();
            OnEnemySensed += EnemySensor_OnEnemySensed;
            Tags.Add("Sensor");
            SenseTimer = new Timer(0.5f, false);
            SenseRadius = 15 * 15;
        }


        public void Sense()
        {
            List<CreatureAI> sensed = new List<CreatureAI>();
            List<CreatureAI> collide = new List<CreatureAI>();
            foreach (KeyValuePair<string, Faction> faction in PlayState.ComponentManager.Factions.Factions)
            {
                if (Alliance.GetRelationship(Creature.Creature.Allies, faction.Value.Alliance) == Relationship.Hates)
                {
                    foreach (CreatureAI minion in faction.Value.Minions)
                    {
                        float dist = (minion.Position - GlobalTransform.Translation).LengthSquared();

                        if (dist < SenseRadius)
                        {
                            sensed.Add(minion);
                        }

                        if (dist < 1.0f)
                        {
                            collide.Add(minion);
                        }
                    }
                }

            }

            if (sensed.Count > 0)
            {
                OnEnemySensed.Invoke(sensed);
            }

            foreach (CreatureAI minion in collide)
            {
                Vector3 diff = minion.Position - Creature.Position;
                diff.Normalize();
                minion.Physics.ApplyForce(diff * 10, Act.Dt);
                Creature.Physics.ApplyForce(diff * 10, Act.Dt);
            }
        }


        public override void Update(DwarfTime DwarfTime, ChunkManager chunks, Camera camera)
        {
            SenseTimer.Update(DwarfTime);
            
            if (SenseTimer.HasTriggered)
            {
                Sense();
            }
            Enemies.RemoveAll(ai => ai.IsDead);
            base.Update(DwarfTime, chunks, camera);
        }

        private void EnemySensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            Enemies = enemies;
        }

    }

}