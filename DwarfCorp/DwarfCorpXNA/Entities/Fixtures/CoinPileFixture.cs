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
        private int _frame = -1;
        public void SetFullness(float value)
        {
            value = MathFunctions.Clamp(value, 0.0f, 1.0f);
            int frame = (int)Math.Round(value * 2);

            if (_frame != frame)
            {
                ResetSprite(new SpriteSheet(ContentPaths.Entities.DwarfObjects.coinpiles, 32, 32), new Point(frame, 0));
                _frame = frame;
            }
            
        }
        public CoinPileFixture()
        {

        }

        public CoinPileFixture(ComponentManager manager, Vector3 position) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.DwarfObjects.coinpiles, 32, 32), 
                new Point(MathFunctions.RandInt(0, 3), 0))
        {
            Name = "Coins";
            Tags.Add("Coins");
        }
    }
}
