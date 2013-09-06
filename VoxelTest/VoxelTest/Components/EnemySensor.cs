using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class EnemySensor : Sensor
    {
        public delegate void EnemySensed(List<CreatureAIComponent> enemies);
        public event EnemySensed OnEnemySensed;

        [JsonIgnore]
        public CreatureAIComponent Creature { get; set; }

        public EnemySensor(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            OnSensed += new Sense(EnemySensor_OnSensed);
            OnEnemySensed += new EnemySensed(EnemySensor_OnEnemySensed);
            Tags.Add("Sensor");
        }

        void EnemySensor_OnEnemySensed(List<CreatureAIComponent> enemies)
        {
            ;
        }

        void EnemySensor_OnSensed(List<LocatableComponent> sensed)
        {
            
            List<CreatureAIComponent> creatures = new List<CreatureAIComponent>();

            foreach (LocatableComponent c in sensed)
            {
                List<CreatureAIComponent> children = c.GetChildrendOfTypeRecursive<CreatureAIComponent>();

                foreach (CreatureAIComponent child in children)
                {
                    if (child != Creature && Alliance.GetRelationship(Creature.Creature.Allies, child.Creature.Allies) == Relationship.Hates)
                    {
                        creatures.Add(child);
                    }
                }
            }

            OnEnemySensed.Invoke(creatures);
             
        }
    }
}
