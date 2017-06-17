using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Anvil : Fixture
    {
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
