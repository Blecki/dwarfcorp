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
            var list = Agent.Blackboard.GetData<List<Resource>>(BlackboardEntry);
            if (list != null)
                foreach (var item in list)
                    EquipHeldItem(item);
            else
            {
                var toolResource = Agent.Blackboard.GetData<Resource>(BlackboardEntry);

                if (toolResource == null)
                {
                    yield return Status.Fail;
                    yield break;
                }

                EquipHeldItem(toolResource);
            }
            yield return Status.Success;
        }

        private void EquipHeldItem(Resource Item)
        {
            if (Agent.Creature.Equipment.HasValue(out var equipment))
            {
                if (equipment.GetItemInSlot(Item.Equipment_Slot).HasValue(out var existingTool))
                {
                    equipment.UnequipItem(existingTool);
                    Creature.Inventory.AddResource(existingTool);
                }

                equipment.EquipItem(Item);
                Creature.Inventory.Remove(Item, Inventory.RestockType.Any);
            }
        }
    }

    

    public class UnequipToolAct : CreatureAct
    {
        public Resource Resource;

        public UnequipToolAct()
        {

        }

        public UnequipToolAct(CreatureAI Agent, Resource Resource) :
            base(Agent)
        {
            this.Resource = Resource;
            Name = "Unequip Tool";
        }

        public override IEnumerable<Status> Run()
        {
            if (Agent.Creature.Equipment.HasValue(out var equipment) && equipment.GetItemInSlot(Resource.Equipment_Slot).HasValue(out var existingTool) && Object.ReferenceEquals(existingTool, Resource))
            {
                equipment.UnequipItem(existingTool);
                Creature.Inventory.AddResource(existingTool);
            }

            yield return Status.Success;
        }
    }
}