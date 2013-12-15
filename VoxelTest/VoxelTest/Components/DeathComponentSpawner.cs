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

    [JsonObject(IsReference = true)]
    public class DeathComponentSpawner : LocatableComponent
    {
        public List<LocatableComponent> Spawns { get; set; }
        public float ThrowSpeed { get; set; }

        public DeathComponentSpawner(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingExtents, Vector3 boundingBoxPos, List<LocatableComponent> spawns) :
            base(manager, name, parent, localTransform, boundingExtents, boundingBoxPos, false)
        {
            Spawns = spawns;
            ThrowSpeed = 1.0f;
            AddToOctree = false;
        }

        public override void Die()
        {
            if(IsDead)
            {
                return;
            }

            foreach(LocatableComponent locatable in Spawns)
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