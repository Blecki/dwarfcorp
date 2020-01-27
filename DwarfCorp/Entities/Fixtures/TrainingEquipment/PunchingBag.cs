using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class PunchingBag : TrainingEquipment
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
            base("Punching Bag", componentManager, position, Resource, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 5))
        {
        }
    }
}
