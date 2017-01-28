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

        public ResourcePack(Body parent) :
            base(parent)
        {
            Fixture sprite = new Fixture(Vector3.Zero, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 0) , this);
            Contents = new Inventory("Contents", this)
            {
                Resources = new ResourceContainer() {MaxResources = 999999},
                DropRate = 0.1f
            };
        }
    }
}
