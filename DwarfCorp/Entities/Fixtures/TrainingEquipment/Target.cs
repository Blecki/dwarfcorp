using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Target : TrainingEquipment
    {
        [EntityFactory("Target")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Target(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Target()
        {

        }

        public Target(ComponentManager componentManager, Vector3 position, Resource Resource) :
            base("Target", componentManager, position, Resource, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 5))
        {
        }
    }
}
