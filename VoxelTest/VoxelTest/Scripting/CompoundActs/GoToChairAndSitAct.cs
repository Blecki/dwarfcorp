using System;
using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<Status> UnreserveChair()
        {
            Body body = Creature.AI.Blackboard.GetData<Body>("Chair");

            if (body != null)
            {
                body.IsReserved = false;
                body.ReservedFor = null;
                yield return Status.Success;
            }

            yield return Status.Fail;
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

            if (body == null || body.IsDead || body.IsReserved)
            {
                Creature.OverrideCharacterMode = false;
                yield return Status.Success;
                yield break;
            }

            body.IsReserved = true;
            
            while (true)
            {
                if (Creature.AI.Tasks.Count > 0)
                {
                    Creature.OverrideCharacterMode = false;
                    body.IsReserved = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Status.Energy.IsUnhappy())
                {
                    Creature.OverrideCharacterMode = false;
                    body.IsReserved = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Status.Hunger.IsUnhappy())
                {
                    Creature.OverrideCharacterMode = false;
                    body.IsReserved = false;
                    yield return Status.Success;
                }

                waitTimer.Update(Act.LastTime);

                if (waitTimer.HasTriggered)
                {
                    Creature.OverrideCharacterMode = false;
                    body.IsReserved = false;
                    yield return Status.Success;
                }

                ConverseFriends();

                eatTimer.Update(Act.LastTime);

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
                Creature.CurrentCharacterMode = Creature.CharacterMode.Falling;
                Creature.OverrideCharacterMode = true;
                yield return Status.Running;
            }
        }

        public override void Initialize()
        {
            Creature.OverrideCharacterMode = false;
            Tree = new Sequence(new GoToTaggedObjectAct(Creature.AI) {Tag = "Chair", Teleport = true, TeleportOffset = new Vector3(0, 0.1f, 0), ObjectName = "Chair"},
                                new Wrap(WaitUntilBored),
                                new Wrap(UnreserveChair)) | new Wrap(UnreserveChair);
            base.Initialize();
        }
    }
}
