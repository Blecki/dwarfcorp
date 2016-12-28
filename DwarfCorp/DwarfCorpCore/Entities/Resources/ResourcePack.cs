using DwarfCorp;
using Microsoft.Xna.Framework;

namespace DwarfCorpCore
{
    public class ResourcePack : Follower
    {
        public ResourcePack()
        {
        }

        public ResourcePack(Body parent) :
            base(parent)
        {
            var sprite = new Fixture(Vector3.Zero,
                new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 0), this);
            Contents = new Inventory("Contents", this)
            {
                Resources = new ResourceContainer {MaxResources = 999999},
                DropRate = 0.1f
            };
        }

        public Inventory Contents { get; set; }
    }
}