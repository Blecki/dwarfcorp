using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GetMoneyAct : CompoundCreatureAct
    {
        public DwarfBux Money { get; set; }
        public Faction Faction { get; set; }
        public bool IncrementDays = true;

        public GetMoneyAct()
        {

        }

        public GetMoneyAct(CreatureAI agent, DwarfBux money, Faction faction = null) :
            base(agent)
        {
            Name = "Get paid " + money.ToString();
            Money = money;
            if (faction == null)
                Faction = Creature.Faction;
            else
                Faction = faction;
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
                Agent.SetMessage("Failed to get money. Internal error.");
                yield return Act.Status.Fail;
                yield break;
            }

            var needed = Agent.Blackboard.GetData<DwarfBux>("MoneyNeeded");
            if (needed <= 0)
            {
                Agent.NumDaysNotPaid = 0;
                yield return Act.Status.Fail;
                yield break;
            }

            if (Faction.Economy.Funds < needed)
            {
                Agent.World.MakeAnnouncement(String.Format("Could not pay {0}, not enough money!", Agent.Stats.FullName));
                Agent.SetMessage("Failed to get money, not enough in treasury.");
                if (IncrementDays)
                {
                    Agent.NumDaysNotPaid++;

                    if (Agent.NumDaysNotPaid < 2)
                    {
                        Agent.Creature.AddThought(Thought.ThoughtType.NotPaid);
                    }
                    else
                    {
                        Agent.Creature.AddThought(new Thought()
                        {
                            Description = String.Format("I have not been paid in {0} days!", Agent.NumDaysNotPaid),
                            HappinessModifier = -25 * Agent.NumDaysNotPaid,
                            TimeLimit = new TimeSpan(1, 0, 0, 0, 0),
                            TimeStamp = Agent.World.Time.CurrentDate,
                            Type = Thought.ThoughtType.Other
                        }, false);
                    }
                }

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