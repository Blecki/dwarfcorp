using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{

    /// <summary>
    /// When an entity dies, this component releases other components (such as resources)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class DeathComponentSpawner : Body
    {
        public List<Body> Spawns { get; set; }
        public float ThrowSpeed { get; set; }

        public DeathComponentSpawner(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingExtents, Vector3 boundingBoxPos, List<Body> spawns) :
            base(manager, name, parent, localTransform, boundingExtents, boundingBoxPos, false)
        {
            Spawns = spawns;
            ThrowSpeed = 5.0f;
            AddToOctree = false;
        }

        public override void Die()
        {
            if(IsDead)
            {
                return;
            }

            foreach(Body locatable in Spawns)
            {
                locatable.SetVisibleRecursive(true);
                locatable.SetActiveRecursive(true);
                locatable.HasMoved = true;
                locatable.WasAddedToOctree = false;
                locatable.AddToOctree = true;

                var component = locatable as PhysicsComponent;
                if(component != null)
                {
                    Vector3 radialThrow = MathFunctions.RandVector3Cube() * ThrowSpeed;
                    component.Velocity += radialThrow;
                }

                Manager.AddComponent(locatable);
                Manager.RootComponent.AddChild(locatable);
            }

            base.Die();
        }
    }

}