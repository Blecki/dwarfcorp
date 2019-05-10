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
                if (minion == Creature.AI || minion.Creature.Stats.IsAsleep)
                    continue;

                float dist = (minion.Position - Creature.AI.Position).Length();

                if (dist < 2 && MathFunctions.Rand(0, 1) < 0.1f)
                {
                    Creature.AI.Converse(minion);
                }
            }
        }

        public bool ValidateSit()
        {
            GameComponent chair = Agent.Blackboard.GetData<GameComponent>("Chair");
            if (chair == null || chair.IsDead || !chair.Active)
            {
                return false;
            }

            return true;
        }

        public IEnumerable<Status> WaitUntilBored()
        {
            Timer waitTimer = new Timer(SitTime, false);
            GameComponent body = Creature.AI.Blackboard.GetData<GameComponent>("Chair");

            // Snap relative the chair's position, not their own...
            Vector3 snapPosition = body.Position + new Vector3(0, 0.4f, 0);

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

                if (Creature.AI.Stats.Energy.IsDissatisfied())
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Stats.Hunger.IsDissatisfied())
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


                Agent.Position = snapPosition;
                Agent.Physics.PropogateTransforms();
                Agent.Physics.IsSleeping = true;
                Agent.Physics.Velocity = Vector3.Zero;
                Creature.CurrentCharacterMode = CharacterMode.Sitting;
                Creature.OverrideCharacterMode = true;
                yield return Status.Running;
            }
        }

        public override void Initialize()
        {
            Creature.OverrideCharacterMode = false;
           
            Tree = new Domain(  () => !Agent.IsDead && !Agent.Creature.Stats.IsAsleep,
                                new Sequence(new ClearBlackboardData(Creature.AI, "Chair"),
                                new Wrap(() => Creature.FindAndReserve("Chair", "Chair")),
                                new Domain(ValidateSit, new Sequence(
                                new GoToTaggedObjectAct(Creature.AI) {Tag = "Chair", Teleport = true, TeleportOffset = new Vector3(0, 0.1f, 0), ObjectName = "Chair", CheckForOcclusion = false},
                                new Wrap(WaitUntilBored))),
                                new Wrap(() => Creature.Unreserve("Chair")))) | new Wrap(() => Creature.Unreserve("Chair"));
            base.Initialize();
        }

        public override void OnCanceled()
        {
            Agent.Physics.IsSleeping = false;
            Agent.Physics.Velocity = Vector3.Zero;
            Creature.OverrideCharacterMode = false;
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
