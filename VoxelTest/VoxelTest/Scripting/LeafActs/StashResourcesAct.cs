using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature grabs a given item and puts it in their inventory
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class StashResourcesAct : CreatureAct
    {
        public List<ResourceAmount> Resources { get; set; }

        public StashResourcesAct()
        {

        }

        public StashResourcesAct(CreatureAI agent, List<ResourceAmount> resources) :
            base(agent)
        {
            Resources = resources;
            Name = "Stash " + Resources.ToString();
        }

        public override IEnumerable<Status> Run()
        {
            bool removed = Agent.Faction.RemoveResources(Resources, Agent.Position);

            if(!removed)
            {
                yield return Status.Fail;
            }
            else
            {
                foreach(ResourceAmount resource in Resources)
                {
                    Agent.Creature.Inventory.Resources.AddResource(resource);   
                }
                yield return Status.Success;
            }

        }

    }

}

