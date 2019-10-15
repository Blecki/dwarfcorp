using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Trade
{
    public interface ITradeEntity
    {
        ResourceSet Resources { get; }
        DwarfBux Money { get; }
        int AvailableSpace { get; }
        DwarfBux ComputeValue(List<Resource> Resources);
        DwarfBux ComputeValue(String Resource);
        Race TraderRace { get; }
        Faction TraderFaction { get; }
        void RemoveResources(List<Resource> Resources);
        void AddResources(List<Resource> Resources);
        void AddMoney(DwarfBux Money);
    }
}
