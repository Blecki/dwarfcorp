using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Trade
{
    public interface ITradeEntity
    {
        List<ResourceAmount> Resources { get; }
        int Money { get; }
        int AvailableSpace { get; }
        float ComputeValue(List<ResourceAmount> Resources);
        float ComputeValue(ResourceLibrary.ResourceType Resource);

        void RemoveResources(List<ResourceAmount> Resources);
        void AddResources(List<ResourceAmount> Resources);
        void AddMoney(float Money);
    }
}
