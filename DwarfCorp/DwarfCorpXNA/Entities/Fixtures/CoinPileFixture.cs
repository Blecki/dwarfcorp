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
        [EntityFactory("Coins")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new CoinPileFixture(Manager, Position);
        }


        private int _frame = -1;
        public void SetFullness(float value)
        {
            if (Children == null)
            {
                return;
            }

            value = MathFunctions.Clamp(value, 0.0f, 1.0f);
            int frame = (int)Math.Round(value * 2);

            if (_frame != frame)
            {
                Frame = new Point(frame, 0);

                //var childrenToKill = Children?.OfType<SimpleSprite>().ToList();
                //foreach (var child in childrenToKill)
                //    child.Delete();

                var sprite = GetComponent<SimpleSprite>();
                if (sprite != null)
                    sprite.SetFrame(Frame);

                _frame = frame;
            }
            
        }

        public CoinPileFixture()
        {
            Frame = new Point(0, 0);
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
