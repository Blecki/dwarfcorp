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

        public int AvailableSpace { get { return 0; } }
        public DwarfBux Money { get { return World.Settings.PlayerCorporationFunds; } }
        public List<ResourceAmount> Resources { get { return SourceEnvoy.TradeGoods; } } // Todo: Grab overworld resources

        public void AddMoney(DwarfBux Money)
        {
            World.Settings.PlayerCorporationFunds += Money;
        }

        public void AddResources(List<ResourceAmount> Resources)
        {
            foreach(var resource in Resources)
                World.Settings.PlayerCorporationResources.Add(resource);
        }

        public Race TraderRace { get { return SourceEnvoy.OwnerFaction.Race; } }
        public Faction TraderFaction { get { return SourceEnvoy.OwnerFaction; } }

        public DwarfBux ComputeValue(String Resource)
        {
            return 0;
        }

        public DwarfBux ComputeValue(List<ResourceAmount> Resources)
        {
            return 0;
        }

        public void RemoveResources(List<ResourceAmount> Resources)
        {
            foreach (var r in Resources)
                World.Settings.PlayerCorporationResources.Remove(r);
        }
    }
}
