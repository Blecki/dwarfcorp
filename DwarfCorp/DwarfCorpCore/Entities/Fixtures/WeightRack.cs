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
    public class WeightRack : Fixture
    {
        public WeightRack()
        {

        }

        public WeightRack(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 5), PlayState.ComponentManager.RootComponent)
        {
            Name = "WeightRack";
            Tags.Add("WeightRack");
            Tags.Add("Train");
        }
    }
}
