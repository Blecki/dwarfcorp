using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class UnequipAct : CreatureAct
    {
        public String BlackboardEntry = "Resource";

        public UnequipAct()
        {

        }

        public UnequipAct(CreatureAI Agent) :
            base(Agent)
        {
            Name = "Unequip";
        }

        public override IEnumerable<Status> Run()
        {
            var list = Agent.Blackboard.GetData<List<Resource>>(BlackboardEntry);
            if (list != null)
                foreach (var item in list)
                    UnequipItem(item);
            else
            {
                var resource = Agent.Blackboard.GetData<Resource>(BlackboardEntry);

                if (resource == null)
                {
                    yield return Status.Fail;
                    yield break;
                }

                UnequipItem(resource);
            }
            yield return Status.Success;
        }

        private void UnequipItem(Resource Item)
        {
            if (Agent.Creature.Equipment.HasValue(out var equipment)
                && equipment.GetItemInSlot(Item.Equipment_Slot).HasValue(out var existingEquipment)
                && Object.ReferenceEquals(Item, existingEquipment))
            {
                equipment.UnequipItem(existingEquipment);
                Creature.Inventory.AddResource(existingEquipment);
            }
        }
    }
}