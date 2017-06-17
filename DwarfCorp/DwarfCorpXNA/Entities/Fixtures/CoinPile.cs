using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{

    [JsonObject(IsReference = true)]
    public class CoinPileFixture : Fixture
    {
        public CoinPileFixture()
        {

        }

        public CoinPileFixture(ComponentManager manager, Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.DwarfObjects.coinpiles, 32, 32), new Point(MathFunctions.RandInt(0, 3), 0), manager.RootComponent)
        {
            Name = "Coins";
            Tags.Add("Coins");
        }
    }

    [JsonObject(IsReference = true)]
    public class CoinPile : ResourceEntity
    {
        public DwarfBux Money { get; set; }
        public CoinPile()
        {

        }

        public CoinPile(ComponentManager manager, Vector3 position) :
            base(manager, ResourceLibrary.ResourceType.Coins, position)
        {
            Name = "Coins";
            Tags.Add("Coins");
        }
    }


}
