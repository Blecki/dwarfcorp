using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item from a stockpile or BuildRoom with the given tags, goes to it, and picks it up.
    /// </summary>
    public class GetResourcesAct : CompoundCreatureAct
    {
        public List<Quantitiy<Resource.ResourceTags>> Resources { get; set; }
        public List<KeyValuePair<Zone, ResourceAmount> > ResourcesToStash { get; set; }

        public GetResourcesAct()
        {

        }

        public GetResourcesAct(CreatureAI agent, List<ResourceAmount> resources) :
            base(agent)
        {
            Name = "Get Resources";
            ResourcesToStash = agent.World.GetStockpilesContainingResources(agent.Position, resources).ToList();
        }


        public GetResourcesAct(CreatureAI agent, List<Quantitiy<Resource.ResourceTags>> resources ) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = resources;
        }

        public GetResourcesAct(CreatureAI agent, Resource.ResourceTags resources) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = new List<Quantitiy<Resource.ResourceTags>>(){new Quantitiy<Resource.ResourceTags>(resources)};
        }

        public IEnumerable<Status> AlwaysTrue()
        {
            yield return Status.Success;
        }

        bool HasResources(CreatureAI agent, KeyValuePair<Zone, ResourceAmount> resource)
        {
            return resource.Key.Resources.HasResources(resource.Value);
        }

        public override void Initialize()
        {
            bool hasAllResources = true;

            if (Resources != null)
            {
                foreach (Quantitiy<Resource.ResourceTags> resource in Resources)
                {
                    if (!Creature.Inventory.HasResource(resource))
                    {
                        hasAllResources = false;
                    }
                }
            }
            else if (ResourcesToStash != null)
            {
                foreach (var resource in ResourcesToStash)
                {
                    if (!Creature.Inventory.HasResource(resource.Value))
                    {
                        hasAllResources = false;
                        break;
                    }
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
                    children.Add(new SetBlackboardData<List<ResourceAmount>>(Agent, "ResourcesStashed", ResourcesToStash.Select(r => r.Value).ToList()));
                    Tree = new Sequence(children.ToArray()) | (new Wrap(Agent.Creature.RestockAll) & false);
                }
            }
            else
            {
                if (ResourcesToStash == null && Resources != null)
                {
                    // In this case the dwarf already has all the resources. We have to find the resources from the inventory.
                    List<ResourceAmount> resourcesStashed = new List<ResourceAmount>();
                    foreach (var tag in Resources)
                    {
                        resourcesStashed.AddRange(Creature.Inventory.GetResources(tag, Inventory.RestockType.Any));
                    }
                    Tree = new SetBlackboardData<List<ResourceAmount>>(Agent, "ResourcesStashed", resourcesStashed);
                }
                else if (ResourcesToStash != null)
                {
                    Tree = new SetBlackboardData<List<ResourceAmount>>(Agent, "ResourcesStashed", ResourcesToStash.Select(r => r.Value).ToList());
                }
                else
                {
                    Tree = null;
                }
            }
          
            base.Initialize();
        }
    }

}