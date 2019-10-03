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
            return new WoodenDoor(Manager, Position, Manager.World.PlayerFaction, Data.GetData<Resource>("Resource", null));
        }

        public WoodenDoor()
        {
        }

        public WoodenDoor(ComponentManager manager, Vector3 position, Faction team, Resource Resource) :
            base(manager, position, team, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3,1), Resource, "Wooden Door", 30.0f)
        {
            Name = "Wooden Door";
        }
    }
}
