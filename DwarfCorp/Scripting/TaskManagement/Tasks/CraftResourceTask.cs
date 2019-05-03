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
        public CraftDesignation Item { get; set; }
        private string noise;
        public bool IsAutonomous { get; set; }
        public int NumRepeats;
        public int CurrentRepeat;

        public CraftResourceTask()
        {
            Category = TaskCategory.CraftItem;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public CraftResourceTask(CraftItem selectedResource, int CurrentRepeat, int NumRepeats, List<ResourceAmount> SelectedResources, int id = -1)
        {
            this.CurrentRepeat = CurrentRepeat;
            this.NumRepeats = NumRepeats;

            TaskID = id < 0 ? MaxID : id;
            MaxID++;
            Item = new CraftDesignation()
            {
                ItemType = selectedResource,
                Location = VoxelHandle.InvalidHandle,
                Valid = true,
                SelectedResources = SelectedResources
            };
            string verb = selectedResource.Verb;
            Name = String.Format("{4} order {0}: {1}/{2} {3}", TaskID, CurrentRepeat, NumRepeats, selectedResource.PluralDisplayName, verb);
            Priority = PriorityType.Medium;

            if (ResourceLibrary.GetResourceByName(Item.ItemType.ResourceCreated).Tags.Contains(Resource.ResourceTags.Edible))
            {
                noise = "Cook";
                Category = TaskCategory.Cook;
            }
            else
            {
                noise = "Craft";
                Category = selectedResource.IsMagical ? TaskCategory.Research : TaskCategory.CraftItem;
            }

            AutoRetry = true;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public override bool IsComplete(Faction faction)
        {
            return Item.Finished;
        }

        private bool HasResources(Creature agent)
        {
            if (Item.HasResources)
                return true;

            if (Item.SelectedResources.Count != 0)
                return agent.Faction.HasResources(Item.SelectedResources);
            return agent.Faction.HasResources(Item.ItemType.RequiredResources);
        }

        private bool HasLocation(Creature agent)
        {
            if (Item.ItemType.CraftLocation != "")
            {
                var anyCraftLocation = agent.Faction.OwnedObjects.Any(o => o.Tags.Contains(Item.ItemType.CraftLocation) && (!o.IsReserved || o.ReservedFor == agent.AI));
                if (!anyCraftLocation)
                    return false;
            }
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
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.Faction == creature.World.PlayerFaction)
                {
                    creature.World.MakeAnnouncement(
                        StringLibrary.GetString("cancelled-crafting-unreachable", creature.Stats.FullName, Item.ItemType.DisplayName));
                    creature.World.Master.TaskManager.CancelTask(this);
                }
            }
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature creature)
        {
            return new Sequence(new CraftItemAct(creature.AI, Item)
            {
                Noise = noise
            }) | new Wrap(() => Cleanup(creature.AI));
        }
    }
}