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
    public class Anvil : CraftedFixture
    {
        public Anvil()
        {
            Name = "Anvil";
            Tags.Add("Anvil");
        }

        public Anvil(ComponentManager manager, Vector3 position, List<ResourceAmount> resources) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 3), new DwarfCorp.CraftDetails(manager, "Anvil", resources))
        {
            Name = "Anvil";
            Tags.Add("Anvil");
        }
    }
}
