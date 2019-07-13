using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Weights : CraftedFixture
    {
        [EntityFactory("Weights")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Weights(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }

        public Weights()
        {

        }

        public Weights(ComponentManager componentManager, Vector3 position, List<ResourceAmount> resources) :
            base(componentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 5), new DwarfCorp.CraftDetails(componentManager, "Weights", resources))
        {
            Name = "Weights";
            Tags.Add("Weights");
            Tags.Add("Train");

            if (GetRoot().GetComponent<Health>().HasValue(out var health))
            {
                health.MaxHealth = 500;
                health.Hp = 500;
            }
        }
    }
}
