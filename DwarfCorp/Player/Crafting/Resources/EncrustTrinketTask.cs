using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    class EncrustTrinketTask : Task
    {
        public int TaskID = 0;
        private static int MaxID = 0;
        public ResourceTypeAmount Gem;
        public Resource BaseTrinket;
        public CraftItem ItemType;
        public ResourceDes Des;

        public EncrustTrinketTask()
        {
            Category = TaskCategory.CraftItem;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
            MaxAssignable = 1;
        }

        public EncrustTrinketTask(CraftItem ItemType)
        {
            TaskID = MaxID;
            MaxID++;

            this.ItemType = ItemType;

            Name = String.Format("{2} order {0}: {1}", TaskID, ItemType.PluralDisplayName, ItemType.Verb.Base);
            Priority = TaskPriority.Medium;

            Category = ItemType.CraftTaskCategory;

            AutoRetry = true;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;

            Des = new ResourceDes();

            MaxAssignable = 1;
        }

        public override bool IsComplete(WorldManager World)
        {
            return Des.Finished;
        }

        private bool HasResources(Creature agent)
        {
            if (Des.HasResources)
                return true;

            return agent.World.HasResources(new List<ResourceTypeAmount> { Gem });
        }

        private bool HasLocation(Creature agent)
        {
            if (ItemType.CraftLocation != ""
                && !agent.Faction.OwnedObjects.Any(o => o.Tags.Contains(ItemType.CraftLocation) && (!o.IsReserved || o.ReservedFor == agent.AI)))
                    return false;
            return true;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(Category))
                return Feasibility.Infeasible;

            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            return HasResources(agent) && HasLocation(agent) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public IEnumerable<Act.Status> Cleanup(CreatureAI creature)
        {
            yield return Act.Status.Success;
        }

        public override void OnDequeued(WorldManager World)
        {
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            return new Sequence(new EncrustTrinketAct(creature.AI, ItemType, BaseTrinket, Gem, Des)
            {
                Noise = ItemType.CraftNoise
            }) | new Wrap(() => Cleanup(creature.AI));
        }
    }
}