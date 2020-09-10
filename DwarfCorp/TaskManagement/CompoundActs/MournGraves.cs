using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class MournGraves : CompoundCreatureAct
    {
        public float SitTime { get; set; }

        public MournGraves()
        {
            Name = "Mourn the dead";
            SitTime = 30.0f;
        }

        public MournGraves(CreatureAI agent) :
            base(agent)
        {
            Name = "Mourn the dead";
            SitTime = 30.0f;
        }


        public void ConverseWithDead()
        {
            Creature.AI.Converse(Creature.AI);
        }

        public IEnumerable<Status> WaitUntilBored()
        {
            Timer waitTimer = new Timer(SitTime, false);
            Vector3 snapPosition = Agent.Position;
            GameComponent body = Creature.AI.Blackboard.GetData<GameComponent>("grave-object");

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

                waitTimer.Update(Agent.FrameDeltaTime);

                if (waitTimer.HasTriggered)
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                ConverseWithDead();


                Agent.Position = snapPosition;
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

            Tree = new Select(
                new Sequence(
                    new ClearBlackboardData(Creature.AI, "grave-object"),
                    new Wrap(() => Creature.FindAndReserve("Grave", "grave-object")),
                    new GoToTaggedObjectAct(Creature.AI) { Teleport = false, TeleportOffset = new Vector3(1.0f, 0.0f, 0), ObjectBlackboardName = "grave-object" },
                    new Wrap(WaitUntilBored),
                    new Wrap(() => Creature.Unreserve("grave-object"))),
                new Wrap(() => Creature.Unreserve("grave-object")));
            base.Initialize();
        }

        public override void OnCanceled()
        {
            foreach (var statuses in Creature.Unreserve("grave-object"))
                continue;
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}
