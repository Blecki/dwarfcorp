using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item from a stockpile with the given tags, goes to it, and picks it up.
    /// </summary>
    public class GetResourcesOfApparentType : CompoundCreatureAct
    {
        public List<ResourceApparentTypeAmount> Resources;
        public String BlackboardEntry = "ResourcesStashed";

        public GetResourcesOfApparentType()
        {

        }

        public GetResourcesOfApparentType(CreatureAI agent, List<ResourceApparentTypeAmount> Resources) :
            base(agent)
        {
            Name = "Get Resources";
            this.Resources = OptimizeList(Resources);
        }

        public IEnumerable<Status> AlwaysTrue()
        {
            yield return Status.Success;
        }

        bool HasResources(CreatureAI agent, KeyValuePair<Stockpile, Resource> resource)
        {
            return resource.Key.Resources.Contains(resource.Value);
        }

        private static List<ResourceApparentTypeAmount> OptimizeList(List<ResourceApparentTypeAmount> Resources)
        {
            var r = new Dictionary<String, int>();
            foreach (var res in Resources)
            {
                if (!r.ContainsKey(res.Type))
                    r.Add(res.Type, 0);
                r[res.Type] += res.Count;
            }
            return r.Select(k => new ResourceApparentTypeAmount(k.Key, k.Value)).ToList();
        }

        public override void Initialize()
        {
            var needed = new List<ResourceApparentTypeAmount>();
                       
            foreach (var resource in OptimizeList(Resources))
            {
                var count = Creature.Inventory.Resources.Count(i => i.Resource.TypeName == resource.Type);
                if (count < resource.Count)
                    needed.Add(new ResourceApparentTypeAmount(resource.Type, resource.Count - count));
            }

            if (needed.Count > 0)
            {
                var resourcesToStash = Creature.World.GetStockpilesContainingResources(needed).ToList();

                if (resourcesToStash != null && resourcesToStash.Count == 0)
                {
                    Tree = null;
                    return;
                }
                else
                {
                    var children = new List<Act>();
                    foreach (var resource in resourcesToStash.OrderBy(r => (r.Key.GetBoundingBox().Center() - Agent.Position).LengthSquared()))
                    {
                        children.Add(new Domain(() => HasResources(Agent, resource), new GoToZoneAct(Agent, resource.Key)));
                        children.Add(new Sequence(new Condition(() => HasResources(Agent, resource)), new StashResourcesAct(Agent, resource.Key, resource.Value)));
                    }
                    children.Add(new SetBlackboardData<List<Resource>>(Agent, BlackboardEntry, resourcesToStash.Select(r => r.Value).ToList()));
                    Tree = new Select(
                        new Sequence(children.ToArray()),
                        new Sequence(
                            new Wrap(Agent.Creature.RestockAll),
                            false));
                }
            }
            else
            {
                // In this case the dwarf already has all the resources. We have to find the resources from the inventory.
                var resourcesStashed = new List<Resource>();
                foreach (var amount in OptimizeList(Resources))
                    resourcesStashed.AddRange(Creature.Inventory.FindResourcesOfApparentType(amount));
                Tree = new SetBlackboardData<List<Resource>>(Agent, BlackboardEntry, resourcesStashed);
            }
            
            base.Initialize();
        }
    }

}