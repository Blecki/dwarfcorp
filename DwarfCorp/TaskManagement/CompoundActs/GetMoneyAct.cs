using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GetMoneyAct : CompoundCreatureAct
    {
        public DwarfBux Money { get; set; }
        public bool IncrementDays = true;

        public GetMoneyAct()
        {

        }

        public GetMoneyAct(CreatureAI agent, DwarfBux money) :
            base(agent)
        {
            Name = "Get paid " + money.ToString();
            Money = money;
        }

        public IEnumerable<Act.Status> SetMoneyNeeded(DwarfBux money)
        {
            Agent.Blackboard.SetData<DwarfBux>("MoneyNeeded", money);
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> ShouldContinue()
        {
            if (!Agent.Blackboard.Has("MoneyNeeded"))
            {
                Agent.SetTaskFailureReason("Failed to get money. Internal error.");
                yield return Act.Status.Fail;
                yield break;
            }

            var needed = Agent.Blackboard.GetData<DwarfBux>("MoneyNeeded");

            if (needed <= 0)
            {
                if (Agent is DwarfAI dorf) dorf.OnPaid();
                yield return Act.Status.Fail;
                yield break;
            }

            if (Agent.Faction.Economy.Funds < needed)
            {
                Agent.World.MakeAnnouncement(String.Format("Could not pay {0}, not enough money!", Agent.Stats.FullName));
                Agent.SetTaskFailureReason("Failed to get money, not enough in treasury.");
                if (IncrementDays && Agent is DwarfAI dorf)
                    dorf.OnNotPaid();

                yield return Act.Status.Fail;
                yield break;
            }
            
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            base.Initialize();

        }

        public override IEnumerable<Status> Run()
        {
            if (Tree == null)
            {
                Agent.Blackboard.SetData<DwarfBux>("MoneyNeeded", Money);

                Tree = new WhileLoop(new Sequence(new CollectPay(Agent, "MoneyNeeded")), 
                                     new Wrap(() => ShouldContinue())
                    );

            }
            return base.Run();
        }
    }
}