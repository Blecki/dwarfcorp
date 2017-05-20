using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Events
{
    public class Trade : GameEvent
    {
        public Faction A;
        public DwarfBux AGold;
        public List<ResourceAmount> AGoods;

        public Faction B;
        public DwarfBux BGold;
        public List<ResourceAmount> BGoods;
    }
}
