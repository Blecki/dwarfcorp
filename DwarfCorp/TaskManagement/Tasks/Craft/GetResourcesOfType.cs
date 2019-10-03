using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item from a stockpile with the given tags, goes to it, and picks it up.
    /// </summary>
    public class GetResourcesOfType : CompoundCreatureAct
    {
        public List<ResourceTypeAmount> Resources;
        public List<KeyValuePair<Stockpile, Resource>> ResourcesToStash;
        public String BlackboardEntry = "ResourcesStashed";

        public GetResourcesOfType()
        {

        }

        public GetResourcesOfType(CreatureAI agent, List<ResourceTypeAmount> Resources) :
            base(agent)
        {
            Name = "Get Resources";
            this.Resources = Resources;
            ResourcesToStash = agent.World.GetStockpilesContainingResources(agent.Position, Resources).ToList();
        }

        public IEnumerable<Status> AlwaysTrue()
        {
            yield return Status.Success;
        }

        bool HasResources(CreatureAI agent, KeyValuePair<Stockpile, Resource> resource)
        {
            return resource.Key.Resources.Contains(resource.Value);
        }

        public override void Initialize()
        {
            var hasAllResources = true;

            if (Resources != null)
            {
                foreach (var resource in Resources)
                    if (!Creature.Inventory.HasResource(resource))
                        hasAllResources = false;
            }
            else if (ResourcesToStash != null)
            {
                foreach (var resource in ResourcesToStash)
                    if (!Creature.Inventory.Contains(resource.Value))
                    {
                        hasAllResources = false;
                        break;
                    }
            }
            else
            {
                Tree = null;
                return;
            }

            if(!hasAllResources)
            { 
                if(ResourcesToStash == null && Resources != null)
                    ResourcesToStash = Creature.World.GetStockpilesContainingResources(Resources).ToList();

                if(ResourcesToStash != null &&  ResourcesToStash.Count == 0)
                {
                    Tree = null;
                    return;
                }
                else
                {
                    var children = new List<Act>();
                    foreach (var resource in ResourcesToStash.OrderBy(r => (r.Key.GetBoundingBox().Center() - Agent.Position).LengthSquared()))
                    {
                        children.Add(new Domain(() => HasResources(Agent, resource), new GoToZoneAct(Agent, resource.Key)));
                        children.Add(new Sequence(new Condition(() => HasResources(Agent, resource)), new StashResourcesAct(Agent, resource.Key, resource.Value)));
                    }
                    children.Add(new SetBlackboardData<List<Resource>>(Agent, BlackboardEntry, ResourcesToStash.Select(r => r.Value).ToList()));
                    Tree = new Sequence(children.ToArray()) | (new Wrap(Agent.Creature.RestockAll) & false);
                }
            }
            else
            {
                if (ResourcesToStash == null && Resources != null)
                {
                    // In this case the dwarf already has all the resources. We have to find the resources from the inventory.
                    var resourcesStashed = new List<Resource>();
                    foreach (var amount in Resources)
                        resourcesStashed.AddRange(Creature.Inventory.FindResourcesOfType(amount));
                    Tree = new SetBlackboardData<List<Resource>>(Agent, BlackboardEntry, resourcesStashed);
                }
                else if (ResourcesToStash != null)
                    Tree = new SetBlackboardData<List<Resource>>(Agent, BlackboardEntry, ResourcesToStash.Select(r => r.Value).ToList());
                else
                    Tree = null;
            }
          
            base.Initialize();
        }
    }

}