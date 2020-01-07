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
        public Resource ResourceToStock = null;
        public string ZoneType = "Stockpile";

        public StockResourceTask()
        {
            Category = TaskCategory.Other;
            Priority = TaskPriority.Medium;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
        }

        public StockResourceTask(Resource ResourceToStock)
        {
            Category = TaskCategory.Other;
            this.ResourceToStock = ResourceToStock;
            Name = "Stock Entity: " + ResourceToStock.TypeName;
            Priority = TaskPriority.Medium;
            ReassignOnDeath = false;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            return new StockResourceAct(creature.AI, ResourceToStock);
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (!agent.Inventory.Contains(ResourceToStock))
                return true;
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            return agent.World.HasFreeStockpile(new ResourceTypeAmount(ResourceToStock.TypeName, 1)) && 
                !agent.AI.Movement.IsSessile && agent.Inventory.Contains(ResourceToStock) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return IsFeasible(agent) == Feasibility.Feasible ? 1.0f : 1000.0f;
        }

    }

}