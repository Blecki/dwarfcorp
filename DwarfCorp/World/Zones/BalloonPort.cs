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
        private static Zone _factory(String ZoneTypeName, WorldManager World)
        {
            return new BalloonPort(ZoneTypeName, World);
        }

        public BalloonPort()
        {

        }

        private BalloonPort(String ZoneTypeName, WorldManager World) :
            base(ZoneTypeName, World)
        {
        }
    }
}
