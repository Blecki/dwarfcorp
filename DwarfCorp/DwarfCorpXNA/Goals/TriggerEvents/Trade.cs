using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Events
{
    public class Trade : TriggerEvent
    {
        public Faction PlayerFaction;
        public DwarfBux PlayerGold;
        public List<ResourceAmount> PlayerGoods;

        public Faction OtherFaction;
        public DwarfBux OtherGold;
        public List<ResourceAmount> OtherGoods;
    }
}
