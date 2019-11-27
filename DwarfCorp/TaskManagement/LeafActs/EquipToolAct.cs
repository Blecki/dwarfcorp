using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    /// <summary>
    /// Equip the item stored on the blackboard.
    /// </summary>
    public class EquipToolAct : CreatureAct // Todo: Change to generic 'equip item' act.
    {
        public String BlackboardEntry = "ResourcesStashed";

        public EquipToolAct()
        {

        }

        public EquipToolAct(CreatureAI Agent) :
            base(Agent)
        {
            Name = "Equip Tool";
        }

        public override IEnumerable<Status> Run()
        {
            var toolResource = Agent.Blackboard.GetData<Resource>(BlackboardEntry);

            if (toolResource == null)
            {
                yield return Status.Fail;
                yield break;
            }

            if (Agent.Creature.Equipment.HasValue(out var equipment))
            {
                // Kinda assumes the new item will go in the tool slot, no? Also that an existing item should be removed.
                if (equipment.GetItemInSlot(EquipmentSlot.Tool).HasValue(out var existingTool))
                {
                    equipment.UnequipItem(existingTool);
                    Creature.Inventory.AddResource(existingTool);
                }

                equipment.EquipItem(toolResource);
                Creature.Inventory.Remove(toolResource, Inventory.RestockType.Any);
                yield return Status.Success;
            }
            else
                yield return Status.Fail;
        }
    }

    public class UnequipToolAct : CreatureAct
    {
        public UnequipToolAct()
        {

        }

        public UnequipToolAct(CreatureAI Agent) :
            base(Agent)
        {
            Name = "Unequip Tool";
        }

        public override IEnumerable<Status> Run()
        {
            if (Agent.Creature.Equipment.HasValue(out var equipment) && equipment.GetItemInSlot(EquipmentSlot.Tool).HasValue(out var existingTool))
            {
                equipment.UnequipItem(existingTool);
                Creature.Inventory.AddResource(existingTool);
            }

            yield return Status.Success;
        }
    }
}