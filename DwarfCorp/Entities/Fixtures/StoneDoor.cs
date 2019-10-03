using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class StoneDoor : Door
    {
        [EntityFactory("Stone Door")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new StoneDoor(Manager, Position, Manager.World.PlayerFaction, Data.GetData<Resource>("Resource", null));
        }

        public StoneDoor()
        {
        }

        public StoneDoor(ComponentManager manager, Vector3 position, Faction team, Resource Resource) :
            base(manager, position, team, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 8), Resource, "Stone Door", 80.0f)
        {
            Name = "Stone Door";
        }
    }
}
