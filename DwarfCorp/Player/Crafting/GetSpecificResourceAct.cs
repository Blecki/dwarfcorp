using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GetSpecificResourceAct : CompoundCreatureAct
    {
        public Resource Resource;
        public String BlackboardEntry = "ResourcesStashed";

        public GetSpecificResourceAct()
        {

        }

        public GetSpecificResourceAct(CreatureAI agent, Resource Resource) :
            base(agent)
        {
            Name = "Get Resources";
            this.Resource = Resource;
        }

        public override void Initialize()
        {
            var hasAllResources = Creature.Inventory.Contains(Resource);

            if (!hasAllResources)
            {
                var zone = Creature.World.GetStockpileContainingResource(Resource);
                if (zone.HasValue(out var stockpile))
                    Tree = new Domain(() => stockpile.Resources.Contains(Resource),
                        new Sequence(
                            new GoToZoneAct(Agent, stockpile),
                            new StashResourcesAct(Agent, stockpile, Resource),
                            new SetBlackboardData<List<Resource>>(Agent, BlackboardEntry, new List<Resource> { Resource })));
                else
                    Tree = null;
            }
            else // Dwarf already has the resource!
                Tree = new SetBlackboardData<List<Resource>>(Agent, BlackboardEntry, new List<Resource> { Resource });
          
            base.Initialize();
        }
    }

}