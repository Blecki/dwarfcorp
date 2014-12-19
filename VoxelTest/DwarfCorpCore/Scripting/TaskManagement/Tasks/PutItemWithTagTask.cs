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
    internal class PutItemWithTagTask : Task
    {
        public Zone Zone;
        public TagList Tags;

        public PutItemWithTagTask()
        {
            Priority = PriorityType.Low;
        }

        public PutItemWithTagTask(TagList tags, Zone zone)
        {
            Name = "Put Item with tag: " + tags + " in zone " + zone.ID;
            Tags = tags;
            Zone = zone;
            Priority = PriorityType.Low;
        }

        public override Task Clone()
        {
            return new PutItemWithTagTask(Tags, Zone);
        }

        public override Act CreateScript(Creature creature)
        {
            Room room = Zone as Room;
            if(room == null)
            {
                return null;
            }

            if(!creature.Faction.RoomBuilder.IsBuildDesignation(room))
            {
                return null;
            }

            BuildVoxelOrder voxVoxelOrder = creature.Faction.RoomBuilder.GetBuildDesignation(room);

            if(voxVoxelOrder == null)
            {
                return null;
            }

            BuildRoomOrder designation = voxVoxelOrder.Order;
            return  new BuildRoomAct(creature.AI, designation);
        }

        public override float ComputeCost(Creature agent)
        {
            return (Zone == null) ? 1000 : 1.0f;
        }
    }

}