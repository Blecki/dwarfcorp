using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should put the given item in the given voxel zone.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class PutItemInZoneTask : Task
    {
        public Zone Zone;
        public Item Item;

        public PutItemInZoneTask()
        {

        }

        public PutItemInZoneTask(Item item, Zone zone)
        {
            Name = "Put Item: " + item.ID + " in zone " + zone.ID;
            Item = item;
            Zone = zone;
        }

        public override Task Clone()
        {
            return new PutItemInZoneTask(Item, Zone);
        }

        public override Act CreateScript(Creature creature)
        {
            return null;
            //return new MoveItemAct(creature.AI, Item, Zone);
        }

        public override float ComputeCost(Creature agent)
        {
            return (Zone == null || Item == null || Item.UserData == null) ? 1000 : (agent.AI.Position - Item.UserData.GlobalTransform.Translation).LengthSquared();
        }
    }

}