using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class MoveItemAct : CompoundCreatureAct
    {
        public Item Item { get; set; }
        public Zone Zone { get; set; }

        public MoveItemAct(CreatureAIComponent agent, Item item, Zone zone) :
            base(agent)
        {
            Name = "Move item " + item.ID + " to zone " + zone.ID;
            Item = item;
            Zone = zone;

            Tree = new Sequence(new GoToEntityAct(item.userData, agent),
                                new PickUpTargetAct(agent, PickUpTargetAct.PickUpType.Stockpile, item.Zone),
                                new Sequence(
                                new GetNearestFreeVoxelInZone(agent, Zone, "FreeVoxel"),
                                new GoToNamedVoxelAct("FreeVoxel", agent),
                                new PutItemInZoneAct(agent, Zone)) | new DropItemAct(agent));
        }

        
    }
}
