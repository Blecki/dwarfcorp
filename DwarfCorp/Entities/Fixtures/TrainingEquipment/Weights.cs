using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class Weights : TrainingEquipment
    {
        [EntityFactory("Weights")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Weights(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Weights()
        {

        }

        public Weights(ComponentManager componentManager, Vector3 position, Resource Resource) :
            base("Weights", componentManager, position, Resource, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 5))
        {

        }
    }
}
