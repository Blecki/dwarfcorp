using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item from a stockpile or BuildRoom with the given tags, goes to it, and picks it up.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GetResourcesAct : CompoundCreatureAct
    {
        public List<ResourceAmount> Resources { get; set; }
 
        public GetResourcesAct()
        {

        }

        public GetResourcesAct(CreatureAI agent, List<ResourceAmount> resources ) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = resources;

        }


        public IEnumerable<Status> AlwaysTrue()
        {
            yield return Status.Success;
        }

        public override void Initialize()
        {

            bool hasAllResources = true;


            foreach(ResourceAmount resource in Resources)
            {
             
                if (!Creature.Inventory.Resources.HasResource(resource))
                {
                    hasAllResources = false;
                }
            }


            if(!hasAllResources)
            { 
                Stockpile nearestStockpile = Agent.Faction.GetNearestStockpile(Agent.Position);

                if(nearestStockpile == null)
                {
                    Tree = null;
                    return;
                }
                else
                {
                    Tree = new Sequence(new GoToZoneAct(Agent, nearestStockpile),
                                        new StashResourcesAct(Agent, Resources)
                                        );
                }
            }
            else
            {
                Tree = new Wrap(AlwaysTrue);
            }
          
            base.Initialize();
        }
    }

}