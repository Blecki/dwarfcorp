using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    public class TrainingEquipment : Fixture
    {
        [JsonProperty] private Resource SourceResource;

        public TrainingEquipment()
        {
            DebugColor = Microsoft.Xna.Framework.Color.Turquoise;
        }

        public TrainingEquipment(String ResourceName, ComponentManager componentManager, Vector3 position, Resource Resource, SpriteSheet Sheet, Point Tile) :
            base(componentManager, position, Sheet, Tile)
        {
            DebugColor = Microsoft.Xna.Framework.Color.Turquoise;

            this.SourceResource = Resource;
            if (SourceResource == null)
            {
                SourceResource = new Resource(ResourceName);
                SourceResource.SetProperty<float>("hp", 5000.0f);
            }

            Name = ResourceName;
            Tags.Add(Name);
            Tags.Add("Train");

            if (GetRoot().GetComponent<Health>().HasValue(out var health))
            {
                health.MaxHealth = 5000;
                health.Hp = SourceResource.GetProperty<float>("hp", 5000.0f);
            }
        }

        public override void Die()
        {
            if (GetRoot().GetComponent<Health>().HasValue(out var health) && health.Hp > 0)
            {
                SourceResource.SetProperty<float>("hp", health.Hp);
                var bounds = this.GetRoot().GetBoundingBox();
                var pos = MathFunctions.RandVector3Box(bounds);
                Manager.RootComponent.AddChild(new ResourceEntity(Manager, SourceResource, pos));
            }

            base.Die();
        }

        public override string GetDescription()
        {
            return string.Format("{0} {1}/500", Name, GetRoot().GetComponent<Health>().HasValue(out var health) ? health.Hp.ToString() : "???");
        }
    }
}