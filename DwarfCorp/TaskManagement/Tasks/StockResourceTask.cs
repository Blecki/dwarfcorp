using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should pick up an item and put it in a stockpile.
    /// </summary>
    internal class StockResourceTask : Task
    {
        public ResourceAmount EntityToGather = null;
        public string ZoneType = "Stockpile";

        public StockResourceTask()
        {
            Category = TaskCategory.Gather;
            Priority = TaskPriority.Medium;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        public StockResourceTask(ResourceAmount entity)
        {
            Category = TaskCategory.Gather;
            EntityToGather = entity.CloneResource();
            Name = "Stock Entity: " + entity.Type + " " + entity.Count;
            Priority = TaskPriority.Medium;
            ReassignOnDeath = false;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            return new StockResourceAct(creature.AI, EntityToGather);
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (!agent.Inventory.HasResource(EntityToGather))
            {
                return true;
            }
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            return agent.World.HasFreeStockpile(EntityToGather) && 
                !agent.AI.Movement.IsSessile && agent.Inventory.HasResource(EntityToGather) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return IsFeasible(agent) == Feasibility.Feasible ? 1.0f : 1000.0f;
        }

    }

}