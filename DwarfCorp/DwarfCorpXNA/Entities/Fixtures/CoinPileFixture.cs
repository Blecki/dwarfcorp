using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CoinPileFixture : Fixture
    {
        public CoinPileFixture()
        {

        }

        public CoinPileFixture(ComponentManager manager, Vector3 position) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.DwarfObjects.coinpiles, 32, 32), new Point(MathFunctions.RandInt(0, 3), 0))
        {
            Name = "Coins";
            Tags.Add("Coins");
        }
    }
}
