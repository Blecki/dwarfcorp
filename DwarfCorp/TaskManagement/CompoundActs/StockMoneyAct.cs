using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class StockMoneyAct : CompoundCreatureAct
    {
        public DwarfBux Money { get; set; }
        public StockMoneyAct()
        {

        }

        public StockMoneyAct(CreatureAI agent, DwarfBux money) :
            base(agent)
        {
            Tree = null;
            Name = "Stock money";
            Money = money;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            if (Tree != null) return base.Run();
            ResourceAmount coins = new ResourceAmount(ResourceType.Coins, 1);
            Tree = new DepositMoney(Agent, Money);
            Tree.Initialize();
            return base.Run();
        }
    }

}