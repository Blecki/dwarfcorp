using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Anvil : Fixture
    {
        [EntityFactory("Anvil")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            // Todo: Can probably eliminate Anvil class entirely!
            return new Anvil(Manager, Position);
        }

        public Anvil()
        {
            Name = "Anvil";
            Tags.Add("Anvil");
        }

        public Anvil(ComponentManager manager, Vector3 position) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 3))
        {
            Name = "Anvil";
            Tags.Add("Anvil");
        }
    }
}
