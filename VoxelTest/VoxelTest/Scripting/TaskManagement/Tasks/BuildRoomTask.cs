using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should find an item with the specified
    /// tags and put it in a given zone.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class BuildRoomTask : Task
    {
        public BuildRoomOrder Zone;

        public BuildRoomTask()
        {

        }

        public BuildRoomTask(BuildRoomOrder zone)
        {
            Name = "Build BuildRoom " + zone.ToBuild.RoomType.Name + zone.ToBuild.ID;
            Zone = zone;
        }

        public override Act CreateScript(Creature creature)
        {
            return new BuildRoomAct(creature.AI, Zone);
        }

        public override float ComputeCost(Creature agent)
        {
            return (Zone == null) ? 1000 : 1.0f;
        }
    }

}