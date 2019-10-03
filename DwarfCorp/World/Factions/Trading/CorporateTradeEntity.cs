using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Trade
{
    public class CorporateTradeEntity : ITradeEntity
    {
        private TradeEnvoy SourceEnvoy;
        private WorldManager World;

        public CorporateTradeEntity(TradeEnvoy SourceEnvoy)
        {
            this.SourceEnvoy = SourceEnvoy;
            this.World = SourceEnvoy.OwnerFaction.World; // Gross
        }

        public int AvailableSpace => 0;
        public DwarfBux Money => World.Overworld.PlayerCorporationFunds;
        public ResourceSet Resources => World.Overworld.PlayerCorporationResources; // Todo: Makes add/remove obsolete?

        public void AddMoney(DwarfBux Money)
        {
            World.Overworld.PlayerCorporationFunds += Money;
        }

        public void AddResources(List<Resource> Resources)
        {
            foreach(var resource in Resources)
                World.Overworld.PlayerCorporationResources.Add(resource);
        }

        public Race TraderRace { get { return SourceEnvoy.OwnerFaction.Race; } }
        public Faction TraderFaction { get { return SourceEnvoy.OwnerFaction; } }

        public DwarfBux ComputeValue(String Resource)
        {
            return 0;
        }

        public DwarfBux ComputeValue(List<ResourceTypeAmount> Resources)
        {
            return 0;
        }

        public List<Resource> RemoveResourcesByType(List<ResourceTypeAmount> ResourceTypes)
        {
            return World.Overworld.PlayerCorporationResources.RemoveByType(ResourceTypes);
        }
    }
}
