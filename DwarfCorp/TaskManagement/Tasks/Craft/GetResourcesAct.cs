using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item from a stockpile with the given tags, goes to it, and picks it up.
    /// </summary>
    public class GetResourcesWithTag : CompoundCreatureAct
    {
        public List<ResourceTagAmount> Resources { get; set; }
        public List<KeyValuePair<Stockpile, ResourceAmount> > ResourcesToStash { get; set; }
        public String BlackboardEntry = "ResourcesStashed";

        public GetResourcesWithTag()
        {

        }

        public GetResourcesWithTag(CreatureAI agent, List<ResourceAmount> resources) :
            base(agent)
        {
            Name = "Get Resources";
            ResourcesToStash = agent.World.GetStockpilesContainingResources(agent.Position, resources).ToList();
        }


        public GetResourcesWithTag(CreatureAI agent, List<ResourceTagAmount> resources ) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = resources;
        }

        public GetResourcesWithTag(CreatureAI agent, String Tag) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = new List<ResourceTagAmount>(){new ResourceTagAmount(Tag, 1)};
        }

        public IEnumerable<Status> AlwaysTrue()
        {
            yield return Status.Success;
        }

        bool HasResources(CreatureAI agent, KeyValuePair<Stockpile, ResourceAmount> resource)
        {
            return resource.Key.Resources.Has(resource.Value.Type, resource.Value.Count);
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
                    if (!Creature.Inventory.HasResource(resource.Value))
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
                    List<Act> children = new List<Act>();
                    foreach (var resource in ResourcesToStash.OrderBy(r => (r.Key.GetBoundingBox().Center() - Agent.Position).LengthSquared()))
                    {
                        children.Add(new Domain(() => HasResources(Agent, resource), new GoToZoneAct(Agent, resource.Key)));
                        children.Add(new Sequence(new Condition(() => HasResources(Agent, resource)), new StashResourcesAct(Agent, resource.Key, resource.Value)));
                    }
                    children.Add(new SetBlackboardData<List<ResourceAmount>>(Agent, BlackboardEntry, ResourcesToStash.Select(r => r.Value).ToList()));
                    Tree = new Sequence(children.ToArray()) | (new Wrap(Agent.Creature.RestockAll) & false);
                }
            }
            else
            {
                if (ResourcesToStash == null && Resources != null)
                {
                    // In this case the dwarf already has all the resources. We have to find the resources from the inventory.
                    var resourcesStashed = new List<ResourceAmount>();
                    foreach (var tag in Resources)
                        resourcesStashed.AddRange(Creature.Inventory.EnumerateResources(tag, Inventory.RestockType.Any));
                    Tree = new SetBlackboardData<List<ResourceAmount>>(Agent, BlackboardEntry, resourcesStashed);
                }
                else if (ResourcesToStash != null)
                    Tree = new SetBlackboardData<List<ResourceAmount>>(Agent, BlackboardEntry, ResourcesToStash.Select(r => r.Value).ToList());
                else
                    Tree = null;
            }
          
            base.Initialize();
        }
    }

}