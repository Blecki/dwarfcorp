using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Barrel : CraftedFixture
    {
        [EntityFactory("Barrel")]
        private static GameComponent __factory01(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Barrel(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }
        
        public Barrel()
        {

        }

        public Barrel(ComponentManager manager, Vector3 position, List<ResourceAmount> resources) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 0), new DwarfCorp.CraftDetails(manager, "Barrel", resources))
        {
            Name = "Barrel";
            Tags.Add("Barrel");
        }
    }
}
