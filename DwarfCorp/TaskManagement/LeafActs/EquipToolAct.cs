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
            var list = Agent.Blackboard.GetData<List<ResourceAmount>>(BlackboardEntry);
            if (list.Count == 0)
            {
                yield return Status.Fail;
                yield break;
            }

            var actualResource = Creature.Inventory.Resources.FirstOrDefault(i => i.Resource == list[0].Type);
            if (actualResource == null)
                yield return Status.Fail;
            else
            {
                if (Agent.Stats.Equipment.GetItemInSlot("tool").HasValue(out var existingTool))
                {
                    Agent.Stats.Equipment.UnequipItem("tool");
                    Creature.Inventory.AddResource(new ResourceAmount(existingTool.Resource, 1));
                }

                Agent.Stats.Equipment.EquipItem("tool", new EquippedItem { Resource = list[0].Type });
                Creature.Inventory.Remove(new ResourceAmount(list[0].Type, 1), Inventory.RestockType.Any);
                yield return Status.Success;
            }
        }
    }
}