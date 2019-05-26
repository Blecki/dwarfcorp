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
        private static Zone _factory(RoomData Data, Faction Faction, WorldManager World)
        {
            return new BalloonPort(Data, Faction, World);
        }

        public BalloonPort()
        {

        }

        private BalloonPort(RoomData Data, Faction Faction, WorldManager World) :
            base(Data, Faction, World)
        {
        }

        private void CreateFlag(Vector3 At)
        {
            WorldManager.DoLazy(new Action(() =>
               {
                   var flag = EntityFactory.CreateEntity<Flag>("Flag", At + new Vector3(0.5f, 0.5f, 0.5f));
                   AddBody(flag, true);
               }));
        }

        public override void OnBuilt()
        {
            var box = GetBoundingBox();
            CreateFlag(new Vector3((box.Min.X + box.Max.X - 1) / 2, box.Max.Y, (box.Min.Z + box.Max.Z - 1) / 2));
        }
    }
}
