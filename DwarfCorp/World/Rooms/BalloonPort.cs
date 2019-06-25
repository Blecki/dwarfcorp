using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class BalloonPort : Stockpile
    {
        [RoomFactory("Balloon Port")]
        private static Zone _factory(RoomType Data, WorldManager World)
        {
            return new BalloonPort(Data, World);
        }

        public BalloonPort()
        {

        }

        private BalloonPort(RoomType Data, WorldManager World) :
            base(Data, World)
        {
        }

        public override void OnBuilt()
        {
        }
    }
}
