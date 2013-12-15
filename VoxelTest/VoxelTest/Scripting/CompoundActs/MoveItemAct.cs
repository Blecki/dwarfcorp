using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class MoveItemAct : CompoundCreatureAct
    {
        public Item Item { get; set; }
        public Zone Zone { get; set; }

        public MoveItemAct()
        {

        }

        public MoveItemAct(CreatureAIComponent agent, Item item, Zone zone) :
            base(agent)
        {
            Name = "Move item " + item.ID + " to zone " + zone.ID;
            Item = item;
            Zone = zone;

            Tree = new Sequence(
                new GoToEntityAct(item.UserData, agent),
                new SetBlackboardData<LocatableComponent>(agent, "TargetObject", item.UserData),
                new PickUpAct(agent, PickUpAct.PickUpType.Stockpile, item.Zone, "TargetObject"),
                new Sequence(
                    new GetNearestFreeVoxelInZone(agent, Zone, "FreeVoxel"),
                    new GoToNamedVoxelAct("FreeVoxel", agent),
                    new PutItemInZoneAct(agent, Zone)) | new DropItemAct(agent));
        }
    }

}