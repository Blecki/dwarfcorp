using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class GoToChairAndSitAct : CompoundCreatureAct
    {
        public float SitTime { get; set; }

        public GoToChairAndSitAct()
        {
            Name = "Go to chair and sit";
            SitTime = 30.0f;
        }

        public GoToChairAndSitAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Go to chair and sit";
            SitTime = 30.0f;
        }


        public void ConverseFriends()
        {
            foreach (CreatureAI minion in Creature.Faction.Minions)
            {
                if (minion == Creature.AI || minion.Creature.IsAsleep)
                    continue;

                float dist = (minion.Position - Creature.AI.Position).Length();

                if (dist < 2 && MathFunctions.Rand(0, 1) < 0.1f)
                {
                    Creature.AI.Converse(minion);
                }
            }
        }

        public IEnumerable<Status> EatFood()
        {
            if (Creature.Status.Hunger.IsSatisfied()) yield break;

            foreach (Status status in Creature.EatStockedFood())
            {
                yield return status;
            }

        }

        public IEnumerable<Status> WaitUntilBored()
        {
            Timer waitTimer = new Timer(SitTime, false);
            Timer eatTimer = new Timer(10.0f + MathFunctions.Rand(0, 1), false);
            Vector3 snapPosition = Agent.Position + new Vector3(0, 0.2f, 0);
            Body body = Creature.AI.Blackboard.GetData<Body>("Chair");

            if (body == null || body.IsDead)
            {
                Creature.OverrideCharacterMode = false;
                yield return Status.Success;
                yield break;
            }

            while (true)
            {
                if (Creature.AI.Tasks.Count > 1)
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Status.Energy.IsUnhappy())
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Status.Hunger.IsUnhappy())
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Sensor.Enemies.Count > 0)
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                waitTimer.Update(DwarfTime.LastTime);

                if (waitTimer.HasTriggered)
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                ConverseFriends();

                eatTimer.Update(DwarfTime.LastTime);

                if(eatTimer.HasTriggered)
                    foreach (Act.Status status in EatFood())
                    {
                        if (status == Act.Status.Running)
                        {
                            Creature.OverrideCharacterMode = false;
                            yield return Act.Status.Running;
                        }
                    }

                Agent.Position = snapPosition;
                Agent.Physics.IsSleeping = true;
                Creature.CurrentCharacterMode = Creature.CharacterMode.Sitting;
                Creature.OverrideCharacterMode = true;
                yield return Status.Running;
            }
        }

        public override void Initialize()
        {
            Creature.OverrideCharacterMode = false;
           
            Tree = new Sequence(new ClearBlackboardData(Creature.AI, "Chair"),
                                new Wrap(() => Creature.FindAndReserve("Chair", "Chair")),
                                new GoToTaggedObjectAct(Creature.AI) {Tag = "Chair", Teleport = true, TeleportOffset = new Vector3(0, 0.1f, 0), ObjectName = "Chair"},
                                new Wrap(WaitUntilBored),
                                new Wrap(() => Creature.Unreserve("Chair"))) | new Wrap(() => Creature.Unreserve("Chair"));
            base.Initialize();
        }

        public override void OnCanceled()
        {
            foreach (var statuses in Creature.Unreserve("Chair"))
            {
                continue;
            }
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}
