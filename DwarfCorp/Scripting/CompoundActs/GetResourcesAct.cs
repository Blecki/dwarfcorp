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
        public List<KeyValuePair<Room, ResourceAmount> > ResourcesToStash { get; set; }
        public bool AllowHeterogenous { get; set; } // Todo: Unused
        public Faction Faction = null;

        public GetResourcesAct()
        {

        }

        public GetResourcesAct(CreatureAI agent, List<KeyValuePair<Room, ResourceAmount>> resources) :
            base(agent)
        {
            Name = "Get Resources";
            ResourcesToStash = resources;
            AllowHeterogenous = false;

        }

        public GetResourcesAct(CreatureAI agent, List<ResourceAmount> resources) :
            base(agent)
        {
            Name = "Get Resources";
            ResourcesToStash = agent.Faction.GetStockpilesContainingResources(agent.Position, resources).ToList();
            AllowHeterogenous = false;

        }


        public GetResourcesAct(CreatureAI agent, List<Quantitiy<Resource.ResourceTags>> resources ) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = resources;
            AllowHeterogenous = false;
        }

        public GetResourcesAct(CreatureAI agent, Resource.ResourceTags resources) :
            base(agent)
        {
            Name = "Get Resources";
            Resources = new List<Quantitiy<Resource.ResourceTags>>(){new Quantitiy<Resource.ResourceTags>(resources)};
            AllowHeterogenous = false;
        }


        public IEnumerable<Status> AlwaysTrue()
        {
            yield return Status.Success;
        }

        bool HasResources(CreatureAI agent, KeyValuePair<Room, ResourceAmount> resource)
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

            if (Faction == null)
            {
                Faction = Agent.Faction;
            }


            if(!hasAllResources)
            { 

                if(ResourcesToStash == null && Resources != null)
                    ResourcesToStash = Faction.GetStockpilesContainingResources(Resources).ToList();

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
                        children.Add(new Sequence(new Condition(() => HasResources(Agent, resource)), new StashResourcesAct(Agent, resource.Key, resource.Value) { Faction = Faction }));
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

    // Todo: SPLIT FILE
    public class TransferResourcesTask : Task
    {
        public string StockpileFrom;
        private Stockpile stockpile;
        public ResourceAmount Resources;

        public TransferResourcesTask()
        {

        }

        public TransferResourcesTask(string stockpile, ResourceAmount resources)
        {
            Priority = PriorityType.Medium;
            StockpileFrom = stockpile;
            Resources = resources;
            Name = String.Format("Transfer {0} {1} from {2}", Resources.Count, Resources.Type, stockpile);
            AutoRetry = true;
            ReassignOnDeath = true;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (!GetStockpile(agent.Faction))
                return 9999;

            return (stockpile.GetBoundingBox().Center() - agent.AI.Position).LengthSquared();
        }

        public bool GetStockpile(Faction faction)
        {
            if (stockpile != null)
                return faction.RoomBuilder.DesignatedRooms.Contains(stockpile);

            stockpile = faction.RoomBuilder.DesignatedRooms.FirstOrDefault(s => s.ID == StockpileFrom) as Stockpile;
            return stockpile != null;
        }

        public override bool IsComplete(Faction faction)
        {
            if (!GetStockpile(faction))
                return true;

            return !stockpile.Resources.HasResources(Resources);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || agent.Stats.IsAsleep || !agent.Active)
            {
                return Feasibility.Infeasible;
            }

            if (!GetStockpile(agent.Faction))
            {
                return Feasibility.Infeasible;
            }

            if (stockpile.Resources.HasResource(Resources))
            {
                return Feasibility.Feasible;
            }

            return Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature agent)
        {
            if (!GetStockpile(agent.Faction))
            {
                return null;
            }

            return new TransferResourcesAct(agent.AI, stockpile, Resources) { Name = "Transfer Resources" };
        }
    }


    public class TransferResourcesAct : CompoundCreatureAct
    {
        public Stockpile StockpileFrom = null;
        public ResourceAmount Resources = null;


        public TransferResourcesAct()
        {

        }

        public TransferResourcesAct(CreatureAI agent, Stockpile from, ResourceAmount resources) :
            base(agent)
        {
            StockpileFrom = from;
            Resources = resources;
        }

        public override void Initialize()
        {
            Tree = new Sequence(new GoToZoneAct(Agent, StockpileFrom),
                                new StashResourcesAct(Agent, StockpileFrom, Resources) { RestockType = Inventory.RestockType.RestockResource },
                                new StockResourceAct(Agent, Resources));
            base.Initialize();                     
        }
    }
}