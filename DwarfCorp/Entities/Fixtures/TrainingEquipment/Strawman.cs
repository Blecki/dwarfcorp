using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class Strawman : TrainingEquipment
    {
        [EntityFactory("Strawman")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Strawman(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Strawman()
        {

        }

        public Strawman(ComponentManager manager, Vector3 position, Resource Resource) :
            base("Strawman", manager, position, Resource, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 5))
        {
        }
    }
}
