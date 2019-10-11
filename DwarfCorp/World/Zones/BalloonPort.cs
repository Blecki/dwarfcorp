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
        [ZoneFactory("Balloon Port")]
        private static Zone _factory(ZoneType Data, WorldManager World)
        {
            return new BalloonPort(Data, World);
        }

        public BalloonPort()
        {

        }

        private BalloonPort(ZoneType Data, WorldManager World) :
            base(Data, World)
        {
        }
    }
}
