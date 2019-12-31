using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class UnequipAct : CreatureAct
    {
        public Resource Resource;

        public UnequipAct()
        {

        }

        public UnequipAct(CreatureAI Agent, Resource Resource) :
            base(Agent)
        {
            this.Resource = Resource;
            Name = "Unequip Tool";
        }

        public override IEnumerable<Status> Run()
        {
            if (Agent.Creature.Equipment.HasValue(out var equipment) && equipment.GetItemInSlot(Resource.Equipment_Slot).HasValue(out var item) && Object.ReferenceEquals(item, Resource))
            {
                equipment.UnequipItem(item);
                Creature.Inventory.AddResource(item);
            }

            yield return Status.Success;
        }
    }
}