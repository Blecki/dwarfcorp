using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    /// <summary>
    /// Equip the item stored on the blackboard.
    /// </summary>
    public class EquipAct : CreatureAct
    {
        public String BlackboardEntry = "ResourcesStashed";

        public EquipAct()
        {

        }

        public EquipAct(CreatureAI Agent) :
            base(Agent)
        {
            Name = "Equip Tool";
        }

        public override IEnumerable<Status> Run()
        {
            var list = Agent.Blackboard.GetData<List<Resource>>(BlackboardEntry);
            if (list != null)
                foreach (var item in list)
                    EquipHeldItem(item);
            else
            {
                var resource = Agent.Blackboard.GetData<Resource>(BlackboardEntry);

                if (resource == null)
                {
                    Agent.SetTaskFailureReason("The item I just had vanished!");
                    yield return Status.Fail;
                    yield break;
                }

                EquipHeldItem(resource);
            }
            yield return Status.Success;
        }

        private void EquipHeldItem(Resource Item)
        {
            if (Agent.Creature.Equipment.HasValue(out var equipment))
            {
                if (equipment.GetItemInSlot(Item.Equipment_Slot).HasValue(out var existingEquipment))
                {
                    equipment.UnequipItem(existingEquipment);
                    Creature.Inventory.AddResource(existingEquipment);
                }

                equipment.EquipItem(Item);
                Creature.Inventory.Remove(Item, Inventory.RestockType.Any);
            }
        }
    }
}