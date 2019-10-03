using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class PunchingBag : CraftedFixture
    {
        [EntityFactory("Punching Bag")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new PunchingBag(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public PunchingBag()
        {

        }

        public PunchingBag(ComponentManager componentManager, Vector3 position, Resource Resource) :
            base(componentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 5), new CraftDetails(componentManager, Resource))
        {
            Name = "Punching Bag";
            Tags.Add("Punching  Bag");
            Tags.Add("Train");

            if (GetRoot().GetComponent<Health>().HasValue(out var health))
            {
                health.MaxHealth = 500;
                health.Hp = 500;
            }
        }
    }
}
