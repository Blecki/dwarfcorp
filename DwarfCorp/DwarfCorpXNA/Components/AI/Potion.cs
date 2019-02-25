using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    // Todo: Split file.
    public class Potion
    {
        public String Name;
        public string Description;
        public Buff Effects;
        public List<Quantitiy<Resource.ResourceTags>> Ingredients;
        public int Icon;
        public Potion()
        {

        }

        public void Drink(Creature creature)
        {
            creature.AddBuff(Effects.Clone());
        }

        public bool ShouldDrink(Creature creature)
        {
            if (Effects == null)
            {
                return false;
            }
            return Effects.IsRelevant(creature);
        }
    }

    public class GatherPotionsTask : Task
    {
        public GatherPotionsTask()
        {
            Name = "Gather Potions";
            ReassignOnDeath = false;
            AutoRetry = false;
            Priority = PriorityType.Medium;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.Faction.ListResourcesWithTag(Resource.ResourceTags.Potion).Count > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GetResourcesAct(agent.AI, new List<Quantitiy<Resource.ResourceTags>>() { new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Potion)});
        }
    }

    public static class PotionLibrary
    {
        public static Dictionary<string, Potion> Potions = null;

        public static void Initialize()
        {
            // Todo: Embedd potion object into item (or resource-item? Why is it in one and not the other) and do away with this library entirely.
            Potions = new Dictionary<string, Potion>();
            foreach (var potion in FileUtils.LoadJsonListFromMultipleSources<Potion>(ContentPaths.potions, null, p => p.Name))
                Potions.Add(potion.Name, potion);
        }
    }

}
