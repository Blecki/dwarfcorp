using System.Linq;
using System;
using System.Collections.Generic;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public enum EquipmentFallback
        {
            NoFallback,
            AllowDefault
        }

        public static Act CreateEquipmentCheckAct(CreatureAI Agent, String Slot, EquipmentFallback FallbackType, params String[] RequiredTags)
        {
            return new EquipmentCheckAct(Agent, Slot, FallbackType, RequiredTags);
        }

    }

    public class EquipmentCheckAct : Act
    {
        

        public CreatureAI Agent;
        public String Slot;
        public ActHelper.EquipmentFallback FallbackType;
        public String[] RequiredTags;

        public EquipmentCheckAct(CreatureAI Agent, String Slot, ActHelper.EquipmentFallback FallbackType, params String[] RequiredTags)
        {
            this.Agent = Agent;
            this.Slot = Slot;
            this.FallbackType = FallbackType;
            this.RequiredTags = RequiredTags;
        }

        public override IEnumerable<Status> Run()
        {
            if (!Agent.Stats.CurrentClass.HasValue(out var c))
            {
                yield return Status.Success;
                yield break;
            }

            if (!c.RequiresTools)
            {
                yield return Status.Success;
                yield break;
            }

            if (!Agent.Creature.Equipment.HasValue(out var equipment))
            {
                Debugger.RaiseDebugWarning("Encountered entity that requires tools but does not have an equipment component.");
                yield return Status.Success;
                yield break;
            }

            var equippedItem = equipment.GetItemInSlot(Slot);

            // We already have the appropriate tool!
            if (equippedItem.HasValue(out var item) && item.ResourceType.HasValue(out var res)
                && res.Tags.Any(t => RequiredTags.Contains(t)))
            {
                yield return Status.Success;
                yield break;
            }

            var getResourceAct = new GetAnySingleMatchingResourceAct(Agent, RequiredTags.ToList()) { BlackboardEntry = "tool-stashed" };
            getResourceAct.Initialize();
            foreach (var status in getResourceAct.Run())
            {
                if (status == Status.Fail) // Couldn't find a resource...
                {
                    if (FallbackType == ActHelper.EquipmentFallback.AllowDefault)
                    {
                        if (equippedItem.HasValue(out var tool))
                        {
                            Agent.Blackboard.SetData("item-to-remove", tool);
                            var unequipAct = new UnequipAct(Agent) { BlackboardEntry = "item-to-remove" };
                            unequipAct.Initialize();
                            foreach (var unequip_status in unequipAct.Run())
                                yield return unequip_status;
                            yield return Status.Success;
                            yield break;
                        }
                        else
                        {
                            yield return Status.Success;
                            yield break;
                        }
                    }
                    else
                    {
                        Agent.SetTaskFailureReason("Coult not locate tool.");
                        yield return Status.Fail;
                        yield break;
                    }
                }
                else if (status == Status.Success) // Found a tool!
                {
                    var equipAct = new EquipAct(Agent) { BlackboardEntry = "tool-stashed" };
                    equipAct.Initialize();
                    foreach (var equip_status in equipAct.Run())
                        yield return equip_status;
                    yield return Status.Success;
                    yield break;
                }
                else if (status == Status.Running)
                    yield return Status.Running;
            }
        }
    }
}