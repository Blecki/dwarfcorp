using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class WoodenDoor : Door
    {
        [EntityFactory("Wooden Door")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new WoodenDoor(Manager, Position, Manager.World.PlayerFaction, Data.GetData<List<ResourceAmount>>("Resources", null), "Wooden Door");
        }

        public WoodenDoor()
        {
        }

        public WoodenDoor(ComponentManager manager, Vector3 position, Faction team, List<ResourceAmount> resourceType, string craftType) :
            base(manager, position, team, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3,1), resourceType, craftType, 30.0f)
        {
            Name = "Wooden Door";
        }
    }
}
