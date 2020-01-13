using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    class CraftResourceTask : Task
    {
        public int TaskID = 0;
        private static int MaxID = 0;
        public bool IsAutonomous { get; set; }
        public int NumRepeats;
        public int CurrentRepeat;
        public List<ResourceApparentTypeAmount> RawMaterials;
        public CraftItem ItemType;
        public ResourceDes Des;

        public CraftResourceTask()
        {
            Category = TaskCategory.CraftItem;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
            MaxAssignable = 1;
        }

        public CraftResourceTask(CraftItem ItemType, int CurrentRepeat, int NumRepeats, List<ResourceApparentTypeAmount> RawMaterials, int id = -1)
        {
            this.CurrentRepeat = CurrentRepeat;
            this.NumRepeats = NumRepeats;

            TaskID = id < 0 ? MaxID : id;
            MaxID++;

            this.RawMaterials = RawMaterials;
            this.ItemType = ItemType;

            Name = String.Format("{4} order {0}: {1}/{2} {3}", TaskID, CurrentRepeat, NumRepeats, ItemType.PluralDisplayName, ItemType.Verb.Base);
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

            return agent.World.HasResources(RawMaterials);
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
            return new Sequence(new CraftResourceAct(creature.AI, ItemType, RawMaterials, Des)
            {
                Noise = ItemType.CraftNoise
            }) | new Wrap(() => Cleanup(creature.AI));
        }
    }
}