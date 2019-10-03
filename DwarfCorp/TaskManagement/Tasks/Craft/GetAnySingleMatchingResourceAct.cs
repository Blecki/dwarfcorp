using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GetAnySingleMatchingResourceAct : CompoundCreatureAct
    {
        public List<String> Tags;
        public String BlackboardEntry = "ResourcesStashed";

        public GetAnySingleMatchingResourceAct()
        {

        }

        public GetAnySingleMatchingResourceAct(CreatureAI agent, List<String> Tags) :
            base(agent)
        {
            Name = "Get Resources";
            this.Tags = Tags;
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
            var resource = Tags.Select(t =>
            {
                var matches = Creature.Inventory.EnumerateResources(new ResourceTagAmount(t, 1));
                if (matches.Count > 0)
                    return matches[0];
                return null;
            }).FirstOrDefault(r => r != null);

            if (resource == null)
            {
                var location = Creature.World.GetFirstStockpileContainingResourceWithMatchingTag(Tags);

                if (!location.HasValue)
                {
                    Tree = null;
                    return;
                }
                else
                {
                    Tree = new Sequence(
                        new Domain(() => HasResources(Agent, location.Value),
                            new GoToZoneAct(Agent, location.Value.Key)),
                        new Condition(() => HasResources(Agent, location.Value)),
                        new StashResourcesAct(Agent, location.Value.Key, location.Value.Value),
                        new SetBlackboardData<Resource>(Agent, BlackboardEntry, location.Value.Value))
                        | (new Wrap(Agent.Creature.RestockAll) & false);
                }
            }
            else
            {
                Tree = new SetBlackboardData<Resource>(Agent, BlackboardEntry, resource);
            }
            
            base.Initialize();
        }
    }

}