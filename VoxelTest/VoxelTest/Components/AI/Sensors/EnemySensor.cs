using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class EnemySensor : Sensor
    {
        public delegate void EnemySensed(List<CreatureAI> enemies);

        public event EnemySensed OnEnemySensed;

        public CreatureAI Creature { get; set; }

        public EnemySensor() : base()
        {
            OnSensed += EnemySensor_OnSensed;
            OnEnemySensed += EnemySensor_OnEnemySensed;
        }

        public EnemySensor(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            OnSensed += EnemySensor_OnSensed;
            OnEnemySensed += EnemySensor_OnEnemySensed;
            Tags.Add("Sensor");
        }

        private void EnemySensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            ;
        }

        private void EnemySensor_OnSensed(List<Body> sensed)
        {
            List<CreatureAI> creatures = (from c in sensed.OfType<Physics>() from child in c.GetChildrenOfTypeRecursive<CreatureAI>() where child != Creature && Alliance.GetRelationship(Creature.Creature.Allies, child.Creature.Allies) == Relationship.Hates select child).ToList();
            OnEnemySensed.Invoke(creatures);
        }
    }

}