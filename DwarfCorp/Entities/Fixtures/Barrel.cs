using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Barrel : CraftedFixture
    {
        [EntityFactory("Barrel")]
        private static GameComponent __factory01(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Barrel(Manager, Position, Data.GetData<Resource>("Resource", null));
        }
        
        public Barrel()
        {

        }

        public Barrel(ComponentManager manager, Vector3 position, Resource resource) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 0),
                new DwarfCorp.CraftDetails(manager, resource))
        {
            Name = "Barrel";
            Tags.Add("Barrel");
        }
    }
}
