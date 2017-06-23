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
    public class Barrel : Fixture
    {
        public Barrel()
        {
            Name = "Barrel";
            Tags.Add("Barrel");
        }

        public Barrel(ComponentManager manager, Vector3 position) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 0))
        {
            Name = "Barrel";
            Tags.Add("Barrel");
        }
    }
}
