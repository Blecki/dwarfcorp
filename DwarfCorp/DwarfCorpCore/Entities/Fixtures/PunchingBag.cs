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
    public class PunchingBag : Fixture
    {
        public PunchingBag()
        {

        }

        public PunchingBag(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 5), PlayState.ComponentManager.RootComponent)
        {
            Name = "PunchingBag";
            Tags.Add("PunchingBag");
            Tags.Add("Train");
        }
    }
}
