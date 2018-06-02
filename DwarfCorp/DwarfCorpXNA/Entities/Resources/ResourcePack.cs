using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ResourcePack : Follower
    {
        public Inventory Contents { get; set; }

        public ResourcePack()
        {
            
        }

        public ResourcePack(ComponentManager Manager) :
            base(Manager)
        {
            AddChild(new SimpleSprite(Manager, "Trade Goods", Matrix.CreateTranslation(Vector3.Zero), new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 0)));
            Contents = AddChild(new Inventory(Manager, "Contents", BoundingBox.Extents(), LocalBoundingBoxOffset)
            {
                Resources = new List<Inventory.InventoryItem>(),
                DropRate = 0.1f
            }) as Inventory;
        }
    }
}
