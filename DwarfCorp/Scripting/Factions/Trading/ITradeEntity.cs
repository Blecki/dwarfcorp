using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Trade
{
    public interface ITradeEntity
    {
        List<ResourceAmount> Resources { get; }
        DwarfBux Money { get; }
        int AvailableSpace { get; }
        DwarfBux ComputeValue(List<ResourceAmount> Resources);
        DwarfBux ComputeValue(String Resource);
        Race TraderRace { get; }
        Faction TraderFaction { get; }
        void RemoveResources(List<ResourceAmount> Resources);
        void AddResources(List<ResourceAmount> Resources);
        void AddMoney(DwarfBux Money);
    }
}
