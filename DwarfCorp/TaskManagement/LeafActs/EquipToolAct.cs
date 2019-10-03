using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    /// <summary>
    /// Equip the item stored on the blackboard.
    /// </summary>
    public class EquipToolAct : CreatureAct
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

            if (Agent.Stats.Equipment.GetItemInSlot("tool").HasValue(out var existingTool))
            {
                Agent.Stats.Equipment.UnequipItem("tool");
                Creature.Inventory.AddResource(existingTool.Resource);
            }

            Agent.Stats.Equipment.EquipItem("tool", new EquippedItem { Resource = toolResource });
            Creature.Inventory.Remove(toolResource, Inventory.RestockType.Any);
            yield return Status.Success;
        }
    }
}