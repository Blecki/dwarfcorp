using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CollectPay : CreatureAct
    {
        public DwarfBux Money { get { return GetMoney(); } set { SetMoney(value); } }

        public string MoneyName = "MoneyNeeded";

        public DwarfBux GetMoney()
        {
            return Agent.Blackboard.GetData<DwarfBux>(MoneyName);
        }

        public void SetMoney(DwarfBux value)
        {
            Agent.Blackboard.SetData(MoneyName, value);
        }

        public CollectPay()
        {

        }

        public CollectPay(CreatureAI agent, string moneyAmountName) : base(agent)
        {
            MoneyName = moneyAmountName;
            Name = "Stash " + moneyAmountName;
        }

        public CollectPay(CreatureAI agent, DwarfBux money) :
            base(agent)
        {
            Money = money;
            Name = "Stash " + Money;
        }

        public override IEnumerable<Status> Run()
        {
            Creature.IsCloaked = false;
            Timer waitTimer = new Timer(1.0f, true);

            if(Agent.Faction.Economy.Funds < Money)
            {
                Agent.SetTaskFailureReason("Failed to remove money from zone.");
                yield return Status.Fail;
            }
            else
            {
                Agent.AddMoney(Money);
                Agent.Faction.Economy.Funds -= Money;
                Money = 0;

                var component = EntityFactory.CreateEntity<GameComponent>("Coins", Agent.Physics.Position + new Microsoft.Xna.Framework.Vector3(0.0f, 2.0f, 0.0f));
                var toss = new TossMotion(1.0f, 2.5f, component.LocalTransform, Agent.Physics.Position);
                component.AnimationQueue.Add(toss);
                toss.OnComplete += component.Die;

                Agent.Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
                while (!waitTimer.HasTriggered)
                {
                    Agent.Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                    waitTimer.Update(DwarfTime.LastTime);
                    yield return Status.Running;
                }
                Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
                yield return Status.Success;
            }

        }

    }

}

