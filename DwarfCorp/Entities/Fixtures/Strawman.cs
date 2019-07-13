using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class Strawman : CraftedFixture
    {
        [EntityFactory("Strawman")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Strawman(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }

        public Strawman()
        {

        }

        public Strawman(ComponentManager manager, Vector3 position, List<ResourceAmount> Resources) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 5), new DwarfCorp.CraftDetails(manager, "Strawman", Resources))
        {
            Name = "Strawman";
            Tags.Add("Strawman");
            Tags.Add("Train");

            if (GetRoot().GetComponent<Health>().HasValue(out var health))
            {
                health.MaxHealth = 500;
                health.Hp = 500;
            }
        }
    }
}
